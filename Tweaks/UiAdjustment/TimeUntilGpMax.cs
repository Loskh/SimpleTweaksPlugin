﻿using System;
using System.Diagnostics;
using Dalamud.Game;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;
using SimpleTweaksPlugin.Helper;
using SimpleTweaksPlugin.TweakSystem;

namespace SimpleTweaksPlugin.Tweaks.UiAdjustment {
    public unsafe class TimeUntilGpMax : UiAdjustments.SubTweak {
        public override string Name => "显示GP回复时间";
        public override string Description => "显示GP恢复至满所需要的时间.";
        
        private readonly Stopwatch lastGpChangeStopwatch = new();
        private readonly Stopwatch lastUpdate = new();
        private uint lastGp = uint.MaxValue;
        private int gpPerTick = 5;
        private float timePerTick = 3f;
        public delegate void UpdateParamDelegate(uint a1, uint* a2, byte a3);
        private Hook<UpdateParamDelegate> updateParamHook;
        
        public class Configs : TweakConfig {
            [TweakConfigOption("目标 GP", EditorSize = 200, IntMin = -1, IntMax = 1000, IntType = TweakConfigOptionAttribute.IntEditType.Slider)]
            public int GpGoal = -1;
        }

        public override bool UseAutoConfig => true;

        public Configs Config { get; private set; }

        public override void Enable() {
            Config = LoadConfig<Configs>() ?? new Configs();
            lastUpdate.Restart();
            updateParamHook ??= new Hook<UpdateParamDelegate>(Common.Scanner.ScanText("48 89 5C 24 ?? 48 89 6C 24 ?? 56 48 83 EC 20 83 3D ?? ?? ?? ?? ?? 41 0F B6 E8 48 8B DA 8B F1 0F 84 ?? ?? ?? ?? 48 89 7C 24"), new UpdateParamDelegate(UpdateParamDetour));
            updateParamHook.Enable();
            Service.Framework.Update += FrameworkUpdate;
            base.Enable();
        }

        private void UpdateParamDetour(uint a1, uint* a2, byte a3) {
            updateParamHook.Original(a1, a2, a3);
            try {
                if (Service.ClientState.LocalPlayer == null) return;
                if (!lastGpChangeStopwatch.IsRunning) {
                    lastGpChangeStopwatch.Restart();
                } else {
                    if (Service.ClientState.LocalPlayer.CurrentGp > lastGp && lastGpChangeStopwatch.ElapsedMilliseconds > 1000 && lastGpChangeStopwatch.ElapsedMilliseconds < 4000) {
                        var diff = (int) Service.ClientState.LocalPlayer.CurrentGp - (int) lastGp;
                        if (diff < 20) {
                            gpPerTick = diff;
                            lastGp = Service.ClientState.LocalPlayer.CurrentGp;
                            lastGpChangeStopwatch.Restart();
                        }
                    }

                    if (Service.ClientState.LocalPlayer.CurrentGp != lastGp) {
                        lastGp = Service.ClientState.LocalPlayer.CurrentGp;
                        lastGpChangeStopwatch.Restart();
                    }
                }
            } catch (Exception ex) {
                Plugin.Error(this, ex, false, "Error in UpdateParamDetour");
            }
        }

        public override void Disable() {
            SaveConfig(Config);
            lastUpdate.Stop();
            updateParamHook?.Disable();
            Service.Framework.Update -= FrameworkUpdate;
            Update(true);
            base.Disable();
        }

        public override void Dispose() {
            updateParamHook?.Disable();
            updateParamHook?.Dispose();
            base.Dispose();
        }

        private void FrameworkUpdate(Framework framework) {
            try {
                if (!lastUpdate.IsRunning) lastUpdate.Restart();
                if (lastUpdate.ElapsedMilliseconds < 1000) return;
                lastUpdate.Restart();
                Update();
            } catch (Exception ex) {
                SimpleLog.Error(ex);
            }
        }

