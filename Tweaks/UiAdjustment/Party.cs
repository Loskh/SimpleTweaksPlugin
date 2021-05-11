using Dalamud;
using Dalamud.Game.Internal;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using SimpleTweaksPlugin.Helper;
using SimpleTweaksPlugin.Tweaks.UiAdjustment;
using System;
using System.Collections.Generic;
using SimpleTweaksPlugin.GameStructs;


namespace SimpleTweaksPlugin
{
    public partial class UiAdjustmentsConfig
    {
        public PartyListAdjustments.Configs PartyListAdjustments = new();
    }
}

namespace SimpleTweaksPlugin.Tweaks.UiAdjustment
{
    public unsafe class PartyListAdjustments : UiAdjustments.SubTweak
    {
        public class Configs
        {
            public bool Target;
            public bool Focus;
            public bool HpPercent = true;
            public bool PartyName;
        }

        public Configs Config => PluginConfig.UiAdjustments.PartyListAdjustments;

        private const string PartyNumber = "";

        private delegate long PartyUiUpdate(long a1, long a2, long a3);

        private delegate void MainTargetUiUpdate(long a1, long a2, long a3);

        private delegate long FocusUiUpdate(long a1, long a2, long a3);

        private Hook<PartyUiUpdate> partyUiUpdateHook;
        private Hook<MainTargetUiUpdate> mainTargetUpdateHook;
        private Hook<FocusUiUpdate> focusUpdateHook;

        private PartyUi* party;
        private DataArray* data;

        //private AtkComponentNode* partyNode;
        private AtkTextNode* focusTextNode;
        private AtkTextNode* tTextNode;

        private AtkTextNode* ttTextNode;

        //private readonly AtkTextNode*[] textsNodes = new AtkTextNode*[8];
        //private readonly AtkTextNode*[] hpNodes = new AtkTextNode*[8];
        //private string[] namecache = new string[8];
        private IntPtr l1, l2, l3;


        public override string Name => "队伍列表修改";
        public override string Description => "队伍列表相关内容修改";


        protected override DrawConfigDelegate DrawConfigTree => (ref bool changed) =>
        {
            changed |= ImGui.Checkbox("HP及盾值百分比显示", ref Config.HpPercent);
            if (Config.PartyName || Config.Target || Config.Focus)
            {
                changed |= ImGui.Checkbox("隐藏队伍栏的队友姓名", ref Config.PartyName);
                ImGui.SameLine();
                changed |= ImGui.Checkbox("隐藏目标栏的队友姓名", ref Config.Target);
                ImGui.SameLine();
                changed |= ImGui.Checkbox("隐藏焦点栏的队友姓名", ref Config.Focus);
            }


            if (changed) RefreshHooks();
        };


        private void RefreshHooks()
        {
            try
            {
                if (PluginInterface.ClientState.Actors[0] == null) return;

                partyUiUpdateHook ??= new Hook<PartyUiUpdate>(
                    Common.Scanner.ScanText(
                        "48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC ?? 48 8B 7A ?? 48 8B D9 49 8B 70 ?? 48 8B 47"),
                    new PartyUiUpdate(PartyListUpdateDelegate));

                if (Config.HpPercent || Config.PartyName) partyUiUpdateHook?.Enable();
                else partyUiUpdateHook?.Disable();

                mainTargetUpdateHook ??= new Hook<MainTargetUiUpdate>(
                    Common.Scanner.ScanText(
                        "40 55 57 41 56 48 83 EC 40 48 8B 6A 48 48 8B F9 4D 8B 70 40 48 85 ED 0F 84 ?? ?? ?? ?? 4D 85 F6 0F 84 ?? ?? ?? ?? 48 8B 45 20 48 89 74 24 ?? 4C 89 7C 24 ?? 44 0F B6 B9 ?? ?? ?? ?? 83 38 00 8B 70 08 0F 94 C0"),
                    new MainTargetUiUpdate(MainTargetUpdateDelegate));

                if (Config.Target) mainTargetUpdateHook?.Enable();
                else mainTargetUpdateHook?.Disable();

                focusUpdateHook ??= new Hook<FocusUiUpdate>(
                    Common.Scanner.ScanText("40 53 41 54 41 56 41 57 48 83 EC 78 4C 8B 7A 48"),
                    new FocusUiUpdate(FocusUpdateDelegate));

                if (Config.Focus) focusUpdateHook?.Enable();
                else focusUpdateHook?.Disable();
            }
            catch (Exception e)
            {
                SimpleLog.Error(e);
                throw;
            }
        }

