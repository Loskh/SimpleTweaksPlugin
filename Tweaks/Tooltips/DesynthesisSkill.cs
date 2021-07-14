﻿
using System;
using System.Linq;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Lumina.Excel.GeneratedSheets;
using ImGuiNET;
using SimpleTweaksPlugin.GameStructs;
using SimpleTweaksPlugin.Helper;
using SimpleTweaksPlugin.TweakSystem;
using static SimpleTweaksPlugin.Tweaks.TooltipTweaks.ItemTooltip.TooltipField;

namespace SimpleTweaksPlugin {
    public partial class TooltipTweakConfig {
        public bool ShouldSerializeDesynthesisDelta() => false;
        public bool DesynthesisDelta = false;
    }
}

namespace SimpleTweaksPlugin.Tweaks.Tooltips {
    public class DesynthesisSkill : TooltipTweaks.SubTweak {
        public override string Name => "显示分解等级";
        public override string Description => "当你分解物品时显示你对应的分解等级";

        private readonly uint[] desynthesisInDescription = { 46, 56, 65, 66, 67, 68, 69, 70, 71, 72 };

        public class Configs : TweakConfig {
            public bool Delta = false;
        }
        
        public Configs Config { get; private set; }

        public override void Enable() {
            Config = LoadConfig<Configs>() ?? new Configs() {Delta = PluginConfig.TooltipTweaks.DesynthesisDelta};
            base.Enable();
        }

        public override void Disable() {
            SaveConfig(Config);
            base.Disable();
        }
        public override unsafe void OnItemTooltip(TooltipTweaks.ItemTooltip tooltip, InventoryItem itemInfo) {

            var id = PluginInterface.Framework.Gui.HoveredItem;
            if (id < 2000000) {
                id %= 500000;

                var item = PluginInterface.Data.Excel.GetSheet<Sheets.ExtendedItem>().GetRow((uint)id);
                if (item != null && item.Desynth > 0) {
                    var classJobOffset = 2 * (int)(item.ClassJobRepair.Row - 8);
                    // 5.5 0x6A6
                    var desynthLevel = *(ushort*)(Common.PlayerStaticAddress + (0x69A + classJobOffset)) / 100f;
                    var desynthDelta = item.LevelItem.Row - desynthLevel;

                    var useDescription = desynthesisInDescription.Contains(item.ItemSearchCategory.Row);

                    var seStr = tooltip[useDescription ? ItemDescription : ExtractableProjectableDesynthesizable];

                    if (seStr != null) {
                        if (seStr.Payloads.Last() is TextPayload textPayload) {
                            if (Config.Delta) {
                                textPayload.Text = textPayload.Text.Replace($"{item.LevelItem.Row},00", $"{item.LevelItem.Row} ({desynthDelta:+#;-#}");
                                textPayload.Text = textPayload.Text.Replace($"{item.LevelItem.Row}.00", $"{item.LevelItem.Row} ({desynthDelta:+#;-#})");
                            } else {
                                textPayload.Text = textPayload.Text.Replace($"{item.LevelItem.Row},00", $"{item.LevelItem.Row} ({desynthLevel:F0})");
                                textPayload.Text = textPayload.Text.Replace($"{item.LevelItem.Row}.00", $"{item.LevelItem.Row} ({desynthLevel:F0})");
                            }
                            tooltip[useDescription ? ItemDescription : ExtractableProjectableDesynthesizable] = seStr;
                        }
                    }
                }
            }
        }

        protected override DrawConfigDelegate DrawConfigTree => (ref bool hasChanged) => {
            hasChanged |= ImGui.Checkbox($"显示差值###{GetType().Name}DesynthesisDelta", ref Config.Delta);
        };
    }
}
