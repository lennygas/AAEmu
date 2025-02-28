﻿using System.Collections.Generic;
using System.Drawing;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Managers.UnitManagers;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.DoodadObj;
using AAEmu.Game.Models.Game.Units;
using AAEmu.Game.Utils.Scripts;
using AAEmu.Game.Utils.Scripts.SubCommands;

namespace AAEmu.Game.Scripts.SubCommands.Doodads;

public class DoodadPhaseChangeSubCommand : SubCommandBase
{
    public DoodadPhaseChangeSubCommand()
    {
        Title = "[Doodad Phase Change]";
        Description = "Change the phase of a given doodad";
        CallPrefix = $"{CommandManager.CommandPrefix}doodad phase change";
        AddParameter(new NumericSubCommandParameter<uint>("ObjId", "Object Id", true));
        AddParameter(new NumericSubCommandParameter<int>("PhaseId", "Phase Id", true));
    }
    public override void Execute(ICharacter character, string triggerArgument, IDictionary<string, ParameterValue> parameters, IMessageOutput messageOutput)
    {
        uint doodadObjId = parameters["ObjId"];
        int phaseId = parameters["PhaseId"];
        var doodad = WorldManager.Instance.GetDoodad(doodadObjId);
        if (doodad is null)
        {
            SendColorMessage(messageOutput, Color.Red, "Doodad with objId {0} Does not exist |r", doodadObjId);
        }
        if (!(doodad is Doodad))
        {
            SendColorMessage(messageOutput, Color.Red, "Doodad with objId {0} is invalid (not a Doodad) |r", doodadObjId);
        }

        var availablePhases = string.Join(", ", DoodadManager.Instance.GetDoodadFuncGroupsId(doodad.TemplateId));

        SendMessage(messageOutput, "SetPhase {0}", phaseId);
        SendMessage(messageOutput, "TemplateId {0}: ObjId:{1}, ChangedPhase:{2}, Available phase ids (func groups): {3}", doodad.TemplateId, doodad.ObjId, phaseId, availablePhases);
        Logger.Warn($"{Title} Chain: TemplateId {doodad.TemplateId}, doodadObjId {doodad.ObjId}, SetPhase {phaseId}, Available phase ids (func groups): {availablePhases}");
        doodad.DoChangePhase((Unit)character, phaseId);
    }
}