        private void DisposeHooks()
        {
            partyUiUpdateHook?.Dispose();
            partyUiUpdateHook = null;
            mainTargetUpdateHook?.Dispose();
            mainTargetUpdateHook = null;
            focusUpdateHook?.Dispose();
            focusUpdateHook = null;
        }

        private void DisableHooks()
        {
            //if (!partyUiUpdateHook.IsDisposed) 
            partyUiUpdateHook?.Disable();
            //if (!mainTargetUpdateHook.IsDisposed) 
            mainTargetUpdateHook?.Disable();
            //if (!focusUpdateHook.IsDisposed) 
            focusUpdateHook?.Disable();
        }


        #region detors

        private long PartyListUpdateDelegate(long a1, long a2, long a3)
        {
            if ((IntPtr) a1 != l1)
            {
                l1 = (IntPtr) a1;
                l2 = (IntPtr) (*(long*) (*(long*) (a2 + 0x20) + 0x20));
                l3 = (IntPtr) (*(long*) (*(long*) (a3 + 0x18) + 0x20) + 0x30); //+Index*0x68
                party = (PartyUi*) l1;
                data = (DataArray*) l2;


                SimpleLog.Information("NewAddress:");
                SimpleLog.Information("L1:" + l1.ToString("X") + " L2:" + l2.ToString("X"));
                SimpleLog.Information("L3:" + l3.ToString("X"));
            }
            UpdatePartyUi(false);
            var ret = partyUiUpdateHook.Original(a1, a2, a3);
            UpdatePartyUi(true);
            return ret;
        }

        private void MainTargetUpdateDelegate(long a1, long a2, long a3)
        {
            mainTargetUpdateHook.Original(a1, a2, a3);
            UpdateTarget();
        }

        private long FocusUpdateDelegate(long a1, long a2, long a3)
        {
            var ret = focusUpdateHook.Original(a1, a2, a3);
            UpdateFocus();
            return ret;
        }

        #endregion

        #region string functions

        private static void SplitString(string str, bool first, out string part1, out string part2)
        {
            if (str.Length == 0)
            {
                part1 = "";
                part2 = "";
                return ;
            }
            var index = first ? str.IndexOf(' ') : str.LastIndexOf(' ');
            if (index == -1)
            {
                part1 = str;
                part2 = "";
            }
            else
            {
                part1 = str.Substring(0, index).Trim();
                part2 = str.Substring(index + 1).Trim();
            }
        }

        private void SetName(AtkTextNode* node, string payload)
        {
            if (node == null || payload == String.Empty) return;
            var seString = new SeString(new List<Payload>());
            seString.Payloads.Add(new TextPayload(payload));
            Plugin.Common.WriteSeString(node->NodeText, seString);
        }