        private void Update(bool reset = false) {
            if (Service.ClientState?.LocalPlayer?.ClassJob?.GameData?.ClassJobCategory?.Row != 32) reset = true;
            var paramWidget = Common.GetUnitBase("_ParameterWidget");
            if (paramWidget == null) return;

            var gatheringWidget = Common.GetUnitBase("Gathering");
            if (gatheringWidget == null) gatheringWidget = Common.GetUnitBase("GatheringMasterpiece");
            
            AtkTextNode* textNode = null;
            for (var i = 0; i < paramWidget->UldManager.NodeListCount; i++) {
                if (paramWidget->UldManager.NodeList[i] == null) continue;
                if (paramWidget->UldManager.NodeList[i]->NodeID == CustomNodes.TimeUntilGpMax) {
                    textNode = (AtkTextNode*)paramWidget->UldManager.NodeList[i];
                    if (reset) {
                        paramWidget->UldManager.NodeList[i]->ToggleVisibility(false);
                        continue;
                    }
                    break;
                }
            }

            if (textNode == null && reset) return;

            if (textNode == null) {

                var newTextNode = (AtkTextNode*)IMemorySpace.GetUISpace()->Malloc((ulong)sizeof(AtkTextNode), 8);
                if (newTextNode != null) {

                    var lastNode = paramWidget->RootNode;
                    if (lastNode == null) return;

                    IMemorySpace.Memset(newTextNode, 0, (ulong)sizeof(AtkTextNode));
                    newTextNode->Ctor();
                    textNode = newTextNode;

                    newTextNode->AtkResNode.Type = NodeType.Text;
                    newTextNode->AtkResNode.Flags = (short)(NodeFlags.AnchorLeft | NodeFlags.AnchorTop);
                    newTextNode->AtkResNode.DrawFlags = 0;
                    newTextNode->AtkResNode.SetPositionShort(210, 1);
                    newTextNode->AtkResNode.SetWidth(200);
                    newTextNode->AtkResNode.SetHeight(14);

                    newTextNode->LineSpacing = 24;
                    newTextNode->AlignmentFontType = 0x15;
                    newTextNode->FontSize = 12;
                    newTextNode->TextFlags = (byte)(TextFlags.Edge);
                    newTextNode->TextFlags2 = 0;

                    newTextNode->AtkResNode.NodeID = CustomNodes.TimeUntilGpMax;

                    newTextNode->AtkResNode.Color.A = 0xFF;
                    newTextNode->AtkResNode.Color.R = 0xFF;
                    newTextNode->AtkResNode.Color.G = 0xFF;
                    newTextNode->AtkResNode.Color.B = 0xFF;

                    if (lastNode->ChildNode != null) {
                        lastNode = lastNode->ChildNode;
                        while (lastNode->PrevSiblingNode != null) {
                            lastNode = lastNode->PrevSiblingNode;
                        }

                        newTextNode->AtkResNode.NextSiblingNode = lastNode;
                        newTextNode->AtkResNode.ParentNode = paramWidget->RootNode;
                        lastNode->PrevSiblingNode = (AtkResNode*) newTextNode;
                    } else {
                        lastNode->ChildNode = (AtkResNode*)newTextNode;
                        newTextNode->AtkResNode.ParentNode = lastNode;
                    }

                    textNode->TextColor.A = 0xFF;
                    textNode->TextColor.R = 0xFF;
                    textNode->TextColor.G = 0xFF;
                    textNode->TextColor.B = 0xFF;

                    textNode->EdgeColor.A = 0xFF;
                    textNode->EdgeColor.R = 0xF0;
                    textNode->EdgeColor.G = 0x8E;
                    textNode->EdgeColor.B = 0x37;

                    paramWidget->UldManager.UpdateDrawNodeList();
                }
            }

            if (reset) {
                textNode->AtkResNode.ToggleVisibility(false);
                return;
            }

            var targetGp = Config.GpGoal > 0 ? Math.Min(Config.GpGoal, Service.ClientState.LocalPlayer.MaxGp) : Service.ClientState.LocalPlayer.MaxGp;
            if (targetGp - Service.ClientState.LocalPlayer.CurrentGp > 0) {
                textNode->AtkResNode.ToggleVisibility(true);

                var gpPerSecond = gpPerTick / timePerTick;
                var secondsUntilFull = (targetGp - Service.ClientState.LocalPlayer.CurrentGp) / gpPerSecond;

                if (gatheringWidget == null) {
                    secondsUntilFull += timePerTick - (float)lastGpChangeStopwatch.Elapsed.TotalSeconds;
                } else {
                    lastGpChangeStopwatch.Restart();
                }
            
                var minutesUntilFull = 0;
                while (secondsUntilFull >= 60) {
                    minutesUntilFull += 1;
                    secondsUntilFull -= 60;
                }
                textNode->SetText($"{minutesUntilFull:00}:{(int)secondsUntilFull:00}");
            } else {
                UiHelper.Hide(textNode);
            }
        }
    }
}
