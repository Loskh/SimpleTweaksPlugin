﻿using System;
using Dalamud.Game.Internal;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using SimpleTweaksPlugin.Tweaks.UiAdjustment;
using SimpleTweaksPlugin.TweakSystem;

namespace SimpleTweaksPlugin {
    public partial class UiAdjustmentsConfig {
        public bool ShouldSerializeHideAchievementsNotifications() => HideAchievementsNotifications != null;
        public HideAchievementsNotifications.Configs HideAchievementsNotifications = null;
    }
}

namespace SimpleTweaksPlugin.Tweaks.UiAdjustment {
    public class HideAchievementsNotifications : UiAdjustments.SubTweak {
        public class Configs : TweakConfig {
            public bool HideLogIn = true;
            public bool HideZoneIn = true;
        }

        public Configs Config { get; private set; }

        protected override DrawConfigDelegate DrawConfigTree => (ref bool hasChanged) => {
            hasChanged |= ImGui.Checkbox("隐藏登录通知", ref this.Config.HideLogIn);
            hasChanged |= ImGui.Checkbox("隐藏切换区域通知", ref this.Config.HideZoneIn);
        };

        public override string Name => "隐藏接近达成成就提示";
        public override string Description => "完全隐藏登录或切换区域时绿色带倒计时的接近达成成就的通知";
        protected override string Author => "Anna";

        public override void Enable() {
            Config = LoadConfig<Configs>() ?? PluginConfig.UiAdjustments.HideAchievementsNotifications ?? new Configs();
            this.Plugin.PluginInterface.Framework.OnUpdateEvent += this.HideNotifications;
            base.Enable();
        }

        public override void Disable() {
            SaveConfig(Config);
            PluginConfig.UiAdjustments.HideAchievementsNotifications = null;
            this.Plugin.PluginInterface.Framework.OnUpdateEvent -= this.HideNotifications;
            base.Disable();
        }

        private const int VisibilityFlag = 1 << 5;

        private void HideNotifications(Framework framework) {
            if (this.Config.HideLogIn) {
                this.HideNotification("_NotificationAchieveLogIn");
            }

            if (this.Config.HideZoneIn) {
                this.HideNotification("_NotificationAchieveZoneIn");
            }
        }

        private unsafe void HideNotification(string name) {
            var dalamudAddon = this.Plugin.PluginInterface.Framework.Gui.GetAddonByName(name, 1);
            if (dalamudAddon == null || dalamudAddon.Address == IntPtr.Zero) {
                return;
            }

            try {
                var atkUnitBase = (AtkUnitBase*) dalamudAddon.Address;
                atkUnitBase->Flags = (byte) (atkUnitBase->Flags & ~VisibilityFlag);
            } catch (Exception) {
                // ignore
            }
        }
    }
}
