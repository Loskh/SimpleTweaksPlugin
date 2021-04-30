using System;
using System.Collections.Generic;
using Dalamud.Game.Internal;
using FFXIVClientStructs.FFXIV.Component.GUI;
using SimpleTweaksPlugin.Tweaks.UiAdjustment;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Hooking;
using SimpleTweaksPlugin.Helper;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Structs;
using ImGuiNET;


namespace SimpleTweaksPlugin
{
    public partial class UiAdjustmentsConfig
    {
        public HidePartyNames.Configs HidePartyNames = new();
    }
}

namespace SimpleTweaksPlugin.Tweaks.UiAdjustment
{
    public unsafe class HidePartyNames : UiAdjustments.SubTweak
    {
        public class Configs
        {
            public bool Target = true;
            public bool Focus = true;
        }

        public Configs Config => PluginConfig.UiAdjustments.HidePartyNames;

        private static readonly string[] JobStrings = new string[]
        {
            "冒险者",
            "剑术师",
            "格斗家",
            "斧术师",
            "枪术师",
            "弓箭手",
            "幻术师",
            "咒术师",
            "刻木匠",
            "锻铁匠",
            "铸甲匠",
            "雕金匠",
            "制革匠",
            "裁衣匠",
            "炼金术士",
            "烹调师",
            "采矿工",
            "园艺工",
            "捕鱼人",
            "骑士",
            "武僧",
            "战士",
            "龙骑士",
            "诗人",
            "白魔法师",
            "黑魔法师",
            "秘术师",
            "召唤师",
            "学者",
            "双剑师",
            "忍者",
            "机工士",
            "暗黑骑士",
            "占星术士",
            "武士",
            "赤魔法师",
            "青魔法师",
            "绝枪战士",
            "舞者",
            "UNKNOWN"
        };

        private const string PartyNumber = "";

        private delegate void PartyUiUpdate(long a1, long a2, long a3);

        private delegate void MainTargetUiUpdate(long a1, long a2, long a3);

        private delegate long FocusUiUpdate(long a1, long a2, long a3);

        private Hook<PartyUiUpdate> partyUiUpdateHook;
        private Hook<MainTargetUiUpdate> mainTargetUpdateHook;
        private Hook<FocusUiUpdate> focusUpdateHook;

        private Actor player;
        private readonly PartyList partyList = new(Common.PluginInterface);
        private AtkComponentNode* partyNode;
        private AtkTextNode* focusTextNode;
        private AtkTextNode* tTextNode;
        private AtkTextNode* ttTextNode;
        private readonly AtkTextNode*[] textsNodes = new AtkTextNode*[8];

        public override string Name => "隐藏组队界面角色名";
        public override string Description => "隐藏组队界面角色名";


