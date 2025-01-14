﻿using AAEmu.Game.Core.Managers;
using AAEmu.Game.Models.Game;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Utils;
using AAEmu.Game.Utils.Scripts;

namespace AAEmu.Game.Scripts.Commands;

public class Dist : ICommand
{
    public void OnLoad()
    {
        string[] name = { "dist", "distance" };
        CommandManager.Instance.Register(name, this);
    }

    public string GetCommandLineHelp()
    {
        return "";
    }

    public string GetCommandHelpText()
    {
        return "Gets distance using various calculations with target";
    }

    public void Execute(Character character, string[] args, IMessageOutput messageOutput)
    {
        var target = character.CurrentTarget;

        var rawDistance = MathUtil.CalculateDistance(character.Transform.World.Position, target.Transform.World.Position);
        var rawDistanceZ = MathUtil.CalculateDistance(character.Transform.World.Position, target.Transform.World.Position, true);
        var modelDistance = character.GetDistanceTo(target);
        var modelDistanceZ = character.GetDistanceTo(target, true);

        character.SendMessage("[Distance]\nRaw distance : {0}\nRaw distance (Z) : {1}\nModel adjusted distance: {2}\nModel adjusted distance (Z): {3}", rawDistance, rawDistanceZ, modelDistance, modelDistanceZ);
    }
}
