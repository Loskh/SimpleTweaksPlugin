﻿using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using Dalamud;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.ClientState.Objects.Types;
using SimpleTweaksPlugin.Helper;
using SimpleTweaksPlugin.TweakSystem;

namespace SimpleTweaksPlugin.Tweaks {
    public class FixTarget : Tweak {
        public override string Name => "Fix '/target' command";
        public override string Description => "Allows using the default '/target' command for targeting players or NPCs by their names.";

        private Regex regex;
        
        public override void Enable() {
            
            regex = External.ClientState.ClientLanguage switch {
                ClientLanguage.Japanese => new Regex(@"^\d+?番目のターゲット名の指定が正しくありません。： (.+)$"),
                ClientLanguage.German => new Regex(@"^Der Unterbefehl \[Name des Ziels\] an der \d+\. Stelle des Textkommandos \((.+)\) ist fehlerhaft\.$"),
                ClientLanguage.French => new Regex(@"^Le \d+er? argument “nom de la cible” est incorrect (.*?)\.$"), 
                ClientLanguage.English => new Regex(@"^“(.+)” is not a valid target name\.$"),
                ClientLanguage.ChineseSimplified => new Regex(@"^“(.+)”出现问题：\d+?号指定的目标名不正确。$"),
                _ => null
            };
            
            External.Chat.ChatMessage += OnChatMessage;
            
            base.Enable();
        }

        public override void Disable() {
            External.Chat.ChatMessage -= OnChatMessage;
            base.Disable();
        }
        
        private unsafe void OnChatMessage(XivChatType type, uint senderid, ref SeString sender, ref SeString message, ref bool isHandled) {
            if (type != XivChatType.ErrorMessage) return;
            var lastCommandStr = Encoding.UTF8.GetString(Common.LastCommand->StringPtr, (int) Common.LastCommand->BufUsed);
            if (!(lastCommandStr.StartsWith("/target ") || lastCommandStr.StartsWith("/ziel ") || lastCommandStr.StartsWith("/cibler ") || lastCommandStr.StartsWith("/选中 "))) {
                return;
            }

            var match = regex.Match(message.TextValue);
            if (!match.Success) return;
            var searchName = match.Groups[1].Value.ToLowerInvariant();

            GameObject closestMatch = null;
            var closestDistance = float.MaxValue;
            var player = External.ClientState.LocalPlayer;
            foreach (var actor in External.Objects) {
                
                if (actor == null) continue;
                if (actor.Name.TextValue.ToLowerInvariant().Contains(searchName)) {
                    var distance = Vector3.Distance(player.Position, actor.Position);
                    if (closestMatch == null) {
                        closestMatch = actor;
                        closestDistance = distance;
                        continue;
                    }

                    if (closestDistance > distance) {
                        closestMatch = actor;
                        closestDistance = distance;
                    }
                }
            }

            if (closestMatch != null) {
                isHandled = true;
                External.Targets.SetTarget(closestMatch);
            }
        }
    }
}
