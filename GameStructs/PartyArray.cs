﻿using System.Runtime.InteropServices;

namespace SimpleTweaksPlugin.GameStructs
{
    //*(*(a2 + 0x20)+0x20)=Data
    //*(*(*(a3+0x18) + 0x20))+0x30+0x68*index)   =NameText
    //*(*(*(a3+0x18) + 0x20))+0x38+0x68*index)   =Casting Skill



    [StructLayout(LayoutKind.Explicit, Size = 0x9C)]
    public unsafe struct MemberData
    {

        [FieldOffset(0x00)] public int Changed;
        [FieldOffset(0x04)] public int HasMP;
        [FieldOffset(0x08)] public int Level;
        [FieldOffset(0x0C)] public int JobId; //+固定值
        [FieldOffset(0x10)] public int UnknownINT;
        [FieldOffset(0x14)] public int CurrentHP; //范围外FFFFFFFF  跨服00000001
        [FieldOffset(0x18)] public int MaxHp;     //范围外00000001  跨服00000001
        [FieldOffset(0x1C)] public int ShieldPercent;
        [FieldOffset(0x20)] public int CurrentMP; //范围外FFFFFFFF  跨服00000000
        [FieldOffset(0x24)] public int MaxMp;     //范围外00000000  跨服00000000
        
        [FieldOffset(0x2C)] public int EmnityPercent;
        [FieldOffset(0x30)] public int EmnityNumber;
        [FieldOffset(0x34)] public uint Unknown1;       //本地FFFEFFE8    跨服FFFFC8C5
        [FieldOffset(0x38)] public uint Unknown2;       //本地FF985008    跨服FFE22A00
        [FieldOffset(0x3C)] public int BuffCount;
        [FieldOffset(0x40)] public fixed uint BuffIcon[20];
        //[FieldOffset(0x8C)] FFFFFFF
        [FieldOffset(0x94)] public uint ActorId;
        [FieldOffset(0x98)] public uint Occupied;
    }


    [StructLayout(LayoutKind.Explicit)]
    public struct DataArray
    {

        [FieldOffset(0x04)] private uint Unknown;
        [FieldOffset(0x0C)] public int HideWhenSolo;
        [FieldOffset(0x14)] public int LocalCount;

        [FieldOffset(0x18)] public int LeaderNumber; 

        [FieldOffset(0x1C)] private MemberData MemberData0;//数量未知
        [FieldOffset(0xB8)] private MemberData MemberData1;
        [FieldOffset(0x154)] private MemberData MemberData2;
        [FieldOffset(0x1F0)] private MemberData MemberData3;
        [FieldOffset(0x28C)] private MemberData MemberData4;
        [FieldOffset(0x328)] private MemberData MemberData5;
        [FieldOffset(0x3C4)] private MemberData MemberData6;
        [FieldOffset(0x460)] private MemberData MemberData7;
        
        [FieldOffset(0x4FC)] public int QinXinCount;
        [FieldOffset(0x500)] private MemberData MemberData8;
        [FieldOffset(0x59C)] private MemberData MemberData9;
        [FieldOffset(0x638)] private MemberData MemberData10;

        [FieldOffset(0x6D4)] private MemberData MemberData11;
        [FieldOffset(0x770)] private MemberData MemberData12;

        [FieldOffset(0x80C)] public int CPCount;
        [FieldOffset(0x810)] public int PetCount;
    
    public MemberData MemberData(int index)
    {
    return index switch
    {
    0 => MemberData0,
    1 => MemberData1,
    2 => MemberData2,
    3 => MemberData3,
    4 => MemberData4,
    5 => MemberData5,
    6 => MemberData6,
    7 => MemberData7,
    //8 => MemberData8,
    //9 => MemberData9,
    //10 => MemberData10,
    //11 => MemberData11,
    //12 => MemberData12,
    _ => new MemberData(),
};

}
    }
}