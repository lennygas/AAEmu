using System;

using AAEmu.Game.Core.Packets;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Items.Actions;
using AAEmu.Game.Models.Game.Items;
using AAEmu.Game.Models.Game.Skills.Templates;
using AAEmu.Game.Models.Game.Units;
using System.Collections.Generic;

namespace AAEmu.Game.Models.Game.Skills.Effects
{
    public class RepairSlaveEffect : EffectTemplate
    {
        public int Health { get; set; }
        public int Mana { get; set; }

        public override bool OnActionTime => false;

        public override void Apply(BaseUnit caster, SkillCaster casterObj, BaseUnit target, SkillCastTarget targetObj,
            CastAction castObj, EffectSource source, SkillObject skillObject, DateTime time,
            CompressedGamePackets packetBuilder = null)
        {
            _log.Trace("RepairSlaveEffect");

            if (caster is Character character && targetObj is SkillCastItemTarget itemTarget)
            {
                var item = character.Inventory.Bag.GetItemByItemId(itemTarget.Id);
                if (item is Summon summon)
                {
                    summon.NeedRepair = 0;
                }
                character.BroadcastPacket(new SCItemTaskSuccessPacket(ItemTaskType.RepairSlaves, new List<ItemTask> { new ItemUpdate(item) }, new List<ulong>()), true);
                //TODO: cooldown of item recovery in inventory 10 minutes
            }
        }
    }
}
