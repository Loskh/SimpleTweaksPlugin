﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using SimpleTweaksPlugin.GameStructs;
using SimpleTweaksPlugin.TweakSystem;

namespace SimpleTweaksPlugin {
    public partial class TooltipTweakConfig {
        public bool ShouldSerializeFoodStatsHighlight() => false;
        public bool FoodStatsHighlight = false;
    }
}

namespace SimpleTweaksPlugin.Tweaks.Tooltips {
    public class FoodStats : TooltipTweaks.SubTweak {
        public override string Name => "显示食物/药品期望值";
        public override string Description => "基于角色当前属性 计算食物/药品属性增加量的期望值";

        private IntPtr playerStaticAddress;
        private IntPtr getBaseParamAddress;
        private delegate ulong GetBaseParam(IntPtr playerAddress, uint baseParamId);
        private GetBaseParam getBaseParam;

        public class Configs : TweakConfig {
            public bool Highlight = false;
        }
        
        public Configs Config { get; private set; }
        

        public override void Setup() {
            try {
                if (getBaseParamAddress == IntPtr.Zero) {
                    getBaseParamAddress = PluginInterface.TargetModuleScanner.ScanText("E8 ?? ?? ?? ?? 44 8B C0 33 D2 48 8B CB E8 ?? ?? ?? ?? BA ?? ?? ?? ?? 48 8D 0D");
                    getBaseParam = Marshal.GetDelegateForFunctionPointer<GetBaseParam>(getBaseParamAddress);
                }

                if (playerStaticAddress == IntPtr.Zero) {
                    playerStaticAddress = PluginInterface.TargetModuleScanner.GetStaticAddressFromSig("8B D7 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 0F B7 E8");
                }
                base.Setup();
            } catch (Exception ex) {
                Plugin.Error(this, ex);
            }
        }

        public override void Enable() {
            Config = LoadConfig<Configs>() ?? new Configs() { Highlight = PluginConfig.TooltipTweaks.FoodStatsHighlight };
            base.Enable();
        }

        public override void Disable() {
            SaveConfig(Config);
            base.Disable();
        }

        protected override DrawConfigDelegate DrawConfigTree => (ref bool hasChanged) => {
            hasChanged |= ImGui.Checkbox("高亮显示", ref Config.Highlight);
        };

        public override void OnItemTooltip(TooltipTweaks.ItemTooltip tooltip, InventoryItem itemInfo) {

            var id = PluginInterface.Framework.Gui.HoveredItem;

            if (id < 2000000) {
                var hq = id >= 500000;
                id %= 500000;
                var item = PluginInterface.Data.Excel.GetSheet<Sheets.ExtendedItem>().GetRow((uint)id);

                var action = item.ItemAction?.Value;

                if (action != null && (action.Type == 844 || action.Type == 845 || action.Type == 846)) {

                    var itemFood = PluginInterface.Data.Excel.GetSheet<ItemFood>().GetRow(hq ? action.DataHQ[1] : action.Data[1]);
                    if (itemFood != null) {
                        var payloads = new List<Payload>();
                        var hasChange = false;

                        foreach (var bonus in itemFood.UnkStruct1) {
                            if (bonus.BaseParam == 0) continue;
                            var param = PluginInterface.Data.Excel.GetSheet<BaseParam>().GetRow(bonus.BaseParam);
                            var value = hq ? bonus.ValueHQ : bonus.Value;
                            var max = hq ? bonus.MaxHQ : bonus.Max;
                            if (bonus.IsRelative) {
                                hasChange = true;

                                var currentStat = getBaseParam(playerStaticAddress, bonus.BaseParam);
                                var relativeAdd = (short)(currentStat * (value / 100f));
                                var change = relativeAdd > max ? max : relativeAdd;

                                if (payloads.Count > 0) payloads.Add(new TextPayload("\n"));

                                payloads.Add(new TextPayload($"{param.Name} +"));

                                if (Config.Highlight && change < max) payloads.Add(new UIForegroundPayload(PluginInterface.Data, 500));
                                payloads.Add(new TextPayload($"{value}%"));
                                if (change < max) {
                                    if (Config.Highlight) payloads.Add(new UIForegroundPayload(PluginInterface.Data, 0));
                                    payloads.Add(new TextPayload($" (当前 "));
                                    if (Config.Highlight) payloads.Add(new UIForegroundPayload(PluginInterface.Data, 500));
                                    payloads.Add(new TextPayload($"{change}"));
                                    if (Config.Highlight) payloads.Add(new UIForegroundPayload(PluginInterface.Data, 0));
                                    payloads.Add(new TextPayload($")"));
                                }

                                payloads.Add(new TextPayload(" (最大 "));
                                if (Config.Highlight && change == max) payloads.Add(new UIForegroundPayload(PluginInterface.Data, 500));
                                payloads.Add(new TextPayload($"{max}"));
                                if (Config.Highlight && change == max) payloads.Add(new UIForegroundPayload(PluginInterface.Data, 0));
                                payloads.Add(new TextPayload(")"));
                            } else {
                                if (payloads.Count > 0) payloads.Add(new TextPayload("\n"));
                                payloads.Add(new TextPayload($"{param.Name} +{value}"));
                            }
                        }

                        if (payloads.Count > 0 && hasChange) {
                            var seStr = new SeString(payloads);
                            tooltip[TooltipTweaks.ItemTooltip.TooltipField.Effects] = seStr;
                        }

                    }

                }
            }
        }
    }
}
