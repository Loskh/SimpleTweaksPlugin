using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Dalamud.Game.ClientState.Actors;
using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin;
using Common = SimpleTweaksPlugin.Helper.Common;

namespace SimpleTweaksPlugin.Tweaks.UiAdjustment
{
    #region Structure

    //[StructLayout(LayoutKind.Explicit, Size = 0x50)]
    //public struct CrossRealmMember
    //{
    //    [FieldOffset(0x10)] private int ObjectID;
    //    [FieldOffset(0x18)] private byte Level;
    //    [FieldOffset(0x1A)] private ushort CurrentWorld; //need check
    //    [FieldOffset(0x1C)] private ushort HomeWorld; //need check
    //    [FieldOffset(0x1E)] private byte ClassJob;
    //    [FieldOffset(0x1F)] private byte ObjectKind; //need check
    //    [FieldOffset(0x20)] private byte Unknown1; //Hex 10
    //    [FieldOffset(0x21)] private byte Unknown2; //01 or 02 need check
    //    [FieldOffset(0x22)] private string CharacterName;
    //}

    //[StructLayout(LayoutKind.Explicit, Size = 0x230)]
    //public struct RegularMember
    //{
    //    [FieldOffset(0x0)] private BuffList BuffList;
    //    [FieldOffset(0x190)] public float X;
    //    [FieldOffset(0x194)] public float Y;
    //    [FieldOffset(0x198)] public float Z;
    //    [FieldOffset(0x1A0)] public long Unk_1A0;
    //    [FieldOffset(0x1A8)] public uint ObjectID;
    //    [FieldOffset(0x1AC)] public uint Unk_ObjectID_1;
    //    [FieldOffset(0x1B0)] public uint Unk_ObjectID_2;
    //    [FieldOffset(0x1B4)] public uint CurrentHP;
    //    [FieldOffset(0x1B8)] public uint MaxHP;
    //    [FieldOffset(0x1BC)] public ushort CurrentMP;
    //    [FieldOffset(0x1BE)] public ushort MaxMP;
    //    [FieldOffset(0x1C0)] public ushort TerritoryType; // player zone
    //    [FieldOffset(0x1C2)] public ushort CurrentWorld; // seems to be 0x63/99, no idea what it is
    //    [FieldOffset(0x1C4)] public string CharacterName; // byte Name[0x40] character name string
    //    [FieldOffset(0x204)] public byte Sex;
    //    [FieldOffset(0x205)] public byte ClassJob;
    //    [FieldOffset(0x206)] public byte Level;

    //    [FieldOffset(0X207)] private byte ShieldPercent;

    //    // 0x18 byte struct at 0x208
    //    [FieldOffset(0x208)] public byte Unk_Struct_208__0;
    //    [FieldOffset(0x20C)] public uint Unk_Struct_208__4;
    //    [FieldOffset(0x210)] public ushort Unk_Struct_208__8;
    //    [FieldOffset(0x214)] public uint Unk_Struct_208__C;
    //    [FieldOffset(0x218)] public ushort Unk_Struct_208__10;
    //    [FieldOffset(0x21A)] public ushort Unk_Struct_208__14;
    //    [FieldOffset(0x220)] public byte Unk_220;
    //}

    //[StructLayout(LayoutKind.Explicit, Size = 0xC)]
    //public struct Buff
    //{
    //    [FieldOffset(0x0)] public ushort StatusID;
    //    [FieldOffset(0x2)] public byte Param;
    //    [FieldOffset(0x3)] public byte StackCount;
    //    [FieldOffset(0x4)] public float RemainingTime;

    //    [FieldOffset(0x8)] public uint SourceID; 
    //    // objectID matching the entity that cast the effect - regens will be from the white mage ID etc
    //}

    //[StructLayout(LayoutKind.Explicit, Size = 0x190)]
    //public unsafe struct BuffList
    //{
    //    [FieldOffset(0x0)]
    //    public IntPtr*
    //        Owner; // THIS IS NULL IN THE PARTY LIST, this class is used elsewhere and the pointer is filled in

    //    [FieldOffset(0x8)] public fixed byte Buffs[0xC * 30];
    //    [FieldOffset(0x170)] public uint Unk_170;
    //    [FieldOffset(0x174)] public ushort Unk_174;
    //    [FieldOffset(0x178)] public long Unk_178;
    //    [FieldOffset(0x180)] public byte Unk_180;
    //}

    #endregion


    public class PartyMember
    {
        private PartyMember()
        {
        }

        public string CharacterName { get; private set; }

        //public Actor Actor { get; private set; }
        public uint ClassJob { get; private set; }
        public string Address { get; private set; }
        public uint ShieldPercent { get; private set; }
        public uint Hpp { get; private set; }
        public uint CurrentHp { get; private set; }

