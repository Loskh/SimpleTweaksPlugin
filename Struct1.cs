using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.FFXIV.Component.GUI.ULD;

namespace SimpleTweaksPlugin
{
    [StructLayout(LayoutKind.Explicit, Size = 0x100)]
    public unsafe struct SortedMember
    {
        [FieldOffset(0x00)] private AtkComponentNode** buff; //*10
        [FieldOffset(0x50)] private AtkComponentNode* Component;
        [FieldOffset(0x58)] private AtkTextNode* enmityTextNode; // Sorted[11] =2   Icon左下Text（仇恨排名什么的）
        [FieldOffset(0x60)] private AtkResNode* nameResNode; // Sorted[12] =14  姓名版
        [FieldOffset(0x68)] private AtkTextNode* partyNumberTextNode; // Sorted[13] =15  姓名版排序号
        [FieldOffset(0x70)] private AtkTextNode* nameTextNode; //Sorted[14] =16  角色名
        [FieldOffset(0x78)] private AtkTextNode* spellTextNode; //Sorted[15] =4   动作名
        [FieldOffset(0x80)] private AtkImageNode* castBarProgressImageNode; //Sorted[16] =5   施法条进度
        [FieldOffset(0x88)] private AtkImageNode* castBarEdgeImageNode; //Sorted[17] =6   施法条外框
        [FieldOffset(0x90)] private AtkResNode* enmityResNode; //Sorted[18] =7   仇恨条
        [FieldOffset(0x98)] private AtkNineGridNode* enmityBarNineGridNode; //Sorted[19] =8   仇恨量
        [FieldOffset(0xA0)] private AtkImageNode* iconNode; //Sorted[20] =11  职业Icon
        [FieldOffset(0xA8)] private AtkTexture* iconTexture; //Sorted[21] =11? 职业Icon.AtkTexture
        [FieldOffset(0xB0)] private AtkImageNode* crossNode; //Sorted[22] =10  跨服Icon              亲信=0
        [FieldOffset(0xB8)] private AtkTexture* crossTexture; //Sorted[23] =10? 跨服职业Icon.AtkTexture   亲信=0
        [FieldOffset(0xC0)] private AtkComponentNode* hpComponentNode; //Sorted[24] =    HP部分 Components
        [FieldOffset(0xC8)] private AtkComponentNode* hpBarComponentNode; //Sorted[25] =    HP条 Components
        [FieldOffset(0xD0)] private AtkComponentNode* mpBarComponentNode; //Sorted[26] =    MP部分 Components     宠物=0
        [FieldOffset(0xD8)] private AtkResNode* selectedResNode; //Sorted[27] =27  选中ResNode
        [FieldOffset(0xE0)] private AtkNineGridNode* halfLockNineGridNode; //Sorted[28] =29  半锁定bg
        [FieldOffset(0xE8)] private AtkNineGridNode* lockNineGridNode; //Sorted[29] =30  锁定bg
        [FieldOffset(0xF0)] private AtkCollisionNode* collisionNode; //Sorted[30] =31  碰撞Node
        [FieldOffset(0xF8)] private byte enmityNumber; //仇恨（1仇==1，2仇==2）其余==FF
    }


    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct PartyUi
    {
        //[FieldOffset(0x0)] public AtkEventListener AtkEventListener;
        //[FieldOffset(0x8)] public fixed byte Name[0x20];

        //[FieldOffset(0x028)] public ULDData ULDData;
        //[FieldOffset(0x028)] public AtkUldManager UldManager;

        //[FieldOffset(0x0C8)] public AtkResNode* RootNode;
        ////[FieldOffset(0x0D8)] FFFF or FFFB;

        //[FieldOffset(0x0F0)] public AtkCollisionNode* CollisionNode;
        //[FieldOffset(0x108)] public AtkComponentNode* WindowNode;

        ////[FieldOffset(0x19C)] FFFFFFFF or FDFFFFFF;
        //[FieldOffset(0x1AC)] public float Scale;
        //[FieldOffset(0x182)] public byte Flags;
        //[FieldOffset(0x1BC)] public short X;
        //[FieldOffset(0x1BE)] public short Y;

        //[FieldOffset(0x1CC)] public short ID;
        //[FieldOffset(0x1CE)] public short ParentID;
        //[FieldOffset(0x1D5)] public byte Alpha;

        //[FieldOffset(0x1D8)] public AtkCollisionNode** CollisionNodeList; 
        //// seems to be all collision nodes in tree, may be something else though

        //[FieldOffset(0x1E0)] public uint CollisionNodeListCount;

        ////[FieldOffset(0x1F6)] Unknown Textnode*;

        [FieldOffset(0x220)] private SortedMember* Member; //*13;
        [FieldOffset(0xF20)] private fixed uint JobId[8]; //+F294
        [FieldOffset(0xF40)] private fixed uint CrossJobId[8]; //+F294

        [FieldOffset(0xF54)] private fixed short InCrossRealm[8]; // 

        [FieldOffset(0xF88)] private fixed short Edited[13]; //0X11 if edited

        [FieldOffset(0xFA8)] private AtkNineGridNode* BackgroundNineGridNode; //= Background;
        [FieldOffset(0xFB0)] private AtkTextNode* SoloTextNode; //= Solo指示;
        [FieldOffset(0xFB8)] private AtkResNode* LeadeResNode; //= 队长指示(Res);
        [FieldOffset(0xFC0)] private AtkResNode* MpBarSpecialResNode; //= 蓝条特殊Res;
        [FieldOffset(0xFC8)] private AtkTextNode* MpBarSpecialTextNode; //= 蓝条特殊Text;

        [FieldOffset(0xFD0)] private ushort LocalCount; //本地
        [FieldOffset(0xFD4)] private ushort CrossRealmCount; //跨服
        [FieldOffset(0xFD8)] private ushort LeaderNumber; //or FFFF // (从0开始计数)

        [FieldOffset(0xFDC)] private ushort HideWhenSolo;
        //[FieldOffset(0xFE0)] FFFFFFFF
        //[FieldOffset(0xFE4)]FFFFFFFF
        //[FieldOffset(0xFE8)] = 蓝条特殊Res.Y;
        //[FieldOffset(0xFEC)] &= FFFE

        [FieldOffset(0xFF1)] private byte PetCount;
        [FieldOffset(0xFF2)] private byte CPCount;
    }
}