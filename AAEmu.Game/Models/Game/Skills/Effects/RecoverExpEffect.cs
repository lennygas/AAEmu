using System;
using System.Collections.Generic;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Packets;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Formulas;
using AAEmu.Game.Models.Game.Skills.Templates;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.Skills.Effects
{
    public class RecoverExpEffect : EffectTemplate
    {
        public bool NeedMoney { get; set; }
        public bool NeedLaborPower { get; set; }

        public bool NeedPriest { get; set; }
        // TODO 1.2 // public bool Penaltied { get; set; }

        public override bool OnActionTime => false;

        public override void Apply(BaseUnit caster, SkillCaster casterObj, BaseUnit target, SkillCastTarget targetObj,
            CastAction castObj, EffectSource source, SkillObject skillObject, DateTime time,
            CompressedGamePackets packetBuilder = null)
        {
            _log.Trace("RecoverExpEffect");

            if (caster is Character character)
            {
                var formula = FormulaManager.Instance.GetFormula((uint)FormulaKind.LaborPowerForRecoverExp);
                var valueLp = formula.Evaluate(new Dictionary<string, double>() { ["pc_level"] = character.Level });

                character.AddExp(character.RecoverableExp, true);
                character.SendPacket(new SCRecoverableExpPacket(character.ObjId, 0, 0, 0));
                character.LaborPower -= (short)valueLp;
                character.SendPacket(new SCCharacterLaborPowerChangedPacket((short)-valueLp, 0, 0, 0));
                //character.ChangeLabor((short)-valueLp, 0);
            }
        }
    }
}
