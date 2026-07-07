using Simulturn.AI.Analysis;
using Simulturn.AI.Evaluation;
using Simulturn.Core.Model;

namespace Simulturn.AI.Test;

public class CompositionOptimizerTest
{
    private static readonly GameSettings _gameSettings = GameSettingsFactory.HexDisc();
    private static readonly SettingsAnalysis _analysis = GameSettingsAnalyzer.Analyze(_gameSettings);

    [Test]
    public void BestComposition_CountersAPureSquareArmy()
    {
        // Circles blank the strength of squares and are not countered by them;
        // triangles would be deleted by the squares before contributing anything.
        var best = CompositionOptimizer.BestComposition(10, new Army() { Square = 10 }, _gameSettings, _analysis, hedge: 0);

        best.Total.ShouldBe(10);
        best.Triangle.ShouldBe((short)0);
        best.Circle.ShouldBeGreaterThan((short)0);
    }

    [Test]
    public void BestComposition_BeatsTheUniformMixAgainstALopsidedEnemy()
    {
        Army enemy = new() { Square = 12, Triangle = 2 };
        Army exponent = _gameSettings.FightExponent;

        Army best = CompositionOptimizer.BestComposition(12, enemy, _gameSettings, _analysis, hedge: 0);
        Army uniform = _analysis.UniformFighterMix(12);

        double bestScore = best.GetStrengthOver(enemy, exponent) - enemy.GetStrengthOver(best, exponent);
        double uniformScore = uniform.GetStrengthOver(enemy, exponent) - enemy.GetStrengthOver(uniform, exponent);
        bestScore.ShouldBeGreaterThan(uniformScore);
    }

    [Test]
    public void BestComposition_RespectsTheHedgeMinimum()
    {
        var best = CompositionOptimizer.BestComposition(12, new Army() { Square = 12 }, _gameSettings, _analysis, hedge: 0.15);

        best.Total.ShouldBe(12);
        foreach (Unit fighter in _analysis.Fighters)
        {
            best[fighter].ShouldBeGreaterThanOrEqualTo((short)1);
        }
    }

    [Test]
    public void BestComposition_FallsBackToUniformAgainstAnUnknownEnemy()
    {
        var best = CompositionOptimizer.BestComposition(9, Army.Empty, _gameSettings, _analysis);

        best.ShouldBe(_analysis.UniformFighterMix(9));
    }
}