        private void SetHp(AtkTextNode* node, MemberData member)
        {
            var se = new SeString(new List<Payload>());
            if (member.CurrentHP == 1)
            {
                se.Payloads.Add(new TextPayload("1"));
            }
            else if (member.MaxHp == 1)
            {
                se.Payloads.Add(new TextPayload("???"));
            }
            else
            {
                se.Payloads.Add(new TextPayload((member.CurrentHP * 100 / member.MaxHp).ToString()));
                if (member.ShieldPercent != 0)
                {
                    UIForegroundPayload uiYellow =
                        new(PluginInterface.Data, 559);
                    UIForegroundPayload uiNoColor =
                        new(PluginInterface.Data, 0);

                    se.Payloads.Add(new TextPayload("+"));
                    se.Payloads.Add(uiYellow);
                    se.Payloads.Add(new TextPayload(member.ShieldPercent.ToString()));
                    se.Payloads.Add(uiNoColor);
                }

                se.Payloads.Add(new TextPayload("%"));
            }

            Plugin.Common.WriteSeString(node->NodeText, se);
        }

        private string GetJobName(uint id)
        {
            return PluginInterface.ClientState.ClientLanguage == ClientLanguage.English
                ? PluginInterface.Data.Excel.GetSheet<Lumina.Excel.GeneratedSheets.ClassJob>().GetRow(id).NameEnglish
                : PluginInterface.Data.Excel.GetSheet<Lumina.Excel.GeneratedSheets.ClassJob>().GetRow(id).Name;
        }

        private static AtkResNode* GetNodeById(AtkComponentBase* compBase, int id)
        {
            if ((compBase->UldManager.Flags1 & 1) == 0 || id == 0) return null;
            if (compBase->UldManager.Objects == null) return null;
            var count = compBase->UldManager.Objects->NodeCount;
            var ptr = (long) compBase->UldManager.Objects->NodeList;
            for (var i = 0; i < count; i++)
            {
                var node = (AtkResNode*) *(long*) (ptr + 8 * i);
                if (node->NodeID == id) return node;
            }

            return null;
        }

        private int GetIndex(SeString name)
        {
            try
            {
                for (var i = 0; i < data->LocalCount + data->CrossRealmCount; i++)
                {
                    var ptr = *((long*) l3+i*13)+0x68;
                    if (Plugin.Common.ReadSeString((byte*) ptr).TextValue == name.TextValue) return i;
                }

                return -1;
            }
            catch (Exception e)
            {
                SimpleLog.Error(e);
                throw;
            }
        }

        #endregion


        private void FindAddress(Framework framework)
        {
            if (PluginInterface.ClientState.Actors[0] == null) return;

            var mainTargetUi = (AtkUnitBase*) framework.Gui.GetAddonByName("_TargetInfoMainTarget", 1).Address;
            tTextNode = (AtkTextNode*) mainTargetUi->UldManager.NodeList[8];
            ttTextNode = (AtkTextNode*) mainTargetUi->UldManager.NodeList[12];
            focusTextNode =
                (AtkTextNode*) ((AtkUnitBase*) framework.Gui.GetAddonByName("_FocusTargetInfo", 1).Address)->UldManager
                .NodeList[10];

            RefreshHooks();

            PluginInterface.Framework.OnUpdateEvent -= FindAddress;
        }


