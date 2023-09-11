using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Models.Game.Units;
using AAEmu.Game.Models.Game.Units.Static;

namespace AAEmu.Game.Core.Packets.G2C
{
    public class SCUnitDeathPacket : GamePacket
    {
        private readonly uint _objId;
        private readonly KillReason _killReason;
        private readonly Unit _killer;
        private readonly int _resurrectionWaitingTime;
        private readonly int _lostExp;
        private readonly int _durabilityLossRatio;

        public SCUnitDeathPacket(uint objId, KillReason killReason, Unit killer = null, int resurrectionWaitingTime = 0, int lostExp = 0, int durabilityLossRatio = 0) : base(SCOffsets.SCUnitDeathPacket, 1)
        {
            _objId = objId;
            _killReason = killReason;
            _killer = killer;
            _resurrectionWaitingTime = resurrectionWaitingTime;
            _lostExp = lostExp;
            _durabilityLossRatio = durabilityLossRatio;
        }

        public override PacketStream Write(PacketStream stream)
        {
            stream.WriteBc(_objId);
            stream.Write((byte)_killReason);
            // ---------------
            stream.Write(_resurrectionWaitingTime); // resurrectionWaitingTime
            stream.Write(_lostExp); // lostExp
            stream.Write((byte)_durabilityLossRatio); // deathDurabilityLossRatio
            // ---------------
            stream.WriteBc(_killer?.ObjId ?? 0);
            if (_killer != null)
            {
                // ---------------
                stream.Write((byte) 0); // GameType
                // ---------------
                stream.Write((ushort) 0); // killStreak
                stream.Write((byte) 0); // param1
                stream.Write((byte) 0); // param2
                stream.Write((byte) 0); // param3
                stream.Write(_killer.Name);

            }

            return stream;
        }
    }
}
