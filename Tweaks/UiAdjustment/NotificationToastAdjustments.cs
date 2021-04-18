using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Internal;
using Dalamud.Game.Internal.Gui.Toast;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using SimpleTweaksPlugin.Helper;
using SimpleTweaksPlugin.Tweaks.UiAdjustment;

namespace SimpleTweaksPlugin {
    public partial class UiAdjustmentsConfig {
        public NotificationToastAdjustments.Configs NotificationToastAdjustments = new NotificationToastAdjustments.Configs();
    }
}

namespace SimpleTweaksPlugin.Tweaks.UiAdjustment {
    public unsafe class NotificationToastAdjustments : UiAdjustments.SubTweak {
        public override string Name => "弹出通知修改";
        public override string Description => "允许移动或隐藏在不同时间出现在屏幕中间的通知";
        protected override string Author => "Aireil";

        public class Configs {
            public bool Hide = false;
            public bool ShowInCombat = false;
            public int OffsetXPosition = 0;
            public int OffsetYPosition = 0;
            public readonly List<string> Exceptions = new List<string>();
        }

        public Configs Config => PluginConfig.UiAdjustments.NotificationToastAdjustments;

        private string newException = string.Empty;

        protected override DrawConfigDelegate DrawConfigTree => (ref bool hasChanged) => {
            hasChanged |= ImGui.Checkbox("隐藏", ref Config.Hide);
            if (Config.Hide) {
                ImGui.SameLine();
                hasChanged |= ImGui.Checkbox("战斗中显示", ref Config.ShowInCombat);
            }

            if (!Config.Hide || Config.ShowInCombat) {
                var offsetChanged = false;
                ImGui.SetNextItemWidth(100 * ImGui.GetIO().FontGlobalScale);
                offsetChanged |= ImGui.InputInt("水平偏移##offsetPosition", ref Config.OffsetXPosition, 1);
                ImGui.SetNextItemWidth(100 * ImGui.GetIO().FontGlobalScale);
                offsetChanged |= ImGui.InputInt("垂直偏移##offsetPosition", ref Config.OffsetYPosition, 1);
                if (offsetChanged)
                {
                    var toastNode = GetToastNode();
                    if (toastNode != null && !toastNode->IsVisible)
                        this.PluginInterface.Framework.Gui.Toast.ShowNormal("这是一个通知的预览");
                    hasChanged = true;
                }
            }

            if (Config.Hide) return;

            ImGui.Text("如果通知含有以下内容则隐藏:");
            for (var  i = 0; i < Config.Exceptions.Count; i++) {
                ImGui.PushID($"Exception_{i.ToString()}");
                var exception = Config.Exceptions[i];
                if (ImGui.InputText("##ToastTextException", ref exception, 500)) {
                    Config.Exceptions[i] = exception;
                    hasChanged = true;
                }
                ImGui.SameLine();
                ImGui.PushFont(UiBuilder.IconFont);
                if (ImGui.Button(FontAwesomeIcon.Trash.ToIconString())) {
                    Config.Exceptions.RemoveAt(i--);
                    hasChanged = true;
                }
                ImGui.PopFont();
                ImGui.PopID();
                if (i < 0) break;
            }
            ImGui.InputText("##NewToastTextException", ref newException, 500);
            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button(FontAwesomeIcon.Plus.ToIconString())) {
                Config.Exceptions.Add(newException);
                newException = string.Empty;
                hasChanged = true;
            }
            ImGui.PopFont();
        };

        public override void Enable() {
            PluginInterface.Framework.OnUpdateEvent += FrameworkOnUpdate;
            PluginInterface.Framework.Gui.Toast.OnToast += OnToast;
            base.Enable();
        }

        public override void Disable() {
            PluginInterface.Framework.OnUpdateEvent -= FrameworkOnUpdate;
            PluginInterface.Framework.Gui.Toast.OnToast -= OnToast;
            UpdateNotificationToastText(true);
            base.Disable();
        }

        private void FrameworkOnUpdate(Framework framework) {
            try {
                UpdateNotificationToastText();
            } catch (Exception ex) {
                SimpleLog.Error(ex);
            }
        }

        private void UpdateNotificationToastText(bool reset = false) {
            var toastNode = GetToastNode();
            if (toastNode == null) return;
            
            if (reset) {
                SetOffsetPosition(toastNode, 0.0f, 0.0f);

                return;
            }

/*
            if (!isPreviewing && !toastUnitBase->IsVisible) return; // no point continuing

            var hide = Config.Hide;

            if (Config.Exceptions.Any() && !Config.Hide && !isPreviewing) {
                // var text = Marshal.PtrToStringAnsi(new IntPtr(toastTextNode->NodeText.StringPtr));
                // fix text tranfer problem
                byte[] buffer1 = System.Text.Encoding.Default.GetBytes(Marshal.PtrToStringAnsi(new IntPtr(toastTextNode->NodeText.StringPtr)));
                byte[] buffer2 = System.Text.Encoding.Convert(System.Text.Encoding.UTF8, System.Text.Encoding.Default, buffer1, 0, buffer1.Length);
                string text = System.Text.Encoding.Default.GetString(buffer2, 0, buffer2.Length);
                hide = Config.Exceptions.Any(x => text.Contains(x));
            }

            if (Config.Hide && Config.ShowInCombat && !isPreviewing) {
                var inCombat = PluginInterface.ClientState.Condition[Dalamud.Game.ClientState.ConditionFlag.InCombat];
                if (inCombat) {
                    hide = false;
                }
            }

            if (hide && !isPreviewing) {
                UiHelper.Hide(toastBackgroundNode);
                UiHelper.Hide(toastTextNode);
            }
            else {
                UiHelper.Show(toastBackgroundNode);
                UiHelper.Show(toastTextNode);

                SetOffsetPosition(toastNode1, Config.OffsetXPosition, Config.OffsetYPosition);

                if (isPreviewing) {
                    var text = Marshal.PtrToStringAnsi(new IntPtr(toastTextNode->NodeText.StringPtr));
                    if (text == String.Empty) {
                        UiHelper.SetText(toastTextNode, "这只是一个预览，不是游戏通知");
                    }
*/
            if (!toastNode->IsVisible) return;

            SetOffsetPosition(toastNode, Config.OffsetXPosition, Config.OffsetYPosition);
        }

        private static AtkResNode* GetToastNode() {
            var toastUnitBase = Common.GetUnitBase("_WideText", 2);
            if (toastUnitBase == null) return null;
            if (toastUnitBase->UldManager.NodeList == null || toastUnitBase->UldManager.NodeListCount < 4) return null;

            return toastUnitBase->UldManager.NodeList[0];
        }

        private static void SetOffsetPosition(AtkResNode* node, float offsetX, float offsetY) {
            var defaultXPos = (ImGui.GetIO().DisplaySize.X * 1 / 2) - 512;
            var defaultYPos = (ImGui.GetIO().DisplaySize.Y * 3 / 5) - 20;

            UiHelper.SetPosition(node, defaultXPos + offsetX, defaultYPos - offsetY);
        }

        private void OnToast(ref SeString message, ref ToastOptions options, ref bool isHandled) {
            try {
                if (isHandled) return;

                if (Config.Hide) {
                    if (Config.ShowInCombat && PluginInterface.ClientState.Condition[Dalamud.Game.ClientState.ConditionFlag.InCombat])
                        return;
                } else {
                    var messageStr = message.ToString();
                    if (Config.Exceptions.All(x => !messageStr.Contains(x))) return;
                }
                
                isHandled = true;
            } catch (Exception ex) {
                SimpleLog.Error(ex);
            }
        }
    }
}