﻿using System;
using System.Runtime.InteropServices;
using Dalamud.Hooking;
using SimpleTweaksPlugin.TweakSystem;

namespace SimpleTweaksPlugin.Tweaks {
    public unsafe class LongVeil : Tweak {
        [StructLayout(LayoutKind.Sequential, Size = 4)]
        public struct EquipData {
            public ushort Model;
            public byte Variant;
            public byte Dye;
        }

        private delegate bool FlagSlotUpdateDelegate(IntPtr a1, uint a2, EquipData* a3);
        private Hook<FlagSlotUpdateDelegate> flagSlotUpdateHook;
        
        public override string Name => "显示长婚礼头纱";
        public override string Description => "显示婚礼头纱的加长版本(烙印过场时的版本)";

        public override void Enable() {
            flagSlotUpdateHook ??= new Hook<FlagSlotUpdateDelegate>(
                External.SigScanner.ScanText("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 8B DA 49 8B F0 48 8B F9 83 FA 0A"),
                new FlagSlotUpdateDelegate(FlagSlotUpdateDetour));
            flagSlotUpdateHook?.Enable();
            base.Enable();
        }

        public override void Disable() {
            flagSlotUpdateHook?.Disable();
            base.Disable();
        }

        public override void Dispose() {
            flagSlotUpdateHook?.Dispose();
            base.Dispose();
        }
        
        private bool FlagSlotUpdateDetour(IntPtr a1, uint a2, EquipData* a3) {
            try {
                if (a2 == 0 && a3->Model == 208) a3->Model = 199; // Replace Short Veil with Long Veil
                return flagSlotUpdateHook.Original(a1, a2, a3);
            } catch {
                return flagSlotUpdateHook.Original(a1, a2, a3);
            }
        }
    }
}
