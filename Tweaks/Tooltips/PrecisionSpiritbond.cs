﻿using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using SimpleTweaksPlugin.TweakSystem;
using static SimpleTweaksPlugin.Tweaks.TooltipTweaks;
using static SimpleTweaksPlugin.Tweaks.TooltipTweaks.ItemTooltipField;

namespace SimpleTweaksPlugin.Tweaks.Tooltips {
    public class PrecisionSpiritbond : SubTweak {
        public override string Name => "精炼度精确化";
        public override string Description => "显示较为精确的精炼度百分比";

        public class Configs : TweakConfig {
            public bool TrailingZero = true;
        }
        
        public Configs Config { get; private set; }

        public override void Enable() {
            Config = LoadConfig<Configs>() ?? new Configs();
            base.Enable();
        }

        public override void Disable() {
            SaveConfig(Config);
            base.Disable();
        }

        public override unsafe void OnGenerateItemTooltip(NumberArrayData* numberArrayData, StringArrayData* stringArrayData) {
            var c = GetTooltipString(stringArrayData, SpiritbondPercent);
            if (c == null || c.TextValue.StartsWith("?")) return;
            stringArrayData->SetValue((int)SpiritbondPercent, (Item.Spiritbond / 100f).ToString(Config.TrailingZero ? "F2" : "0.##") + "%", false);
        }

        protected override DrawConfigDelegate DrawConfigTree => (ref bool hasChanged) => {
            hasChanged |= ImGui.Checkbox($"显示尾随0###{GetType().Name}TrailingZeros", ref Config.TrailingZero);
        };
    }

}
