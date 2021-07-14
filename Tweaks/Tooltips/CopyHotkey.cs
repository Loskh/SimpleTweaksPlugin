﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Dalamud;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Game.Internal;
using ImGuiNET;
using SimpleTweaksPlugin.Enums;
using SimpleTweaksPlugin.GameStructs;
using SimpleTweaksPlugin.Helper;
using SimpleTweaksPlugin.Sheets;
using SimpleTweaksPlugin.TweakSystem;

namespace SimpleTweaksPlugin {
    public partial class TooltipTweakConfig {
        public bool ShouldSerializeCopyHotkey() => false;
        public bool ShouldSerializeCopyHotkeyEnabled() => false;
        public bool ShouldSerializeTeamcraftLinkHotkey() => false;
        public bool ShouldSerializeTeamcraftLinkHotkeyEnabled() => false;
        public bool ShouldSerializeTeamcraftLinkHotkeyForceBrowser() => false;
        public bool ShouldSerializeGardlandToolsLinkHotkey() => false;
        public bool ShouldSerializeGardlandToolsLinkHotkeyEnabled() => false;
        public bool ShouldSerializeGamerEscapeLinkHotkey() => false;
        public bool ShouldSerializeGamerEscapeLinkHotkeyEnabled() => false;
        public bool ShouldSerializeHideHotkeysOnTooltip() => false;

        public VK[] CopyHotkey = { VK.Ctrl, VK.C };
        public bool CopyHotkeyEnabled = false;

        public VK[] TeamcraftLinkHotkey = {VK.Ctrl, VK.T};
        public bool TeamcraftLinkHotkeyEnabled = false;
        public bool TeamcraftLinkHotkeyForceBrowser = false;

        public VK[] GardlandToolsLinkHotkey = {VK.Ctrl, VK.G};
        public bool GardlandToolsLinkHotkeyEnabled = false;

        public VK[] GamerEscapeLinkHotkey = {VK.Ctrl, VK.E};
        public bool GamerEscapeLinkHotkeyEnabled = false;

        public bool HideHotkeysOnTooltip = false;
    }
}

namespace SimpleTweaksPlugin.Tweaks.Tooltips {
    public class CopyHotkey : TooltipTweaks.SubTweak {

        public class Configs : TweakConfig {
            public VK[] CopyHotkey = { VK.Ctrl, VK.C };
            public bool CopyHotkeyEnabled = false;

            public VK[] TeamcraftLinkHotkey = {VK.Ctrl, VK.T};
            public bool TeamcraftLinkHotkeyEnabled = false;
            public bool TeamcraftLinkHotkeyForceBrowser = false;

            public VK[] GardlandToolsLinkHotkey = {VK.Ctrl, VK.G};
            public bool GardlandToolsLinkHotkeyEnabled = false;

            public VK[] GamerEscapeLinkHotkey = {VK.Ctrl, VK.E};
            public bool GamerEscapeLinkHotkeyEnabled = false;

            public bool HideHotkeysOnTooltip = false;
        }
        
        public Configs Config { get; private set; }

        private readonly string weirdTabChar = Encoding.UTF8.GetString(new byte[] {0xE3, 0x80, 0x80});

        public override string Name => "物品快捷键";
        public override string Description => "当显示物品详情时可以使用快捷键进行一系列操作";
        public override void OnItemTooltip(TooltipTweaks.ItemTooltip tooltip, InventoryItem itemInfo) {
            if (Config.HideHotkeysOnTooltip) return;
            var seStr = tooltip[TooltipTweaks.ItemTooltip.TooltipField.ControlsDisplay];
            if (seStr == null) return;

            var split = seStr.TextValue.Split(new string[] { weirdTabChar }, StringSplitOptions.None);
            if (split.Length > 0) {
                seStr.Payloads.Clear();
                seStr.Payloads.Add(new TextPayload(string.Join("\n", split)));
            }

            if (Config.CopyHotkeyEnabled) seStr.Payloads.Add(new TextPayload($"\n{string.Join("+", Config.CopyHotkey.Select(k => k.GetKeyName()))}  复制物品名"));
            if (Config.TeamcraftLinkHotkeyEnabled) seStr.Payloads.Add(new TextPayload($"\n{string.Join("+", Config.TeamcraftLinkHotkey.Select(k => k.GetKeyName()))}  在Teamcraft查看"));
            if (Config.GardlandToolsLinkHotkeyEnabled) seStr.Payloads.Add(new TextPayload($"\n{string.Join("+", Config.GardlandToolsLinkHotkey.Select(k => k.GetKeyName()))}  在Garland Tools查看"));
            if (Config.GamerEscapeLinkHotkeyEnabled) seStr.Payloads.Add(new TextPayload($"\n{string.Join("+", Config.GamerEscapeLinkHotkey.Select(k => k.GetKeyName()))}  在Gamer Escape查看"));

            SimpleLog.Verbose(seStr.Payloads);
            tooltip[TooltipTweaks.ItemTooltip.TooltipField.ControlsDisplay] = seStr;
        }

