using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Dalamud.Game.ClientState.Actors;
using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Plugin;
using Common = SimpleTweaksPlugin.Helper.Common;

namespace SimpleTweaksPlugin.Tweaks.UiAdjustment
{
    public class PartyMember
    {
        private PartyMember()
        {
        }

        public string CharacterName { get; private set; }

        //public Actor Actor { get; private set; }

        public uint ClassJob { get; private set; }

        internal static PartyMember RegularMember(ActorTable table, IntPtr memberAddress)
        {
            //var actor = GetActorById(table, Marshal.ReadInt32(memberAddress, 0x1A8));
            var member = new PartyMember
            {
                CharacterName = PtrToStringUtf8(memberAddress + 0x1C4),
                ClassJob = Marshal.ReadByte(memberAddress, 0x205)
            };
            return member;
        }

        internal static PartyMember CrossRealmMember(ActorTable table, IntPtr crossMemberAddress)
        {
            //var actor = GetActorById(table, Marshal.ReadInt32(crossMemberAddress, 0x10));
            var member = new PartyMember
            {
                CharacterName = PtrToStringUtf8(crossMemberAddress + 0x22),
                ClassJob = Marshal.ReadByte(crossMemberAddress, 0x1E)
            };
            return member;
        }

        internal static PartyMember CompanionMember(ActorTable table, IntPtr companionMemberAddress)
        {
            var actor = GetActorById(table, Marshal.ReadInt32(companionMemberAddress, 0));
            var member = new PartyMember
            {
                //Actor = actor,
                CharacterName = actor?.Name ?? string.Empty,
                ClassJob = actor != null
                    ? Marshal.PtrToStructure<Dalamud.Game.ClientState.Structs.Actor>(actor.Address).ClassJob
                    : (uint) 0
            };
            return member;
        }

        internal static PartyMember LocalPlayerMember(DalamudPluginInterface dalamud)
        {
            var player = dalamud.ClientState.LocalPlayer;
            return new PartyMember()
            {
                //Actor = player,
                CharacterName = player?.Name ?? string.Empty,
                ClassJob = player?.ClassJob.Id ?? 0
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

        private IntPtr GroupManager =
            Common.Scanner.GetStaticAddressFromSig("48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 80 B8 ?? ?? ?? ?? ?? 76 50");

        //IntPtr CrossRealmGroupManagerPtr = Common.Scanner.GetStaticAddressFromSig("77 71 48 8B 05", 2);
        private IntPtr CompanionManagerPtr = Common.Scanner.GetStaticAddressFromSig("4C 8B 15 ?? ?? ?? ?? 4C 8B C9");
        private IntPtr GetCrossRealmMemberCount = Common.Scanner.ScanText("E8 ?? ?? ?? ?? 3C 01 77 4B");
        private IntPtr GetCrossMemberByGrpIndex = Common.Scanner.ScanText("E8 ?? ?? ?? ?? 44 89 7C 24 ?? 4C 8B C8");
        private IntPtr GetCompanionMemberCounts = Common.Scanner.ScanText("E8 ?? ?? ?? ?? 8B D3 85 C0");

        /// <summary>
        /// Initializes a new instance of the <see cref="PartyList"/> class.
        /// </summary>
        /// <param name="dalamud">A Dalamud.</param>
        public PartyList(DalamudPluginInterface dalamud)
        {
            this.dalamud = dalamud;
            getCrossPartyMemberCount =
                Marshal.GetDelegateForFunctionPointer<GetPartyMemberCountDelegate>(GetCrossRealmMemberCount);
            getCrossMemberByGrpIndex =
                Marshal.GetDelegateForFunctionPointer<GetCrossMemberByGrpIndexDelegate>(GetCrossMemberByGrpIndex);
            getCompanionMemberCount =
                Marshal.GetDelegateForFunctionPointer<GetCompanionMemberCountDelegate>(GetCompanionMemberCounts);
        }

        private delegate byte GetPartyMemberCountDelegate();

        private delegate IntPtr GetCrossMemberByGrpIndexDelegate(int index, int group);

        private delegate byte GetCompanionMemberCountDelegate(IntPtr manager);


        public int IndexOf(string name)
        {
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
                    var member = GroupManager + 0x230 * index;
                    return PartyMember.RegularMember(dalamud.ClientState.Actors, member);
                }

                if (GetCompanionMemberCount() > 0)
                {
                    if (index >= 3) // return a dummy player member if it's not one of the npcs
                        return PartyMember.LocalPlayerMember(dalamud);
                    var member = Marshal.ReadIntPtr(CompanionManagerPtr) + 0x198 * index;
                    return PartyMember.CompanionMember(dalamud.ClientState.Actors, member);
                }

                if (Count == 1 && index == 0)
                {
                    return PartyMember.LocalPlayerMember(dalamud);
                }

                return null;
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
            return Marshal.ReadByte(GroupManager, 0x3D5C);
        }

        private byte GetCompanionMemberCount()
        {
            var manager = Marshal.ReadIntPtr(CompanionManagerPtr);
            if (manager == IntPtr.Zero)
                return 0;
            return getCompanionMemberCount(CompanionManagerPtr);
        }
    }
}