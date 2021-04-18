using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Game.Internal;
using Dalamud.Game.ClientState;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.FFXIV.Group;
using ImGuiNET;
using ImGuiScene;
using Lumina.Excel.GeneratedSheets;
using Lumina.Text;
using SimpleTweaksPlugin.Tweaks.UiAdjustment;
using static SimpleTweaksPlugin.Tweaks.UiAdjustments.Step;
using Addon = Dalamud.Game.Internal.Gui.Addon.Addon;
using SeString = Dalamud.Game.Text.SeStringHandling.SeString;
using System.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;

namespace SimpleTweaksPlugin
{
    public partial class UiAdjustmentsConfig
    {
        public HidePartyNames.Config HidePartyNames = new HidePartyNames.Config();
    }
}

namespace SimpleTweaksPlugin.Tweaks.UiAdjustment
{
    public class HidePartyNames : UiAdjustments.SubTweak
    {
        public class Config
        {
            // int HideParty = 0;
        }

        private string[] jobStrings = new string[40]
        {
            "ADV",
            "GLA",
            "PUG",
            "MRD",
            "LNC",
            "ARC",
            "CNJ",
            "THM",
            "CRP",
            "BSM",
            "ARM",
            "GSM",
            "LTW",
            "WVR",
            "ALC",
            "CUL",
            "MIN",
            "BOT",
            "FSH",
            "PLD",
            "MNK",
            "WAR",
            "DRG",
            "BRD",
            "WHM",
            "BLM",
            "ACN",
            "SMN",
            "SCH",
            "ROG",
            "NIN",
            "MCH",
            "DRK",
            "AST",
            "SAM",
            "RDM",
            "BLU",
            "GNB",
            "DNC",
            "UNKNOWN"
        };

        private string[,] strcache = new string[8,2];
        private byte[] jobcache = new byte[8];

        public override string Name => "隐藏组队界面角色名";
        public override string Description => "隐藏组队界面角色名";


        protected override DrawConfigDelegate DrawConfigTree => (ref bool changed) =>
        {
            /*
            var bSize = buttonSize * ImGui.GetIO().FontGlobalScale;
            ImGui.SetNextItemWidth(90 * ImGui.GetIO().FontGlobalScale);
            if (ImGui.InputInt($"###{GetType().Name}_Offset", ref PluginConfig.UiAdjustments.HidePartyNames.Offset)) {
                if (PluginConfig.UiAdjustments.HidePartyNames.Offset > MaxOffset) PluginConfig.UiAdjustments.HidePartyNames.Offset = MaxOffset;
                if (PluginConfig.UiAdjustments.HidePartyNames.Offset < MinOffset) PluginConfig.UiAdjustments.HidePartyNames.Offset = MinOffset;
                changed = true;
            }
            ImGui.SameLine();
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(2));
            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button($"{(char)FontAwesomeIcon.ArrowUp}", bSize)) {
                PluginConfig.UiAdjustments.HidePartyNames.Offset = 8;
                changed = true;
            }
            ImGui.PopFont();
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("显示在进度条上方");

            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button($"{(char) FontAwesomeIcon.CircleNotch}", bSize)) {
                PluginConfig.UiAdjustments.HidePartyNames.Offset = 24;
                changed = true;
            }
            ImGui.PopFont();
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("初始位置");

            
            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button($"{(char)FontAwesomeIcon.ArrowDown}", bSize)) {
                PluginConfig.UiAdjustments.HidePartyNames.Offset = 32;
                changed = true;
            }
            ImGui.PopFont();
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("显示在进度条下方");
            ImGui.PopStyleVar();
            ImGui.SameLine();
            ImGui.Text("垂直偏移量");
            */
        };

        public void OnFrameworkUpdate(Framework framework)
        {
            try
            {
                FindString(framework);
            }
            catch (Exception ex)
            {
                Plugin.Error(this, ex);
            }
        }

        private unsafe void FindString(Framework framework)
        {
            var groupManager =
                (GroupManager*) Plugin.PluginInterface.TargetModuleScanner.GetStaticAddressFromSig(
                    "48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 80 B8 ?? ?? ?? ?? ?? 76 50");
            var partyMembers = (PartyMember*) groupManager->PartyMembers;
            var party = framework.Gui.GetAddonByName("_PartyList", 1);
            var partyPointer = (AtkUnitBase*) (party.Address);
            var node = partyPointer->RootNode->ChildNode->PrevSiblingNode->PrevSiblingNode->PrevSiblingNode->ChildNode;
            while (node->PrevSiblingNode != null) node = node->PrevSiblingNode;
            for (var i = 0; i < 8; i++)
            {
                var comp = (AtkComponentNode*) node;
                var child = comp->Component->UldManager.RootNode;
                for (var j = 0; j <= 11; j++) child = child->PrevSiblingNode;
                var text = (AtkTextNode*) child->ChildNode;
                var sestr = Plugin.Common.ReadSeString(text->NodeText.StringPtr);
                //if (sestr.ToString() == "") continue;
                var name = Plugin.Common.ReadSeString(partyMembers[i].Name);
                var job = partyMembers[i].ClassJob;

                strcache[i, 0] = name.ToString();
                strcache[i, 1] = sestr.ToString();
                jobcache[i] = job;

                if (job >= 0)
                {
                    var str = sestr.ToString();
                    if (str.IndexOf(' ') <= 0) continue;
                    var tar = new SeString(new List<Payload>());
                    tar.Payloads.Add(item: new TextPayload(str.Substring(0, str.IndexOf(' ') + 1) + jobStrings[job]));
                    Plugin.Common.WriteSeString(text->NodeText, tar);
                    SimpleLog.Information(Plugin.Common.ReadSeString(text->NodeText.StringPtr));
                }

                node = node->NextSiblingNode;
            }
        }


        private unsafe void HandleFocusTargetInfo(Addon addon, bool reset = false)
        {
            var addonStruct = (AtkUnitBase*) (addon.Address);
            if (addonStruct->RootNode == null) return;


            var rootNode = addonStruct->RootNode;
            if (rootNode->ChildNode == null) return;
            var child = rootNode->ChildNode;
            for (var i = 0; i < 6; i++)
            {
                if (child->PrevSiblingNode == null) return;
                child = child->PrevSiblingNode;
            }
        }


        public override void Enable()
        {
            if (Enabled) return;
            PluginInterface.Framework.OnUpdateEvent += OnFrameworkUpdate;
            Enabled = true;
        }

        public override void Disable()
        {
            if (!Enabled) return;
            PluginInterface.Framework.OnUpdateEvent -= OnFrameworkUpdate;
            SimpleLog.Debug($"[{GetType().Name}] Reset");
            //HandleBars(PluginInterface.Framework, true);
            Enabled = false;
        }

        public override void Dispose()
        {
            PluginInterface.Framework.OnUpdateEvent -= OnFrameworkUpdate;
            Enabled = false;
            Ready = false;
        }
    }
}