        private string settingKey = null;
        private string focused = null;
        
        private readonly List<VK> newKeys = new List<VK>();

        public void DrawHotkeyConfig(string name, ref VK[] keys, ref bool enabled, ref bool hasChanged) {
            while (ImGui.GetColumnIndex() != 0) ImGui.NextColumn();
            hasChanged |= ImGui.Checkbox(name, ref enabled);
            ImGui.NextColumn();
            var strKeybind = string.Join("+", keys.Select(k => k.GetKeyName()));

            ImGui.SetNextItemWidth(100);

            if (settingKey == name) {
                for (var k = 0; k < ImGui.GetIO().KeysDown.Count && k < 160; k++) {
                    if (ImGui.GetIO().KeysDown[k]) {
                        if (!newKeys.Contains((VK)k)) {

                            if ((VK)k == VK.ESCAPE) {
                                settingKey = null;
                                newKeys.Clear();
                                focused = null;
                                break;
                            }

                            newKeys.Add((VK)k);
                            newKeys.Sort();
                        }
                    }
                }

                strKeybind = string.Join("+", newKeys.Select(k => k.GetKeyName()));
            }

            if (settingKey == name) {
                ImGui.PushStyleColor(ImGuiCol.Border, 0xFF00A5FF);
                ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 2);
            }

            ImGui.InputText($"###{GetType().Name}hotkeyDisplay{name}", ref strKeybind, 100, ImGuiInputTextFlags.ReadOnly);
            var active = ImGui.IsItemActive();
            if (settingKey == name) {

                ImGui.PopStyleColor(1);
                ImGui.PopStyleVar();

                if (focused != name) {
                    ImGui.SetKeyboardFocusHere(-1);
                    focused = name;
                } else {
                    ImGui.SameLine();
                    if (ImGui.Button(newKeys.Count > 0 ? $"确认##{name}" : $"取消##{name}")) {
                        settingKey = null;
                        if (newKeys.Count > 0) keys = newKeys.ToArray();
                        newKeys.Clear();
                        hasChanged = true;
                    } else {
                        if (!active) {
                            focused = null;
                            settingKey = null;
                            if (newKeys.Count > 0) keys = newKeys.ToArray();
                            hasChanged = true;
                            newKeys.Clear();
                        }
                    }
                }
            } else {
                ImGui.SameLine();
                if (ImGui.Button($"设置快捷键###setHotkeyButton{name}")) {
                    settingKey = name;
                }
            }
        }

        protected override DrawConfigDelegate DrawConfigTree => (ref bool hasChanged) => {
            ImGui.Columns(2);
            ImGui.SetColumnWidth(0, 180 * ImGui.GetIO().FontGlobalScale);
            var c = Config;
            DrawHotkeyConfig("复制物品名", ref c.CopyHotkey, ref c.CopyHotkeyEnabled, ref hasChanged);
            ImGui.Separator();
            DrawHotkeyConfig("在Teamcraft查看", ref c.TeamcraftLinkHotkey, ref c.TeamcraftLinkHotkeyEnabled, ref hasChanged);
            ImGui.SameLine();
            ImGui.Checkbox($"使用浏览器###teamcraftIgnoreClient", ref Config.TeamcraftLinkHotkeyForceBrowser);
            ImGui.Separator();
            DrawHotkeyConfig("在Garland Tools查看", ref c.GardlandToolsLinkHotkey, ref c.GardlandToolsLinkHotkeyEnabled, ref hasChanged);
            ImGui.Separator();
            DrawHotkeyConfig("在Gamer Escape查看", ref c.GamerEscapeLinkHotkey, ref c.GamerEscapeLinkHotkeyEnabled, ref hasChanged);
            ImGui.Columns();
            ImGui.Dummy(new Vector2(5 * ImGui.GetIO().FontGlobalScale));
            hasChanged |= ImGui.Checkbox("Don't show hotkey help on Tooltip", ref c.HideHotkeysOnTooltip);
        };

        public override void Enable() {
            Config = LoadConfig<Configs>() ?? new Configs() {
                CopyHotkey = PluginConfig.TooltipTweaks.CopyHotkey,
                CopyHotkeyEnabled = PluginConfig.TooltipTweaks.CopyHotkeyEnabled,
                GamerEscapeLinkHotkey = PluginConfig.TooltipTweaks.GamerEscapeLinkHotkey,
                GamerEscapeLinkHotkeyEnabled = PluginConfig.TooltipTweaks.GamerEscapeLinkHotkeyEnabled,
                GardlandToolsLinkHotkey = PluginConfig.TooltipTweaks.GardlandToolsLinkHotkey,
                GardlandToolsLinkHotkeyEnabled = PluginConfig.TooltipTweaks.GardlandToolsLinkHotkeyEnabled,
                HideHotkeysOnTooltip = PluginConfig.TooltipTweaks.HideHotkeysOnTooltip,
                TeamcraftLinkHotkey = PluginConfig.TooltipTweaks.TeamcraftLinkHotkey,
                TeamcraftLinkHotkeyEnabled = PluginConfig.TooltipTweaks.TeamcraftLinkHotkeyEnabled,
                TeamcraftLinkHotkeyForceBrowser = PluginConfig.TooltipTweaks.TeamcraftLinkHotkeyForceBrowser
            };
            
            PluginInterface.Framework.OnUpdateEvent += FrameworkOnOnUpdateEvent;
            base.Enable();
        }