        public uint MaxHp { get; private set; }

        
        internal static PartyMember RegularMember(ActorTable table, IntPtr memberAddress)
        {
            //var actor = GetActorById(table, Marshal.ReadInt32(memberAddress, 0x1A8));
            var maxHp = (uint) Marshal.ReadInt32(memberAddress, 0x1B8);
            var currentHp = (uint) Marshal.ReadInt32(memberAddress, 0x1B4);
            var member = new PartyMember
            {
                //Actor = actor,
                CharacterName = PtrToStringUtf8(memberAddress + 0x1C4),
                ClassJob = Marshal.ReadByte(memberAddress, 0x205),
                Address = memberAddress.ToString("X"),
                ShieldPercent = Marshal.ReadByte(memberAddress, 0X207), //Or (byte)actor.Address+0x1977
                CurrentHp = currentHp,
                MaxHp = maxHp,
                Hpp = maxHp == 0 ? 0 : currentHp * 100 / maxHp
            };
            return member;
        }

        internal static PartyMember CrossRealmMember(ActorTable table, IntPtr crossMemberAddress)
        {
            var actor = GetActorById(table, Marshal.ReadInt32(crossMemberAddress, 0x10));
            uint maxHp, currentHp;
            if (actor != null)
            {
                var act = Marshal.PtrToStructure<Dalamud.Game.ClientState.Structs.Actor>(actor.Address);
                maxHp = (uint) act.MaxHp;
                currentHp = (uint) act.CurrentHp;
            }
            else
            {
                maxHp = 0;
                currentHp = 0;
            }
            var member = new PartyMember
            {
                //Actor = actor,
                CharacterName = PtrToStringUtf8(crossMemberAddress + 0x22),
                ClassJob = Marshal.ReadByte(crossMemberAddress, 0x1E),
                Address = crossMemberAddress.ToString("X"),
                ShieldPercent = actor != null ? Marshal.ReadByte(actor.Address, 0x1977) : (uint) 0,
                Hpp = maxHp == 0 ? 0 : currentHp * 100 / maxHp,
                MaxHp = currentHp,
                CurrentHp = currentHp
            };
            return member;
        }

        internal static PartyMember CompanionMember(ActorTable table, IntPtr companionMemberAddress)
        {
            var actor = GetActorById(table, Marshal.ReadInt32(companionMemberAddress, 0));
            uint maxHp, currentHp;
            if (actor != null)
            {
                var act = Marshal.PtrToStructure<Dalamud.Game.ClientState.Structs.Actor>(actor.Address);
                maxHp = (uint) act.MaxHp;
                currentHp = (uint) act.CurrentHp;
            }
            else
            {
                maxHp = 0;
                currentHp = 0;
            }

            var member = new PartyMember
            {
                //Actor = actor,
                CharacterName = actor?.Name ?? string.Empty,
                ClassJob = actor != null
                    ? Marshal.PtrToStructure<Dalamud.Game.ClientState.Structs.Actor>(actor.Address).ClassJob
                    : (uint) 0,
                Address = companionMemberAddress.ToString("X"),
                ShieldPercent = actor != null ? Marshal.ReadByte(actor.Address, 0x1977) : (uint) 0,
                Hpp = maxHp == 0 ? 0 : currentHp * 100 / maxHp,
                CurrentHp = currentHp,
                MaxHp = maxHp
            };
            return member;
        }

        internal static PartyMember LocalPlayerMember(DalamudPluginInterface dalamud)
        {
            var player = dalamud.ClientState.LocalPlayer;
            var maxHp = (uint) (player?.MaxHp ?? 0);
            var currentHp = (uint) (player?.CurrentHp ?? 0);
            return new PartyMember()
            {
                //Actor = player,
                CharacterName = player?.Name ?? string.Empty,
                ClassJob = player?.ClassJob.Id ?? 0,
                Address = player?.Address.ToString("X") ?? "",
                ShieldPercent = player != null ? Marshal.ReadByte(player.Address, 0x1977) : (uint) 0,
                CurrentHp = currentHp,
                MaxHp = maxHp,
                Hpp = maxHp == 0 ? 0 : currentHp * 100 / maxHp
            };
        }

        private static Actor GetActorById(ActorTable table, int id)
        {
            foreach (var obj in table)
                if (obj != null && obj.ActorId == id)
                    return obj;

            return null;
        }

        private static unsafe string PtrToStringUtf8(IntPtr address, int maxLen = 64)
        {
            if (address == IntPtr.Zero)
                return string.Empty;

            var buffer = (byte*) address;
            var len = 0;
            while (len <= maxLen && *(buffer + len) != 0)
                ++len;

            return len < 1 ? string.Empty : Encoding.UTF8.GetString(buffer, len);
        }
    }


