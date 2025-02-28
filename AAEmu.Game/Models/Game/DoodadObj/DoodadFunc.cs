﻿using AAEmu.Game.Core.Managers.UnitManagers;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.DoodadObj;

public class DoodadFunc
{
    public uint GroupId { get; set; }
    public uint FuncId { get; set; }
    public uint FuncKey { get; set; }
    public string FuncType { get; set; }
    public int NextPhase { get; set; }
    public uint SoundId { get; set; }
    public uint SkillId { get; set; }
    public uint PermId { get; set; }
    public int Count { get; set; }

    //This acts as an interface/relay for doodad function chain
    //public async void Use(BaseUnit caster, Doodad owner, uint skillId, int nextPhase = 0)
    public void Use(BaseUnit caster, Doodad owner, uint skillId = 0, int nextPhase = 0)
    {

        var template = DoodadManager.Instance.GetFuncTemplate(FuncId, FuncType);

        template?.Use(caster, owner, skillId, nextPhase);
    }
}
