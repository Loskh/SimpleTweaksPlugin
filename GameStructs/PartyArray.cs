using System.Runtime.InteropServices;

namespace SimpleTweaksPlugin.GameStructs
{
    //*(*(a2 + 0x20)+0x20)=Data
    //*(*(*(a3+0x18) + 0x20))+0x30+0x68*index)   =NameText
    //*(*(*(a3+0x18) + 0x20))+0x38+0x68*index)   =Casting Skill



    [StructLayout(LayoutKind.Explicit, Size = 0x9C)]
    public unsafe struct MemberData
    {

        [FieldOffset(0x00)] public uint Changed;
        [FieldOffset(0x04)] public uint HasMP;
        [FieldOffset(0x08)] public uint Level;
        [FieldOffset(0x0C)] public uint JobId; //+固定值
        [FieldOffset(0x10)] public uint InCrossRealm;
        [FieldOffset(0x14)] public uint CurrentHP; //范围外FFFFFFFF  跨服00000001
        [FieldOffset(0x18)] public uint MaxHp;     //范围外00000001  跨服00000001
        [FieldOffset(0x1C)] public uint ShieldPercent;
        [FieldOffset(0x20)] public uint CurrentMP; //范围外FFFFFFFF  跨服00000000
        [FieldOffset(0x24)] public uint MaxMp;     //范围外00000000  跨服00000000
        
        [FieldOffset(0x2C)] public uint EmnityPercent;
        [FieldOffset(0x30)] public uint EmnityNumber;
        [FieldOffset(0x34)] public uint Unknown1;      //本地FFFEFFE8    跨服FFFFC8C5
        [FieldOffset(0x38)] public uint Unknown2;//本地FF985008    跨服FFE22A00
        [FieldOffset(0x3C)] public uint BuffCount;
        [FieldOffset(0x40)] public fixed uint BuffIcon[20];
        //[FieldOffset(0x8C)] FFFFFFF
        [FieldOffset(0x94)] public uint ActorId;
        [FieldOffset(0x98)] public uint Occupied;
    }


    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct DataArray
    {

        [FieldOffset(0x04)] private uint Unknown;
        [FieldOffset(0x0C)] public uint HideWhenSolo;
        [FieldOffset(0x14)] public uint LocalCount;

        [FieldOffset(0x18)] public uint LeaderNumber; 

        [FieldOffset(0x1C)] private MemberData MemberData0;//数量未知
        [FieldOffset(0xB8)] private MemberData MemberData1;
        [FieldOffset(0x154)] private MemberData MemberData2;
        [FieldOffset(0x1F0)] private MemberData MemberData3;
        [FieldOffset(0x28C)] private MemberData MemberData4;
        [FieldOffset(0x328)] private MemberData MemberData5;
        [FieldOffset(0x3C4)] private MemberData MemberData6;
        [FieldOffset(0x460)] private MemberData MemberData7;
        //[FieldOffset(0x4FC)] private MemberData MemberData8;
        //[FieldOffset(0x59C)] private MemberData MemberData9;
        //[FieldOffset(0x638)] private MemberData MemberData10;
        //[FieldOffset(0x6D4)] private MemberData MemberData11;
        //[FieldOffset(0x770)] private MemberData MemberData12;


        [FieldOffset(0x4FC)] public uint CrossRealmCount;

        [FieldOffset(0x80C)] public uint CPCount;
        [FieldOffset(0x810)] public uint PetCount;
    
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