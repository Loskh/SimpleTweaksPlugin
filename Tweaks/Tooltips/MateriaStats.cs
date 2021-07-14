﻿using System;
using System.Collections.Generic;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using SimpleTweaksPlugin.GameStructs;
using SimpleTweaksPlugin.Sheets;
using SimpleTweaksPlugin.Tweaks.Tooltips;
using SimpleTweaksPlugin.TweakSystem;

namespace SimpleTweaksPlugin {
    public partial class TooltipTweakConfig {
        public bool ShouldSerializeMateriaStats() => MateriaStats != null;
        public MateriaStats.Configs MateriaStats = null;
    }
}

namespace SimpleTweaksPlugin.Tweaks.Tooltips {
    
    public class MateriaStats : TooltipTweaks.SubTweak {
        public override string Name => "魔晶石属性";
        public override string Description => "在装备属性显示中包括镶嵌魔晶石所带来的属性";
        
        public class Configs : TweakConfig {
            public bool Total = true;
            public bool Delta;
            public bool Colour;
            public bool SimpleCombined;
        }

        public Configs Config { get; private set; }

        protected override DrawConfigDelegate DrawConfigTree => (ref bool hasChanged) => {
            ImGui.BeginGroup();
            if (ImGui.Checkbox("显示总值##materiaStatsTooltipTweak", ref Config.Total)) {
                if (!Config.Total && !Config.Delta) {
                    Config.Delta = true;
                }

                hasChanged = true;
            }
            if (ImGui.Checkbox("显示差值##materiaStatsTooltipTweak", ref Config.Delta)) {
                if (!Config.Total && !Config.Delta) {
                    Config.Total = true;
                }

                hasChanged = true;
            }
            ImGui.EndGroup();
                
            if (Config.Total && Config.Delta) {
                var y = ImGui.GetCursorPosY();
                var groupSize = ImGui.GetItemRectSize();
                ImGui.SameLine();
                    
                var text = "简化显示";
                var textSize = ImGui.CalcTextSize(text);
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (groupSize.Y / 2) - (textSize.Y / 2));
                hasChanged |= ImGui.Checkbox($"{text}##materiaStatSTooltipTweak", ref Config.SimpleCombined);
                ImGui.SetCursorPosY(y);
            }
                
            hasChanged |= ImGui.Checkbox("着色##materiaStatsTooltipTweak", ref Config.Colour);
        };

        public IEnumerable<TooltipTweaks.ItemTooltip.TooltipField> Fields() {
            yield return TooltipTweaks.ItemTooltip.TooltipField.Param0;
            yield return TooltipTweaks.ItemTooltip.TooltipField.Param1;
            yield return TooltipTweaks.ItemTooltip.TooltipField.Param2;
            yield return TooltipTweaks.ItemTooltip.TooltipField.Param3;
            yield return TooltipTweaks.ItemTooltip.TooltipField.Param4;
            yield return TooltipTweaks.ItemTooltip.TooltipField.Param5;
        }

        public override void Enable() {
            Config = LoadConfig<Configs>() ?? PluginConfig.TooltipTweaks.MateriaStats ?? new Configs();
            base.Enable();
        }

        public override void Disable() {
            SaveConfig(Config);
            PluginConfig.TooltipTweaks.MateriaStats = null;
            base.Disable();
        }
        
