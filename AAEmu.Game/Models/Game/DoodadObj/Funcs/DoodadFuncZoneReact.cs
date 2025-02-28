﻿using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.DoodadObj.Templates;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.DoodadObj.Funcs;

public class DoodadFuncZoneReact : DoodadPhaseFuncTemplate
{
    public uint ZoneGroupId { get; set; }
    public int NextPhase { get; set; }

    public override bool Use(BaseUnit caster, Doodad owner)
    {
        Logger.Trace("DoodadFuncZoneReact");
        if (caster is Character)
        {
            //I think this is used to reschedule anything that needs triggered at a specific gametime
            owner.OverridePhase = NextPhase;
            return true;
        }

        return false;
    }
}
