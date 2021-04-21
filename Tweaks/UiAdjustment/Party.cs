using System;
using System.Collections.Generic;
using Dalamud.Game.Internal;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.FFXIV.Group;
using SimpleTweaksPlugin.Tweaks.UiAdjustment;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Hooking;
using SimpleTweaksPlugin.Helper;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Structs;
using ImGuiNET;
using PartyMember = FFXIVClientStructs.FFXIV.Group.PartyMember;


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

        private readonly string[] jobStrings = new string[]
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

        private delegate void PartyUiUpdate(long a1, long a2, long a3);

        private delegate void MaintargetUiUpdate(long a1, long a2, long a3);

        private delegate long FocusUiUpdate(long a1, long a2, long a3);

        private Hook<PartyUiUpdate> partyUiUpdateHook;
        private Hook<MaintargetUiUpdate> mainTargetUpdateHook;
        private Hook<FocusUiUpdate> focusUpdateHook;

        private GroupManager* groupManager;
        private PartyMember* partyMembers;
        private AtkComponentNode* partyNode;
        private AtkTextNode*[] textsNodes = new AtkTextNode*[8];
        private string[] namecahce = new string[8];
        private string[] partynames = new string[8];
        private string[] partyjobs = new string[8];
        private int count;
        private IntPtr player;
        private AtkTextNode* focusTextNode;
        private AtkTextNode* tTextNode;
        private AtkTextNode* ttTextNode;

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
                    new PartyUiUpdate(PartyListUpdateDeto));
                //if (partyUiUpdateHook.IsDisposed)
                //    partyUiUpdateHook = new Hook<PartyUiUpdate>(
                //        Common.Scanner.ScanText(
                //            "48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC ?? 48 8B 7A ?? 48 8B D9 49 8B 70 ?? 48 8B 47"),
                //        new PartyUiUpdate(PartyListUpdateDeto));
                if (Enabled) partyUiUpdateHook?.Enable();
                else partyUiUpdateHook?.Disable();

                mainTargetUpdateHook ??= new Hook<MaintargetUiUpdate>(
                    Common.Scanner.ScanText(
                        "40 55 57 41 56 48 83 EC 40 48 8B 6A 48 48 8B F9 4D 8B 70 40 48 85 ED 0F 84 ?? ?? ?? ?? 4D 85 F6 0F 84 ?? ?? ?? ?? 48 8B 45 20 48 89 74 24 ?? 4C 89 7C 24 ?? 44 0F B6 B9 ?? ?? ?? ?? 83 38 00 8B 70 08 0F 94 C0"),
                    new MaintargetUiUpdate(MaintargetUpdateDeto));
                //if (mainTargetUpdateHook.IsDisposed)
                //    mainTargetUpdateHook = new Hook<MaintargetUiUpdate>(
                //        Common.Scanner.ScanText(
                //            "40 55 57 41 56 48 83 EC 40 48 8B 6A 48 48 8B F9 4D 8B 70 40 48 85 ED 0F 84 ?? ?? ?? ?? 4D 85 F6 0F 84 ?? ?? ?? ?? 48 8B 45 20 48 89 74 24 ?? 4C 89 7C 24 ?? 44 0F B6 B9 ?? ?? ?? ?? 83 38 00 8B 70 08 0F 94 C0"),
                //        new MaintargetUiUpdate(MaintargetUpdateDeto));
                if (Config.Target) mainTargetUpdateHook?.Enable();
                else mainTargetUpdateHook?.Disable();

                focusUpdateHook ??= new Hook<FocusUiUpdate>(
                    Common.Scanner.ScanText("40 53 41 54 41 56 41 57 48 83 EC 78 4C 8B 7A 48"),
                    new FocusUiUpdate(FocusUpdateDeto));
                //if (focusUpdateHook.IsDisposed)
                //    focusUpdateHook = new Hook<FocusUiUpdate>(
                //        Common.Scanner.ScanText("40 53 41 54 41 56 41 57 48 83 EC 78 4C 8B 7A 48"),
                //        new FocusUiUpdate(FocusUpdateDeto));
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


        #region detor

        private void PartyListUpdateDeto(long a1, long a2, long a3)
        {
            partyUiUpdateHook.Original(a1, a2, a3);
            UpdatePartyUi(true);
        }

        private void MaintargetUpdateDeto(long a1, long a2, long a3)
        {
            mainTargetUpdateHook.Original(a1, a2, a3);
            UpdateTarget();
        }

        private long FocusUpdateDeto(long a1, long a2, long a3)
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

            groupManager = (GroupManager*) Plugin.PluginInterface.TargetModuleScanner.GetStaticAddressFromSig(
                "48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 80 B8 ?? ?? ?? ?? ?? 76 50");
            partyMembers = (PartyMember*) groupManager->PartyMembers;

            var partyPointer = (AtkUnitBase*) framework.Gui.GetAddonByName("_PartyList", 1).Address;
            partyNode = (AtkComponentNode*) partyPointer->UldManager.NodeList[17];
            if (partyNode == null) return;
            var tempNode = partyNode; //PartylistUI.player<1>.atkComponetnode
            for (var i = 0; i < 8; i++)
            {
                var text = (AtkTextNode*) tempNode->Component->UldManager.NodeList[15]->ChildNode;
                textsNodes[i] = text;
                tempNode = (AtkComponentNode*) tempNode->AtkResNode.NextSiblingNode;
            }

            try
            {
                var maintargetui = (AtkUnitBase*) framework.Gui.GetAddonByName("_TargetInfoMainTarget", 1).Address;
                tTextNode = (AtkTextNode*) maintargetui->UldManager.NodeList[8];
                ttTextNode = (AtkTextNode*) maintargetui->UldManager.NodeList[12];
                focusTextNode =
                    (AtkTextNode*) ((AtkUnitBase*) framework.Gui.GetAddonByName("_FocusTargetInfo", 1).Address)->
                    UldManager.NodeList[10];
            }
            catch (Exception e)
            {
                SimpleLog.Error(e);
            }

            player = PluginInterface.ClientState.Actors[0].Address;

            RefreshHooks();

            PluginInterface.Framework.OnUpdateEvent -= FindAddress;
        }

        private void UpdatePartylist()
        {
            count = groupManager->MemberCount;
            switch (count)
            {
                case 0:
                {
                    var localplayer = Marshal.PtrToStructure<Actor>(player);
                    partynames[0] = localplayer.Name;
                    partyjobs[0] = jobStrings[localplayer.ClassJob];
                    count = 1;
                    break;
                }
                default:
                {
                    for (var i = 0; i < count; i++)
                    {
                        partynames[i] =
                            Plugin.Common.ReadSeString(partyMembers[i].Name).ToString(); //Name from partyList
                        partyjobs[i] = jobStrings[partyMembers[i].ClassJob];
                    }

                    break;
                }
            }
        }

        private void UpdatePartyUi(bool run)
        {
            if (partyNode == null) return;
            var tempNode = partyNode;
            UpdatePartylist();
            for (var i = 0; i < 8; i++)
            {
                if (!tempNode->AtkResNode.IsVisible) break; //Need test! textnodes seems to change visibility
                tempNode = (AtkComponentNode*) tempNode->AtkResNode.NextSiblingNode;
                var str = Plugin.Common.ReadSeString(textsNodes[i]->NodeText.StringPtr).ToString(); //UI string
                if (str == "") break;
                SplitString(str, true, out var lvl, out var namejob);
                var index = Array.IndexOf(jobStrings, namejob);
                if (index == -1) namecahce[i] = namejob; //namejob is a name

                if (!run)
                {
                    WriteSeString(textsNodes[i], lvl + " " + namecahce[i]);
                    continue;
                }

                var pos = Array.IndexOf(partynames, index == -1 ? namejob : namecahce[i], 0, count);
                var job = pos switch
                {
                    -1 => jobStrings[0],
                    _ => partyjobs[pos]
                };

                if (namejob != job) WriteSeString(textsNodes[i], lvl + " " + job);
            }
        }

        private void UpdateTarget()
        {
            try
            {
                SplitString(Plugin.Common.ReadSeString(tTextNode->NodeText.StringPtr).ToString(), false, out var tname,
                    out _);
                var ttname = Plugin.Common.ReadSeString(ttTextNode->NodeText.StringPtr).ToString();

                var index = Array.IndexOf(partynames, tname, 0, count);
                if (index != -1) WriteSeString(tTextNode, partyjobs[index]);

                index = Array.IndexOf(partynames, ttname, 0, count);
                if (index != -1) WriteSeString(ttTextNode, partyjobs[index]);
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
                SplitString(Plugin.Common.ReadSeString(focusTextNode->NodeText.StringPtr).ToString(), true, out var lvl,
                    out var fname);
                if (fname != "")
                {
                    var index = Array.IndexOf(partynames, fname, 0, count);

                    if (index != -1) WriteSeString(focusTextNode, lvl + " " + partyjobs[index]);
                }
            }
            catch (Exception e)
            {
                SimpleLog.Error(e);
            }
        }


        #region Framework

        private void Onlogout(object sender, EventArgs e)
        {
            PluginInterface.Framework.OnUpdateEvent += FindAddress;
            DisposeHooks();
        }

        public override void Enable()
        {
            if (Enabled) return;
            PluginInterface.Framework.OnUpdateEvent += FindAddress;
            PluginInterface.ClientState.OnLogout += Onlogout;
            Enabled = true;
        }

        public override void Disable()
        {
            if (!Enabled) return;
            PluginInterface.Framework.OnUpdateEvent -= FindAddress;
            PluginInterface.ClientState.OnLogout -= Onlogout;
            DisableHooks();
            //if (PluginInterface.ClientState.Actors[0] == null) UpdatePartyUi(false);
            SimpleLog.Debug($"[{GetType().Name}] Reset");
            Enabled = false;
        }


        public override void Dispose()
        {
            PluginInterface.Framework.OnUpdateEvent -= FindAddress;
            PluginInterface.ClientState.OnLogout -= Onlogout;
            DisposeHooks();
            Enabled = false;
            Ready = false;
            SimpleLog.Debug($"[{GetType().Name}] Disposed");
        }

        #endregion
    }
}