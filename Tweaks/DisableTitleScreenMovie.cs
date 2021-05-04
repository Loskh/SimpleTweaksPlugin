using Dalamud.Game.Internal;
using FFXIVClientInterface.Client.UI.Agent;
using SimpleTweaksPlugin.TweakSystem;

namespace SimpleTweaksPlugin.Tweaks {
    internal unsafe class DisableTitleScreenMovie : Tweak {
        public override string Name => "禁用开场动画";
        public override string Description => "禁用在主界面闲置过久时会播放的开场动画";

        public override void Enable() {
            PluginInterface.Framework.OnUpdateEvent += FrameworkUpdate;
            base.Enable();
        }
        
        public override void Disable() {
            PluginInterface.Framework.OnUpdateEvent -= FrameworkUpdate;
            base.Disable();
        }

        private void FrameworkUpdate(Framework framework) {
            try {
                if (PluginInterface.ClientState?.Condition == null) return;
                if (PluginInterface.ClientState.Condition.Any()) return;
                SimpleTweaksPlugin.Client.UiModule.AgentModule.GetAgent<AgentLobby>().Data->IdleTime = 0;
            } catch {
                // Ignored
            }
        }
    }
}
