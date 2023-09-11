using AAEmu.Commons.Network;
using AAEmu.Game.Models.Game.Items.Templates;

namespace AAEmu.Game.Models.Game.Items
{
    public class Summon : Item
    {
        public override ItemDetailType DetailType => ItemDetailType.Mate;
        public int Exp { get; set; }
        public byte NeedRepair { get; set; }
        public byte Level { get; set; }

        public Summon()
        {
        }

        public Summon(ulong id, ItemTemplate template, int count)
            : base(id, template, count)
        {
        }

        public override void ReadDetails(PacketStream stream)
        {
            stream.ReadInt32(); // exp
            stream.ReadByte();
            stream.ReadByte(); // level
        }

        public override void WriteDetails(PacketStream stream)
        {
            stream.Write(Exp); // exp
            stream.Write(NeedRepair);
            stream.Write(Level); // level
        }
    }
}
