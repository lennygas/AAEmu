﻿using AAEmu.Commons.Network;
using AAEmu.Game.Core.Network.Game;

namespace AAEmu.Game.Core.Packets.G2C;

public class SCSkillEndedPacket : GamePacket
{
    public override PacketLogLevel LogLevel => PacketLogLevel.Trace;

    private readonly ushort _tlId;

    public SCSkillEndedPacket(ushort tlId) : base(SCOffsets.SCSkillEndedPacket, 1)
    {
        _tlId = tlId;
    }

    public override PacketStream Write(PacketStream stream)
    {
        stream.Write(_tlId);
        return stream;
    }
}
