﻿using SimulturnDomain.Enums;
using SimulturnDomain.ValueTypes;

namespace SimulturnDomain.Logic;
public static class FightHelper
{
    public static Structure Destroy(Army army, Structure structure, Army damage, Structure armor)
    {
        var totalDamage = (army * damage).Sum();
        var buildings = Enum.GetValues<Building>().OrderBy(x => armor[x]).ToArray();
        Structure losses = new Structure();
        int index = 0;
        while (totalDamage > 0)
        {
            Building building = buildings[index];
            short count = Convert.ToInt16(Math.Floor(totalDamage / (double)structure[building]));
            if (count <= 0)
            {
                totalDamage = 0;
            }
            else
            {
                totalDamage -= Convert.ToInt16(count * structure[building]);
            }
            losses = losses.Add(building, count);
        }
        return losses;
    }

    public static (Army arm1Losses, Army arm2Losses) Fight(double fightExponent, Army army1, Army army2)
    {
        var army1Strength = army1.GetStrengthOver(army2, fightExponent);
        var army2Strength = army2.GetStrengthOver(army1, fightExponent);
        Army arm1Losses;
        Army arm2Losses;
        if (army1Strength > army2Strength)
        {
            var fraction = army2Strength / army1Strength;
            arm1Losses = army1.MultiplyAndRoundUp(1 - fraction);
            arm2Losses = army2;
        }
        else
        {
            var fraction = army1Strength / army2Strength;
            arm2Losses = army2.MultiplyAndRoundUp(1 - fraction);
            arm1Losses = army1;
        }

        return (arm1Losses, arm2Losses);
    }
}