using System;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.Skills.Effects.SpecialEffects
{
    public class HealPet : SpecialEffectAction
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
                _log.Debug("Special effects: HealPet value1 {0}, value2 {1}, value3 {2}, value4 {3}", value1, value2, value3, value4);

                var mate = MateManager.Instance.GetActiveMateByMateObjId(target.ObjId);
                mate.Hp = mate.MaxHp / 100 * value1;
                mate.Mp = mate.MaxMp / 100 * value2;
                mate.Buffs.RemoveBuff((uint)BuffConstants.InjuryMount);
                mate.Buffs.RemoveBuff((uint)BuffConstants.TrippedMount);

                character.BroadcastPacket(new SCMateStatePacket(mate.ObjId), true);
                character.BroadcastPacket(new SCUnitPointsPacket(mate.ObjId, mate.Hp, mate.Mp), true);
            }
        }
    }
}
