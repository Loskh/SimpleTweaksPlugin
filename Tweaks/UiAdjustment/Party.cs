using System;
using System.Collections.Generic;
using Dalamud.Game.Internal;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.FFXIV.Group;
using SimpleTweaksPlugin.Tweaks.UiAdjustment;
using SeString = Dalamud.Game.Text.SeStringHandling.SeString;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using ImGuiNET;

namespace SimpleTweaksPlugin
{
    public partial class UiAdjustmentsConfig
    {
        public HidePartyNames.Config HidePartyNames = new();
    }
}

namespace SimpleTweaksPlugin.Tweaks.UiAdjustment
{
    public unsafe class HidePartyNames : UiAdjustments.SubTweak
    {
        public class Config
        {
            // int HideParty = 0;
        }

        private string[] jobStrings = new string[]
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


        private GroupManager* groupManager;
        private PartyMember* partyMembers;
        private AtkUnitBase* partyPointer;
        private AtkResNode* partyNode;
        private AtkTextNode*[] textsNodes = new AtkTextNode*[8];
        private AtkResNode* tempNode;

        public override string Name => "隐藏组队界面角色名";
        public override string Description => "隐藏组队界面角色名";


        /*
         protected override DrawConfigDelegate DrawConfigTree => (ref bool changed) =>
        {
               
        };
        */

        private void OnFrameworkUpdate(Framework framework)
        {
            try
            {
                FindString(framework, true);
            }
            catch (Exception ex)
            {
                Plugin.Error(this, ex);
            }
        }

        private unsafe void FindString(Framework framework, bool run)
        {
            var count = 0;
            if (groupManager == null)
            {
                groupManager = (GroupManager*) Plugin.PluginInterface.TargetModuleScanner.GetStaticAddressFromSig(
                    "48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 80 B8 ?? ?? ?? ?? ?? 76 50");
                partyMembers = (PartyMember*) groupManager->PartyMembers;
                count = groupManager->MemberCount;
            }


            if (partyNode == null)
            {
                partyPointer = (AtkUnitBase*) (framework.Gui.GetAddonByName("_PartyList", 1).Address);
                partyNode =
                    partyPointer->RootNode->ChildNode->PrevSiblingNode->PrevSiblingNode->PrevSiblingNode->ChildNode;
                while (partyNode->PrevSiblingNode != null) partyNode = partyNode->PrevSiblingNode;
                tempNode = partyNode; //PartylistUI.player<1>.textnode 
                for (var i = 0; i < 8; i++)
                {
                    var comp = (AtkComponentNode*) tempNode;
                    var child = comp->Component->UldManager.RootNode;
                    for (var j = 0; j <= 11; j++) child = child->PrevSiblingNode;
                    var text = (AtkTextNode*) child->ChildNode;
                    textsNodes[i] = text;
                    tempNode = tempNode->NextSiblingNode;
                }
            }

            tempNode = partyNode;

            for (var i = 0; i < 8; i++)
            {
                var str = Plugin.Common.ReadSeString(textsNodes[i]->NodeText.StringPtr).ToString(); //UI string
                var name = Plugin.Common.ReadSeString(partyMembers[i].Name).ToString(); //Name from partyMembers
                string job ; //jobName 
                switch (i)
                {
                    case 0:
                        job = jobStrings[PluginInterface.ClientState.LocalPlayer.ClassJob.Id];
                        break;
                    default:
                        job = jobStrings[partyMembers[i].ClassJob];
                        break;
                }

                if (CombineStrings(str, job).ToString() == str) continue;
                if (str.IndexOf(' ') <= 0) continue;
                SimpleLog.Information(str+job);
                switch (i)
                {
                    //local player
                    case 0:
                        Plugin.Common.WriteSeString(textsNodes[i]->NodeText,
                            CombineStrings(str, run
                                ? jobStrings[PluginInterface.ClientState.LocalPlayer.ClassJob.Id]
                                : PluginInterface.ClientState.LocalPlayer.Name));
                        break;

                    //not-local player
                    case > 0 when (tempNode->IsVisible): //visible
                        Plugin.Common.WriteSeString(textsNodes[i]->NodeText, CombineStrings(str, job));
                        break;

                    default: //invisible
                        Plugin.Common.WriteSeString(textsNodes[i]->NodeText, new SeString(new List<Payload>()));
                        break;
                }

                tempNode = tempNode->NextSiblingNode;
            }
        }

        private static SeString CombineStrings(string payload1, string payload2)
        {
            var tar = new SeString(new List<Payload>());
            tar.Payloads.Add(item: new TextPayload(payload1.Substring(0, payload1.IndexOf(' ') + 1) + payload2));
            return tar;
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
            FindString(PluginInterface.Framework, false);
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