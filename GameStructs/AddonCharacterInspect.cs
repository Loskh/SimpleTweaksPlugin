﻿using System.Runtime.InteropServices;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace SimpleTweaksPlugin.GameStructs; 

[StructLayout(LayoutKind.Explicit, Size = 1280)]
[Addon("CharacterInspect")]
public unsafe struct AddonCharacterInspect {
    [FieldOffset(0x000)] public AtkUnitBase AtkUnitBase;
    [FieldOffset(0x430)] public AtkComponentBase* PreviewComponent;
}