        protected override DrawConfigDelegate DrawConfigTree => (ref bool changed) =>
        {
            changed |= ImGui.Checkbox("隐藏目标栏的队友姓名", ref Config.Target);
            ImGui.SameLine();
            changed |= ImGui.Checkbox("隐藏焦点栏的队友姓名", ref Config.Focus);

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

                if (Enabled) partyUiUpdateHook?.Enable();
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

        private void PartyListUpdateDelegate(long a1, long a2, long a3)
        {
            partyUiUpdateHook.Original(a1, a2, a3);
            UpdatePartyUi();
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

        private void WriteSeString(AtkTextNode* node, string payload)
        {
            var seString = new SeString(new List<Payload>());
            seString.Payloads.Add(new TextPayload(payload));
            Plugin.Common.WriteSeString(node->NodeText, seString);
        }

        #endregion


        private void FindAddress(Framework framework)
        {
            if (PluginInterface.ClientState.Actors[0] == null) return;

            var partyPointer = (AtkUnitBase*) framework.Gui.GetAddonByName("_PartyList", 1).Address;
            partyNode = (AtkComponentNode*) partyPointer->UldManager.NodeList[17];
            if (partyNode == null) return;
            var tempNode = partyNode; //PartyListUI.player<1>.atkComponentNode
            for (var i = 0; i < 8; i++)
            {
                var text = (AtkTextNode*) tempNode->Component->UldManager.NodeList[15]->ChildNode;
                textsNodes[i] = text;
                tempNode = (AtkComponentNode*) tempNode->AtkResNode.NextSiblingNode;
            }

            var mainTargetUi = (AtkUnitBase*) framework.Gui.GetAddonByName("_TargetInfoMainTarget", 1).Address;
            tTextNode = (AtkTextNode*) mainTargetUi->UldManager.NodeList[8];
            ttTextNode = (AtkTextNode*) mainTargetUi->UldManager.NodeList[12];
            focusTextNode =
                (AtkTextNode*) ((AtkUnitBase*) framework.Gui.GetAddonByName("_FocusTargetInfo", 1).Address)->UldManager
                .NodeList[10];

            player = Marshal.PtrToStructure<Actor>(PluginInterface.ClientState.Actors[0].Address);

            RefreshHooks();

            PluginInterface.Framework.OnUpdateEvent -= FindAddress;
        }


        private void UpdatePartyUi()
        {
            try
            {
                if (partyNode == null) return;
                var tempNode = partyNode;

                var str0 = Plugin.Common.ReadSeString(textsNodes[0]->NodeText.StringPtr).ToString();
                str0 = str0.Split(' ')[0];
                WriteSeString(textsNodes[0], str0 + " " + JobStrings[player.ClassJob]);

                for (var i = 1; i < 8; i++)
                {
                    tempNode = (AtkComponentNode*) tempNode->AtkResNode.NextSiblingNode;
                    if (!tempNode->AtkResNode.IsVisible) break;
                    var str = Plugin.Common.ReadSeString(textsNodes[i]->NodeText.StringPtr).ToString(); //UI string
                    if (str == "") break;
                    SplitString(str, true, out var lvl, out var namejob);

                    var index = partyList.IndexOf(namejob);
                    if (index != -1)
                    {
                        var job = JobStrings[partyList[index].ClassJob];
                        WriteSeString(textsNodes[i], lvl + " " + job);
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
            try
            {
                SplitString(Plugin.Common.ReadSeString(tTextNode->NodeText.StringPtr).ToString(), false, out var tname,
                    out _);
                var ttname = Plugin.Common.ReadSeString(ttTextNode->NodeText.StringPtr).ToString();
                if (tname.Length >= 1)
                {
                    var index = partyList.IndexOf(tname);

                    if (index != -1)
                    {
                        var job = JobStrings[partyList[index].ClassJob];
                        WriteSeString(tTextNode, job);
                    }
                }

                if (ttname.Length >= 1)
                {
                    var number = ttname.Substring(0, 1);
                    if (PartyNumber.Contains(number)) ttname = ttname.Substring(1);
                    var index = partyList.IndexOf(ttname);
                    if (index != -1)
                    {
                        var job = JobStrings[partyList[index].ClassJob];
                        WriteSeString(ttTextNode,
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
            try
            {
                SplitString(Plugin.Common.ReadSeString(focusTextNode->NodeText.StringPtr).ToString(), true,
                    out var part1,
                    out var part2);
                if (part2 != "")
                {
                    var number = part2.Substring(0, 1);
                    if (PartyNumber.Contains(number)) part2 = part2.Substring(1);
                    var index = partyList.IndexOf(part2);

                    if (index != -1)
                    {
                        var job = JobStrings[partyList[index].ClassJob];
                        WriteSeString(focusTextNode,
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
                        var index = partyList.IndexOf(part1);
                        if (index != -1)
                        {
                            var job = JobStrings[partyList[index].ClassJob];
                            WriteSeString(focusTextNode,
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

        private void OnlogOut(object sender, EventArgs e)
        {
            PluginInterface.Framework.OnUpdateEvent += FindAddress;
            DisposeHooks();
        }

        public override void Enable()
        {
            if (Enabled) return;
            PluginInterface.Framework.OnUpdateEvent += FindAddress;
            PluginInterface.ClientState.OnLogout += OnlogOut;
            Enabled = true;
        }

        public override void Disable()
        {
            if (!Enabled) return;
            PluginInterface.Framework.OnUpdateEvent -= FindAddress;
            PluginInterface.ClientState.OnLogout -= OnlogOut;
            DisableHooks();
            SimpleLog.Debug($"[{GetType().Name}] Reset");
            Enabled = false;
        }


        public override void Dispose()
        {
            PluginInterface.Framework.OnUpdateEvent -= FindAddress;
            PluginInterface.ClientState.OnLogout -= OnlogOut;
            DisposeHooks();
            Enabled = false;
            Ready = false;
            SimpleLog.Debug($"[{GetType().Name}] Disposed");
        }

        #endregion
    }
}