    /// <summary>
    /// A PartyList.
    /// </summary>
    public class PartyList : IReadOnlyCollection<PartyMember>
    {
        private readonly DalamudPluginInterface dalamud;
        private readonly GetPartyMemberCountDelegate getCrossPartyMemberCount;
        private readonly GetCompanionMemberCountDelegate getCompanionMemberCount;
        private readonly GetCrossMemberByGrpIndexDelegate getCrossMemberByGrpIndex;

        private readonly IntPtr groupManager =
            Common.Scanner.GetStaticAddressFromSig("48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 80 B8 ?? ?? ?? ?? ?? 76 50");

        //IntPtr CrossRealmGroupManagerPtr = Common.Scanner.GetStaticAddressFromSig("77 71 48 8B 05", 2);
        private readonly IntPtr companionManagerPtr =
            Common.Scanner.GetStaticAddressFromSig("4C 8B 15 ?? ?? ?? ?? 4C 8B C9");

        private readonly IntPtr crossRealmMemberCount = Common.Scanner.ScanText("E8 ?? ?? ?? ?? 3C 01 77 4B");

        private readonly IntPtr crossMemberByGrpIndex =
            Common.Scanner.ScanText("E8 ?? ?? ?? ?? 44 89 7C 24 ?? 4C 8B C8");

        private readonly IntPtr companionMemberCounts = Common.Scanner.ScanText("E8 ?? ?? ?? ?? 8B D3 85 C0");

        /// <summary>
        /// Initializes a new instance of the <see cref="PartyList"/> class.
        /// </summary>
        /// <param name="dalamud">A Dalamud.</param>
        public PartyList(DalamudPluginInterface dalamud)
        {
            this.dalamud = dalamud;
            getCrossPartyMemberCount =
                Marshal.GetDelegateForFunctionPointer<GetPartyMemberCountDelegate>(crossRealmMemberCount);
            getCrossMemberByGrpIndex =
                Marshal.GetDelegateForFunctionPointer<GetCrossMemberByGrpIndexDelegate>(crossMemberByGrpIndex);
            getCompanionMemberCount =
                Marshal.GetDelegateForFunctionPointer<GetCompanionMemberCountDelegate>(companionMemberCounts);
        }

        private delegate byte GetPartyMemberCountDelegate();

        private delegate IntPtr GetCrossMemberByGrpIndexDelegate(int index, int group);

        private delegate byte GetCompanionMemberCountDelegate(IntPtr manager);


        public int IndexOf(string name)
        {
            if (Count == 1 && name == PartyMember.LocalPlayerMember(dalamud).CharacterName) return 0;
            for (var i = 0; i < Count; i++)
                if (this[i].CharacterName == name)
                    return i;
            return -1;
        }

        public int Count
        {
            get
            {
                var count = getCrossPartyMemberCount();
                if (count > 0)
                    return count;
                count = GetRegularMemberCount();
                if (count > 1)
                    return count;
                count = GetCompanionMemberCount();
                return count > 0 ? count + 1 : 1;
            }
        }

        /// <summary>
        /// Gets the PartyMember at the specified index or null.
        /// </summary>
        /// <param name="index">The index.</param>
        public PartyMember this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                    return null;

                if (getCrossPartyMemberCount() > 0)
                {
                    var member = getCrossMemberByGrpIndex(index, -1);
                    if (member == IntPtr.Zero)
                        return null;
                    return PartyMember.CrossRealmMember(dalamud.ClientState.Actors, member);
                }

                if (GetRegularMemberCount() > 1)
                {
                    var member = groupManager + 0x230 * index;
                    return PartyMember.RegularMember(dalamud.ClientState.Actors, member);
                }

                if (GetCompanionMemberCount() > 0)
                {
                    if (index >= 3) // return a dummy player member if it's not one of the npcs
                        return PartyMember.LocalPlayerMember(dalamud);
                    var member = Marshal.ReadIntPtr(companionManagerPtr) + 0x198 * index;
                    return PartyMember.CompanionMember(dalamud.ClientState.Actors, member);
                }

                return Count == 1 ? PartyMember.LocalPlayerMember(dalamud) : null;
            }
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc/>
        public IEnumerator<PartyMember> GetEnumerator()
        {
            for (var i = 0; i < Count; i++)
            {
                var member = this[i];
                if (member != null)
                    yield return member;
            }
        }

        private byte GetRegularMemberCount()
        {
            return Marshal.ReadByte(groupManager, 0x3D5C);
        }

        private byte GetCompanionMemberCount()
        {
            var manager = Marshal.ReadIntPtr(companionManagerPtr);
            return manager == IntPtr.Zero ? (byte) 0 : getCompanionMemberCount(companionManagerPtr);
        }
    }
}