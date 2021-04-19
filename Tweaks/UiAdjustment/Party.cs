using System;
using System.CodeDom;
using System.Collections.Generic;
using Dalamud.Game.Internal;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.FFXIV.Group;
using SimpleTweaksPlugin.Tweaks.UiAdjustment;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin;

//using ImGuiNET;

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
        private AtkComponentNode* partyNode;
        private AtkTextNode*[] textsNodes = new AtkTextNode*[8];
        private AtkComponentNode* tempNode;
        private string[] namecahce = new string[8];
        private string[] partynames = new string[8];
        private string[] partyjobs = new string[8];
        private string localname;
        private string localjob;

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
                UpdateString(true);
            }
            catch (Exception ex)
            {
                Plugin.Error(this, ex);
            }
        }

        private void FindAddress(Framework framework)
        {
            if (PluginInterface.ClientState.LocalPlayer== null) return;
            localname = PluginInterface.ClientState.LocalPlayer.Name;

            if (groupManager == null)
            {
                groupManager = (GroupManager*) Plugin.PluginInterface.TargetModuleScanner.GetStaticAddressFromSig(
                    "48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 80 B8 ?? ?? ?? ?? ?? 76 50");
                partyMembers = (PartyMember*) groupManager->PartyMembers;

                //partyUI update handle sig"48 89 5C 24 ? 48 89 74 24 ? 57 48 83 EC ? 48 8B 7A ? 48 8B D9 49 8B 70 ? 48 8B 47"
            }


            if (partyNode == null)
            {
                partyPointer = (AtkUnitBase*) framework.Gui.GetAddonByName("_PartyList", 1).Address;
                
                partyNode = (AtkComponentNode*) partyPointer->UldManager.NodeList[17];
                if (partyNode== null) return;
                tempNode = partyNode; //PartylistUI.player<1>.atkComponetnode
                for (var i = 0; i < 8; i++)
                {
                    var text = (AtkTextNode*) tempNode->Component->UldManager.NodeList[15]->ChildNode;
                    textsNodes[i] = text;

                    tempNode = (AtkComponentNode*) tempNode->AtkResNode.NextSiblingNode;
                }
            }
            PluginInterface.Framework.OnUpdateEvent -= FindAddress;
            PluginInterface.Framework.OnUpdateEvent += OnFrameworkUpdate;

            
        }

        private int UpdatePartylist()
        {
            if (PluginInterface.ClientState.LocalPlayer != null)
            {
                localjob = jobStrings[PluginInterface.ClientState.LocalPlayer.ClassJob.Id];
            }

            var count = groupManager->MemberCount;
            switch (count)
            {
                case 0:
                {
                    count = 1;
                    partynames[0] = localname;
                    partyjobs[0] = localjob;
                    break;
                }
                default:
                {
                    for (var i = 0; i < count; i++)
                    {
                        partynames[i] = Plugin.Common.ReadSeString(partyMembers[i].Name).ToString(); //Name from partyList
                        partyjobs[i] = jobStrings[partyMembers[i].ClassJob];
                    }

                    break;
                }
            }

            return count;
        }

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

        private void UpdateString(bool run)
        {
            tempNode = partyNode;
            var partycount = UpdatePartylist();
            for (var i = 0; i < 8; i++)
            {
                if (!tempNode->AtkResNode.IsVisible) break;
                tempNode = (AtkComponentNode*) tempNode->AtkResNode.NextSiblingNode;
                var str = Plugin.Common.ReadSeString(textsNodes[i]->NodeText.StringPtr).ToString(); //UI string
                if (str == "") break;
                SplitString(str, true, out var lvl, out var namejob);
                var index = Array.IndexOf(jobStrings, namejob);
                if (index == -1) namecahce[i] = namejob; //namejob is a name

                if (!run)
                {
                    Write(textsNodes[i], lvl + " " + namecahce[i]);
                    continue;
                }

                var pos = Array.IndexOf(partynames, index == -1 ? namejob : namecahce[i], 0, partycount);
                var job = pos switch
                {
                    -1 => jobStrings[0],
                    _ => partyjobs[pos]
                };

                if (namejob != job) Write(textsNodes[i], lvl + " " + job);

            }

            if (!PluginInterface.ClientState.IsLoggedIn)
            {
                PluginInterface.Framework.OnUpdateEvent += FindAddress;
                PluginInterface.Framework.OnUpdateEvent -= OnFrameworkUpdate;
                groupManager = null;
                partyNode = null;
            }
        }


        private void Write(AtkTextNode* node, string payload)
        {
            var seString = new SeString(new List<Payload>());
            seString.Payloads.Add(new TextPayload(payload));
            Plugin.Common.WriteSeString(node->NodeText, seString);
        }


        public override void Enable()
        {
            if (Enabled) return;

            PluginInterface.Framework.OnUpdateEvent += FindAddress;
            Enabled = true;
        }

        public override void Disable()
        {
            if (!Enabled) return;
            PluginInterface.Framework.OnUpdateEvent -= FindAddress;
            PluginInterface.Framework.OnUpdateEvent -= OnFrameworkUpdate;
            SimpleLog.Debug($"[{GetType().Name}] Reset");
            UpdateString(false);
            Enabled = false;
        }
        

        public override void Dispose()
        {
            PluginInterface.Framework.OnUpdateEvent -= FindAddress;
            PluginInterface.Framework.OnUpdateEvent -= OnFrameworkUpdate;
            Enabled = false;
            Ready = false;
        }
    }
}
