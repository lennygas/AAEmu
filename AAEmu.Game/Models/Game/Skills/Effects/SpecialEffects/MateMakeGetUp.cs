using System;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.Skills.Effects.SpecialEffects
{
    public class MateMakeGetUp : SpecialEffectAction
    {
        public override void Execute(BaseUnit caster,
            SkillCaster casterObj,
            BaseUnit target,
            SkillCastTarget targetObj,
            CastAction castObj,
            Skill skill,
            SkillObject skillObject,
            DateTime time,
            int value1,
            int value2,
            int value3,
            int value4)
        {
            // TODO ...
            if (caster is Character character) 
            { 
                _log.Debug("Special effects: MateMakeGetUp value1 {0}, value2 {1}, value3 {2}, value4 {3}, target {4}", value1, value2, value3, value4, target?.ObjId);

                var mate = MateManager.Instance.GetActiveMate(character.ObjId);
                mate.Buffs.RemoveBuff((uint)BuffConstants.TrippedMount);

                character.BroadcastPacket(new SCMateStatePacket(mate.ObjId), true);
                character.BroadcastPacket(new SCUnitPointsPacket(mate.ObjId, mate.Hp, mate.Mp), true);
            }
        }
    }
}
