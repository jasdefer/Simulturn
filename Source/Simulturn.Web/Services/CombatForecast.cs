using Simulturn.Core.Model;

namespace Simulturn.Web.Services;

/// <summary>
/// Deterministic preview of a pairwise fight, mirroring the engine's resolution math
/// (GameState.Fight): counter-based strengths, the weaker army is wiped out and the
/// stronger one loses the strength fraction, rounded up per unit type.
/// </summary>
public static class CombatForecast
{
    public sealed record Forecast(
        string OpponentId,
        double OwnStrength,
        double OpponentStrength,
        Army OwnLosses,
        Army OpponentLosses)
    {
        public bool OwnArmyWins => OwnStrength > OpponentStrength;

        /// <summary>Equal strengths (including 0 vs 0) destroy both armies completely.</summary>
        public bool MutualDestruction => OwnStrength == OpponentStrength;
    }

    public static Forecast Evaluate(GameSettings settings,
        string opponentId,
        Army ownArmy,
        Army ownExponentBonus,
        Army opponentArmy,
        Army opponentExponentBonus)
    {
        double ownStrength = ownArmy.GetStrengthOver(opponentArmy, settings.FightExponent + ownExponentBonus);
        double opponentStrength = opponentArmy.GetStrengthOver(ownArmy, settings.FightExponent + opponentExponentBonus);
        Army ownLosses;
        Army opponentLosses;
        if (ownStrength > opponentStrength)
        {
            ownLosses = ownArmy.MultiplyAndRoundUp(opponentStrength / ownStrength);
            opponentLosses = opponentArmy;
        }
        else if (ownStrength == 0 && opponentStrength == 0)
        {
            ownLosses = ownArmy;
            opponentLosses = opponentArmy;
        }
        else
        {
            opponentLosses = opponentArmy.MultiplyAndRoundUp(ownStrength / opponentStrength);
            ownLosses = ownArmy;
        }
        return new Forecast(opponentId, ownStrength, opponentStrength, ownLosses, opponentLosses);
    }
}
