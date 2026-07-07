using Simulturn.Core.Model;
using System.Collections.Immutable;

namespace Simulturn.AI.Analysis;

/// <summary>
/// Finds the fighter composition that maximizes the simulated fight result against an
/// estimated enemy army, using the real strength formula of the engine. This exploits the
/// rock-paper-scissors combat design: an army composed against the enemy's composition
/// beats a uniform army of the same cost.
/// </summary>
public static class CompositionOptimizer
{
    /// <summary>
    /// The fighter composition of the given size with the best fight result against the
    /// enemy army. The hedge keeps a minimum share of every fighter type, so a wrong
    /// enemy estimate cannot produce a fully hard-countered army.
    /// Falls back to a uniform mix when nothing is known about the enemy.
    /// </summary>
    public static Army BestComposition(int fighterCount,
        Army enemyArmy,
        GameSettings gameSettings,
        SettingsAnalysis analysis,
        double hedge = 0.15)
    {
        if (fighterCount <= 0)
        {
            return Army.Empty;
        }
        if (enemyArmy.IsEmpty)
        {
            return analysis.UniformFighterMix(fighterCount);
        }
        ImmutableArray<Unit> fighters = analysis.Fighters;
        int minimumPerType = Math.Min((int)(hedge * fighterCount), fighterCount / fighters.Length);
        int step = Math.Max(1, fighterCount / 24);
        Army exponent = gameSettings.FightExponent;

        Army best = analysis.UniformFighterMix(fighterCount);
        double bestScore = Score(best, enemyArmy, exponent);
        foreach (Army candidate in EnumerateCompositions(fighters, fighterCount, minimumPerType, step))
        {
            double score = Score(candidate, enemyArmy, exponent);
            if (score > bestScore + 1e-9)
            {
                best = candidate;
                bestScore = score;
            }
        }
        return best;
    }

    private static double Score(Army own, Army enemy, Army exponent)
    {
        return own.GetStrengthOver(enemy, exponent) - enemy.GetStrengthOver(own, exponent);
    }

    private static IEnumerable<Army> EnumerateCompositions(ImmutableArray<Unit> fighters,
        int total,
        int minimumPerType,
        int step)
    {
        return Distribute(Army.Empty, 0);

        IEnumerable<Army> Distribute(Army army, int index)
        {
            if (index == fighters.Length - 1)
            {
                int remaining = total - army.Total;
                if (remaining >= minimumPerType)
                {
                    yield return army.AddUnit(fighters[index], (short)remaining);
                }
                yield break;
            }
            int maximum = total - army.Total - minimumPerType * (fighters.Length - 1 - index);
            foreach (int count in Counts(minimumPerType, maximum, step))
            {
                foreach (Army composition in Distribute(army.AddUnit(fighters[index], (short)count), index + 1))
                {
                    yield return composition;
                }
            }
        }
    }

    /// <summary>
    /// The counts from minimum to maximum in the given step, always including the maximum.
    /// </summary>
    private static IEnumerable<int> Counts(int minimum, int maximum, int step)
    {
        for (int count = minimum; count < maximum; count += step)
        {
            yield return count;
        }
        if (maximum >= minimum)
        {
            yield return maximum;
        }
    }
}
