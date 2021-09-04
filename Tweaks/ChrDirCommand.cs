﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Dalamud;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Game.Command;
using ImGuiNET;
using SimpleTweaksPlugin.Enums;
using SimpleTweaksPlugin.TweakSystem;

namespace SimpleTweaksPlugin.Tweaks {
    public class ChrDirCommand : Tweak {
        public override string Name => "Character Directory Command";
        public override string Description => "Adds a command to open the directory when client side character data is stored.";

        public override void Setup() {
            Ready = true;
        }

        private DalamudLinkPayload linkPayload;

        public override void Enable() {
            if (Enabled) return;
            External.Commands.AddHandler("/chrdir", new CommandInfo(CommandHandler) {ShowInHelp = true, HelpMessage = "Print your character save directory to chat. '/chrdir open' to open the directory in explorer."});

            linkPayload = PluginInterface.AddChatLinkHandler((uint) LinkHandlerId.OpenFolderLink, OpenFolder);
            
            Enabled = true;
        }

        private void OpenFolder(uint arg1, SeString arg2) {
            Process.Start("explorer.exe", arg2.TextValue);
        }

        private void CommandHandler(string command, string arguments)
        {
            var saveDir = Path.Combine(External.ClientState.ClientLanguage == ClientLanguage.ChineseSimplified ? Process.GetCurrentProcess().MainModule.FileName.Replace("ffxiv_dx11.exe","").Replace("ffxiv.exe", "") : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "FINAL FANTASY XIV - A Realm Reborn", $"FFXIV_CHR{External.ClientState.LocalContentId:X16}");
            if (arguments == "open") {
                Process.Start("explorer.exe", saveDir);
                return;
            }

            External.Chat.PrintChat(new XivChatEntry() {
                Message= new SeString(new List<Payload>() {
                    new TextPayload("Character Directory:\n"),
                    new UIForegroundPayload(22),
                    linkPayload,
                    new TextPayload(saveDir),
                    RawPayload.LinkTerminator,
                    new UIForegroundPayload(0)
                })
            });
        }

        protected override DrawConfigDelegate DrawConfigTree => (ref bool _) => {
            ImGui.TextDisabled("/chrdir");
            ImGui.TextDisabled("/chrdir open");
        };

        public override void Disable() {
            PluginInterface.RemoveChatLinkHandler((uint) LinkHandlerId.OpenFolderLink);
            External.Commands.RemoveHandler("/chrdir");
            Enabled = false;
        }

        public override void Dispose() {
            Enabled = false;
            Ready = false;
        }
    }
}
