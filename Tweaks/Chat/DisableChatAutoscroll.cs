using System;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.UI;
using ImGuiNET;
using SimpleTweaksPlugin.Helper;
using SimpleTweaksPlugin.Tweaks.Chat;
using SimpleTweaksPlugin.TweakSystem;

namespace SimpleTweaksPlugin {
    public partial class ChatTweaksConfig {
        public bool ShouldSerializeDisableChatAutoscroll() => DisableChatAutoscroll != null;
        public DisableChatAutoscroll.Configs DisableChatAutoscroll = null;
    }
}

namespace SimpleTweaksPlugin.Tweaks.Chat {
    public unsafe class DisableChatAutoscroll : ChatTweaks.SubTweak {
        public override string Name => "智能滚动聊天框";
        public override string Description => "查看既往聊天记录时不会因收到新消息而自动滚动";

        public class Configs : TweakConfig {
            public bool DisablePanel0;
            public bool DisablePanel1;
            public bool DisablePanel2;
            public bool DisablePanel3;
        }

        public Configs Config { get; private set; }
        
        protected override DrawConfigDelegate DrawConfigTree => (ref bool hasChanged) => {
            ImGui.Text("总是允许自动滚动:");
            ImGui.Indent();
            hasChanged |= ImGui.Checkbox("聊天框 1##allowAutoscroll", ref Config.DisablePanel0);
            hasChanged |= ImGui.Checkbox("聊天框 2##allowAutoscroll", ref Config.DisablePanel1);
            hasChanged |= ImGui.Checkbox("聊天框 3##allowAutoscroll", ref Config.DisablePanel2);
            hasChanged |= ImGui.Checkbox("聊天框 4##allowAutoscroll", ref Config.DisablePanel3);
            ImGui.Unindent();
        };
        
        private delegate void* ScrollToBottomDelegate(void* a1);

        private HookWrapper<ScrollToBottomDelegate> scrollToBottomHook;
        
        public override void Enable() {
            Config = LoadConfig<Configs>() ?? PluginConfig.ChatTweaks.DisableChatAutoscroll ?? new Configs();
            scrollToBottomHook = Common.Hook<ScrollToBottomDelegate>("E8 ?? ?? ?? ?? 48 85 FF 75 0D", ScrollToBottomDetour);
            base.Enable();
        }

        private void* ScrollToBottomDetour(void* a1) {
            try {
                var panel = (AddonChatLogPanel*) ((ulong) a1 - 0x268);
                var name = Helper.Common.PtrToUTF8(new IntPtr(panel->AtkUnitBase.Name));
                if (!string.IsNullOrEmpty(name)) {
                    var isEnabled = name switch {
                        "ChatLogPanel_0" => !Config.DisablePanel0,
                        "ChatLogPanel_1" => !Config.DisablePanel1,
                        "ChatLogPanel_2" => !Config.DisablePanel2,
                        "ChatLogPanel_3" => !Config.DisablePanel3,
                        _ => false
                    };

                    if (isEnabled && panel->IsScrolledBottom == 0) {
                        SimpleLog.Verbose($"Prevented Autoscroll in: {name}");
                        return null;
                    }
                }
                return scrollToBottomHook.Original(a1);
            } catch {
                return scrollToBottomHook.Original(a1);
            }
        }

        public override void Disable() {
            SaveConfig(Config);
            PluginConfig.ChatTweaks.DisableChatAutoscroll = null;
            scrollToBottomHook?.Disable();
            base.Disable();
        }

        public override void Dispose() {
            scrollToBottomHook?.Dispose();
            base.Dispose();
        }

    }
}
