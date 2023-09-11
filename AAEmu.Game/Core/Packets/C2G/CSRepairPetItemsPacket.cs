using System.Collections.Generic;
using AAEmu.Commons.Network;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Network.Game;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game.Items.Actions;
using AAEmu.Game.Models.Game.Items;
using AAEmu.Game.Models.Game.Skills;

namespace AAEmu.Game.Core.Packets.C2G
{
    public class CSRepairPetItemsPacket : GamePacket
    {
        public CSRepairPetItemsPacket() : base(CSOffsets.CSRepairPetItemsPacket, 1)
        {
        }

        public override void Read(PacketStream stream)
        {
            var npcId = stream.ReadBc();
            
            _log.Warn("RepairPetItems, NpcId: {0}", npcId);

            var tasks = new List<ItemTask>();
            foreach (var item in Connection.ActiveChar.Inventory.Bag.Items)
            {
                var mateitem = Connection.ActiveChar.Mates.GetMateInfo(item.Id);
                if (item is Summon summon && summon.NeedRepair != 0 && mateitem is not null)
                {
                    summon.NeedRepair = 0;
                    tasks.Add(new ItemUpdate(item));
                    mateitem.Hp = 99999; //After the pet is summoned, the Hp will be equal to the maximum Hp of the pet, instead of 99999
                    mateitem.Mp = 99999;
                }
            }
            Connection.ActiveChar.BroadcastPacket(new SCItemTaskSuccessPacket(ItemTaskType.RepairPets, tasks, new List<ulong>()), true);

            //If a character summoned a dead pet that is near an NPC
            var mate = MateManager.Instance.GetActiveMate(Connection.ActiveChar.ObjId);
            if (mate is not null)
            {
                mate.Buffs.RemoveBuff((uint)BuffConstants.InjuryMount);
                mate.Buffs.RemoveBuff((uint)BuffConstants.TrippedMount);
                mate.Hp = mate.MaxHp;
                mate.Mp = mate.MaxMp;
                mate.BroadcastPacket(new SCUnitPointsPacket(mate.ObjId, mate.Hp, mate.Mp), true);
            }
        }
    }
}
