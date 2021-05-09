using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.FFXIV.Component.GUI.ULD;

namespace SimpleTweaksPlugin
{
    //*(*(a2 + 0x20)+0x20)=Data
    //*(*(*(a3+0x18) + 0x20))+0x30+0x68*index)   =NameText
    //*(*(*(a3+0x18) + 0x20))+0x38+0x68*index)   =Casting Skill



    [StructLayout(LayoutKind.Explicit, Size = 0x9C)]
    public unsafe struct MemberData
    {
        [FieldOffset(0x00)] private uint HasMP;
        [FieldOffset(0x04)] private uint Level;
        [FieldOffset(0x08)] private uint JobId; //+固定值
        [FieldOffset(0x0C)] private uint InCrossRealm;
        [FieldOffset(0x10)] private uint CurrentHP;
        [FieldOffset(0x14)] private uint MaxHp;
        [FieldOffset(0x18)] private uint ShieldPercent;
        [FieldOffset(0x1C)] private uint CurrentMP;
        [FieldOffset(0x20)] private uint MaxMp;

        [FieldOffset(0x28)] private uint EmnityPercent;
        [FieldOffset(0x2C)] private uint EmnityNumber;
        [FieldOffset(0x30)] private uint Unknown1;      //本地FFFEFFE8    跨服FFFFC8C5
        [FieldOffset(0x34)] private uint Unknown2;//本地FF985008    跨服FFE22A00
        [FieldOffset(0x38)] private uint BuffCount;
        [FieldOffset(0x3C)] private fixed uint BuffIcon[20];
        //[FieldOffset(0x8C)] FFFFFFF
        [FieldOffset(0x90)] private uint ActorId;
        [FieldOffset(0x94)] private uint Occupied;
        //[FieldOffset(0xD8)] 00000000
    }


    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct DataArray
    {

        [FieldOffset(0x04)] private uint Unknown;
        [FieldOffset(0x0C)] private uint HideWhenSolo;
        [FieldOffset(0x14)] private uint LocalCount;

        [FieldOffset(0x18)] uint LeaderNumber; 

        [FieldOffset(0x20)] private MemberData* MemberData;//数量未知

        [FieldOffset(0x4FC)] private uint CrossRealmCount;

        [FieldOffset(0x80C)] private uint CPCount;
        [FieldOffset(0x810)] private uint PetCount;
    }
}