        public override void OnItemTooltip(TooltipTweaks.ItemTooltip tooltip, InventoryItem itemInfo) {
            
            
            if (!(Config.Delta || Config.Total == false)) Config.Total = true; // Config invalid check
            try {
                var item = PluginInterface.Data.Excel.GetSheet<Sheets.ExtendedItem>().GetRow(itemInfo.ItemId);
                if (item == null) return;
                if (item.MateriaSlotCount == 0) return;
                var itemLevel = PluginInterface.Data.Excel.GetSheet<ExtendedItemLevel>().GetRow(item.LevelItem.Row);
                if (itemLevel == null) return;
                var baseParams = new Dictionary<uint, ExtendedBaseParam>();
                var baseParamDeltas = new Dictionary<uint, int>();
                var baseParamOriginal = new Dictionary<uint, int>();
                var baseParamLimits = new Dictionary<uint, int>();
                foreach (var bp in item.BaseParam) {
                    if (bp.Value == 0 || bp.BaseParam.Row == 0) continue;
                    baseParamDeltas.Add(bp.BaseParam.Row, 0);
                    baseParamOriginal.Add(bp.BaseParam.Row, bp.Value);
                    baseParamLimits.Add(bp.BaseParam.Row, (int) Math.Ceiling(itemLevel.BaseParam[bp.BaseParam.Row] * (bp.BaseParam.Value.EquipSlotCategoryPct[item.EquipSlotCategory.Row] / 100f)) );
                    baseParams.Add(bp.BaseParam.Row, bp.BaseParam.Value);
                }

                if (itemInfo.IsHQ) {
                    foreach (var bp in item.BaseParamSpecial) {
                        if (bp.Value == 0 || bp.BaseParam.Row == 0) continue;
                        if (baseParamOriginal.ContainsKey(bp.BaseParam.Row)) baseParamOriginal[bp.BaseParam.Row] += bp.Value;
                    }
                }

                if (baseParamDeltas.Count == 0) return;
                
                foreach (var (materiaId, level) in itemInfo.Materia()) {
                    if (level >= 10) continue;
                    var materia = PluginInterface.Data.Excel.GetSheet<Materia>().GetRow(materiaId);
                    if (materia == null) continue;
                    if (materia.BaseParam.Row == 0) continue;
                    if (!baseParamDeltas.ContainsKey(materia.BaseParam.Row)) continue;
                    baseParamDeltas[materia.BaseParam.Row] += materia.Value[level];
                }
                foreach (var bp in baseParamDeltas) {
                    var param = baseParams[bp.Key];
                    if (bp.Value == 0) continue;

                    foreach (var field in Fields()) {
                        var data = tooltip[field];
                        if (data == null) continue;
                        
                        if (data.TextValue.Contains(param.Name)) {
                            data.Payloads.Add(new TextPayload($" ["));
                            var totalValue = baseParamOriginal[bp.Key] + bp.Value;
                            var deltaValue = bp.Value;
                            var exceedLimit = false;
                            if (totalValue > baseParamLimits[bp.Key]) {
                                exceedLimit = true;
                                totalValue = baseParamLimits[bp.Key];
                                deltaValue = baseParamLimits[bp.Key] - baseParamOriginal[bp.Key];
                            }
                            if (Config.Delta) {
                                if (!(Config.Total && Config.SimpleCombined)) data.Payloads.Add(new TextPayload($"+"));
                                if (Config.Colour && !Config.Total) data.Payloads.Add(new UIForegroundPayload(PluginInterface.Data, (ushort) (exceedLimit ? 14 : 500)));
                                data.Payloads.Add(new TextPayload($"{deltaValue}"));
                                if (Config.Colour && !Config.Total) data.Payloads.Add(new UIForegroundPayload(PluginInterface.Data, 0));
                                if (Config.Total && !Config.SimpleCombined) {
                                    data.Payloads.Add(new TextPayload("="));
                                } else if (Config.Total) {
                                    data.Payloads.Add(new TextPayload(" "));
                                }
                            }
                            if (Config.Total) {
                                if (Config.Colour) data.Payloads.Add(new UIForegroundPayload(PluginInterface.Data, (ushort) (exceedLimit ? 14 : 500)));
                                data.Payloads.Add(new TextPayload($"{totalValue}"));
                                if (Config.Colour) data.Payloads.Add(new UIForegroundPayload(PluginInterface.Data, 0));
                            }
                            
                            data.Payloads.Add(new TextPayload("]"));

                            tooltip[field] = data;
                        }

                    }
                }

            } catch (Exception ex) {
                SimpleLog.Error(ex);
            }

        }
    }
}