        public override void Disable() {
            SaveConfig(Config);
            PluginInterface.Framework.OnUpdateEvent -= FrameworkOnOnUpdateEvent;
            base.Disable();
        }

        private void CopyItemName(ExtendedItem extendedItem) {
            ImGui.SetClipboardText(extendedItem.Name);
        }

        private bool teamcraftLocalFailed = false;

        private void OpenTeamcraft(ExtendedItem extendedItem) {
            if (teamcraftLocalFailed || Config.TeamcraftLinkHotkeyForceBrowser) {
                Common.OpenBrowser($"https://ffxivteamcraft.com/db/en/item/{extendedItem.RowId}");
                return;
            }
            Task.Run(() => {
                try {
                    var wr = WebRequest.CreateHttp($"http://localhost:14500/db/en/item/{extendedItem.RowId}");
                    wr.Timeout = 500;
                    wr.Method = "GET";
                    wr.GetResponse().Close();
                } catch {
                    try {
                        if (System.IO.Directory.Exists(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ffxiv-teamcraft"))) {
                            Common.OpenBrowser($"teamcraft:///db/en/item/{extendedItem.RowId}");
                        } else {
                            teamcraftLocalFailed = true;
                            Common.OpenBrowser($"https://ffxivteamcraft.com/db/en/item/{extendedItem.RowId}");
                        }
                    } catch {
                        teamcraftLocalFailed = true;
                        Common.OpenBrowser($"https://ffxivteamcraft.com/db/en/item/{extendedItem.RowId}");
                    }
                }
            });
        }

        private void OpenGarlandTools(ExtendedItem extendedItem) {
            Common.OpenBrowser($"https://www.garlandtools.org/db/#item/{extendedItem.RowId}");
        }
        
        private void OpenGamerEscape(ExtendedItem extendedItem) {
            var name = Uri.EscapeUriString(extendedItem.Name);
            Common.OpenBrowser($"https://ffxiv.gamerescape.com/w/index.php?search={name}");
        }

        private bool isHotkeyPress(VK[] keys) {
            for (var i = 0; i < 0xA0; i++) {
                if (keys.Contains((VK) i)) {
                    if (!PluginInterface.ClientState.KeyState[i]) return false;
                } else {
                    if (PluginInterface.ClientState.KeyState[i]) return false;
                }
            }
            return true;
        }

        private void FrameworkOnOnUpdateEvent(Framework framework) {
            try {
                if (PluginInterface.Framework.Gui.HoveredItem == 0) return;

                Action<ExtendedItem> action = null;
                VK[] keys = null;

                var language = PluginInterface.ClientState.ClientLanguage;
                if (action == null && Config.CopyHotkeyEnabled && isHotkeyPress(Config.CopyHotkey)) {
                    action = CopyItemName;
                    keys = Config.CopyHotkey;
                }

                if (action == null && Config.TeamcraftLinkHotkeyEnabled && isHotkeyPress(Config.TeamcraftLinkHotkey)) {
                    action = OpenTeamcraft;
                    keys = Config.TeamcraftLinkHotkey;
                }

                if (action == null && Config.GardlandToolsLinkHotkeyEnabled && isHotkeyPress(Config.GardlandToolsLinkHotkey)) {
                    action = OpenGarlandTools;
                    keys = Config.GardlandToolsLinkHotkey;
                }
                
                if (action == null && Config.GamerEscapeLinkHotkeyEnabled && isHotkeyPress(Config.GamerEscapeLinkHotkey)) {
                    action = OpenGamerEscape;
                    keys = Config.GamerEscapeLinkHotkey;
                    language = ClientLanguage.English;
                }

                if (action != null) {
                    var id = PluginInterface.Framework.Gui.HoveredItem;
                    if (id >= 2000000) return;
                    id %= 500000;
                    var item = PluginInterface.Data.GetExcelSheet<ExtendedItem>(language).GetRow((uint) id);
                    if (item == null) return;
                    action(item);
                    foreach (var k in keys) {
                        PluginInterface.ClientState.KeyState[(int) k] = false;
                    }
                }
            } catch (Exception ex) {
                Plugin.Error(this, ex);
            }
        }
    }
}