        private void UpdatePartyUi(bool done)
        {
            try
            {
                
                for (var index = 0; index < data->LocalCount + data->CrossRealmCount; index++)
                {
                    if (!done) //改名
                    {
                        if (!Config.PartyName) return;
                        var address = (byte*) *(((long*) l3) + 13 * index);
                        var job = data->MemberData(index).JobId - 0xF294;

                        job = job > 38 ? 0:job;
                        if (Plugin.Common.ReadSeString(address).TextValue != GetJobName(job) ||
                            (data->MemberData(index).JobId != party->JobId[index]))
                        {
                            
                            Plugin.Common.WriteSeString(address, GetJobName(job));
                            *((byte*) data + 0x1C + index * 0x9C) = 1; //Changed
                        }
                    }
                    else //改HP
                    {
                        if (!Config.HpPercent) return;
                        var textNode = (AtkTextNode*) GetNodeById(party->Member(index).hpComponentBase, 2);
                        if (textNode != null)
                        {
                            SetHp(textNode, data->MemberData(index));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                SimpleLog.Error(e);
                throw;
            }

        }

        private void UpdateTarget()
        {
            if (PluginInterface.ClientState.Actors[0] == null) return;
            try
            {
                SplitString(Plugin.Common.ReadSeString(tTextNode->NodeText.StringPtr).ToString(), false, out var tname,
                    out _);
                var ttname = Plugin.Common.ReadSeString(ttTextNode->NodeText.StringPtr).ToString();
                if (tname.Length >= 1)
                {
                    var index = GetIndex(tname);
                    
                    if (index != -1)
                    {
                        var jobid = data->MemberData(index).JobId;
                        jobid = jobid == 0 ? 0 : jobid - 0xF294;
                        var job = GetJobName(jobid);
                        SetName(tTextNode, job);
                    }
                }

                if (ttname.Length >= 1)
                {
                    var number = ttname.Substring(0, 1);
                    if (PartyNumber.Contains(number)) ttname = ttname.Substring(1);
                    var index = GetIndex(ttname);
                    if (index != -1)
                    {
                        var jobid = data->MemberData(index).JobId;
                        jobid = jobid == 0 ? 0 : jobid - 0xF294;
                        var job = GetJobName(jobid);
                        SetName(ttTextNode,
                            PartyNumber.Contains(number) ? number + job : job);
                    }
                }
            }
            catch (Exception e)
            {
                SimpleLog.Error(e);
            }
        }

        private void UpdateFocus()
        {
            if (PluginInterface.ClientState.Actors[0] == null) return;
            try
            {
                SplitString(Plugin.Common.ReadSeString(focusTextNode->NodeText.StringPtr).ToString(), true,
                    out var part1,
                    out var part2);
                if (part2 != "")
                {
                    var number = part2.Substring(0, 1);
                    if (PartyNumber.Contains(number)) part2 = part2.Substring(1);
                    var index = GetIndex(part2);

                    if (index != -1)
                    {
                        var jobid = data->MemberData(index).JobId;
                        jobid = jobid == 0 ? 0 : jobid - 0xF294;
                        var job = GetJobName(jobid);
                        SetName(focusTextNode,
                            PartyNumber.Contains(number)
                                ? part1 + " " + number + job
                                : part1 + " " + job);
                    }
                }
                else if (part1.Length >= 1)
                {
                    if (PartyNumber.Contains(part1.Substring(0, 1)))
                    {
                        var number = part1.Substring(0, 1);
                        part1 = part1.Substring(1);
                        var index = GetIndex(part1);
                        if (index != -1)
                        {
                            var jobid = data->MemberData(index).JobId;
                            jobid = jobid == 0 ? 0 : jobid - 0xF294;
                            var job = GetJobName(jobid);
                            SetName(focusTextNode,
                                number + " " + job);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                SimpleLog.Error(e);
            }
        }


        #region Framework

        private void OnLogOut(object sender, EventArgs e)
        {
            PluginInterface.Framework.OnUpdateEvent += FindAddress;
            //DisposeHooks();
        }

        public override void Enable()
        {
            if (Enabled) return;
            RefreshHooks();
            PluginInterface.Framework.OnUpdateEvent += FindAddress;
            PluginInterface.ClientState.OnLogout += OnLogOut;
            Enabled = true;
        }

        public override void Disable()
        {
            if (!Enabled) return;
            PluginInterface.Framework.OnUpdateEvent -= FindAddress;
            PluginInterface.ClientState.OnLogout -= OnLogOut;
            DisableHooks();
            SimpleLog.Debug($"[{GetType().Name}] Reset");
            Enabled = false;
        }


        public override void Dispose()
        {
            PluginInterface.Framework.OnUpdateEvent -= FindAddress;
            PluginInterface.ClientState.OnLogout -= OnLogOut;
            DisposeHooks();
            Enabled = false;
            Ready = false;
            SimpleLog.Debug($"[{GetType().Name}] Disposed");
        }

        #endregion
    }
}