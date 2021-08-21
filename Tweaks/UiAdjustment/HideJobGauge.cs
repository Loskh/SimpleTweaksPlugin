using System;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState;
using Dalamud.Game.Internal;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Component.GUI;
using SimpleTweaksPlugin.TweakSystem;

#if DEBUG
using SimpleTweaksPlugin.Debugging;
#endif

namespace SimpleTweaksPlugin.Tweaks.UiAdjustment {
    public unsafe class HideJobGauge : UiAdjustments.SubTweak {
        public override string Name => "隐藏职业量谱(爆炸中,请勿尝试)";
        public override string Description => "在非副本或战斗中隐藏职业量谱.";

        public class Configs : TweakConfig {

            [TweakConfigOption("副本中显示", 1)]
            public bool ShowInDuty;

            [TweakConfigOption("战斗中显示", 2)]
            public bool ShowInCombat;

        }

        public Configs Config { get; private set; }
        public override bool UseAutoConfig => true;
        
        public override void Enable() {
            Config = LoadConfig<Configs>() ?? new Configs();
            PluginInterface.Framework.OnUpdateEvent += FrameworkUpdate;
            base.Enable();
            Disable();
        }
        
        private void FrameworkUpdate(Framework framework) {
            try {
                Update();
            } catch {
                // 
            }
            
        }

        private void Update(bool reset = false) {
            var stage = AtkStage.GetSingleton();
            var loadedUnitsList = &stage->RaptureAtkUnitManager->AtkUnitManager.AllLoadedUnitsList;
            var addonList = &loadedUnitsList->AtkUnitEntries;
            #if DEBUG
            PerformanceMonitor.Begin();
            #endif
            for (var i = 0; i < loadedUnitsList->Count; i++) {
                var addon = addonList[i];
                var name = Marshal.PtrToStringAnsi(new IntPtr(addon->Name));
                
                if (name != null && name.StartsWith("JobHud")) {
                    if (reset || Config.ShowInDuty && PluginInterface.ClientState.Condition[ConditionFlag.BoundByDuty]) {
                        if (addon->UldManager.NodeListCount == 0) addon->UldManager.UpdateDrawNodeList();
                    } else if (Config.ShowInCombat && PluginInterface.ClientState.Condition[ConditionFlag.InCombat]) {
                        if (addon->UldManager.NodeListCount == 0) addon->UldManager.UpdateDrawNodeList();
                    } else {
                        addon->UldManager.NodeListCount = 0;
                    }
                }

            }
            #if DEBUG
            PerformanceMonitor.End();
            #endif
        }

        public override void Disable() {
            PluginInterface.Framework.OnUpdateEvent -= FrameworkUpdate;
            try {
                Update(true);
            } catch {
                //
            }
            SaveConfig(Config);
            base.Disable();
        }
    }
}
