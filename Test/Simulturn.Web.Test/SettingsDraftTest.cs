using Simulturn.Core.Model;
using Simulturn.Web.Models;
using Simulturn.Web.Services;

namespace Simulturn.Web.Test;

public class SettingsDraftTest
{
    [Test]
    public void FromDefaults_IsValid_AndBuildsMatchingSettings()
    {
        var draft = SettingsDraft.FromDefaults();
        draft.Validate().ShouldBeEmpty();

        var built = draft.Build();
        var reference = DefaultSettings.Standard(["Player 1", "Player 2"], draft.Map.Seed);

        built.StartMatter.ShouldBe(reference.StartMatter);
        built.ArmyCost.ShouldBe(reference.ArmyCost);
        built.RequiredSpace.ShouldBe(reference.RequiredSpace);
        built.TrainingDuration.ShouldBe(reference.TrainingDuration);
        built.Income.ShouldBe(reference.Income);
        built.MovementRange.ShouldBe(reference.MovementRange);
        built.StructureDamage.ShouldBe(reference.StructureDamage);
        built.FightExponent.ShouldBe(reference.FightExponent);
        built.CompoundCost.ShouldBe(reference.CompoundCost);
        built.Armor.ShouldBe(reference.Armor);
        built.VisibilityRange.ShouldBe(reference.VisibilityRange);
        built.PartialVisibilityRange.ShouldBe(reference.PartialVisibilityRange);
        built.Upgrades[Upgrade.DotUpgrade].Length.ShouldBe(2);
        built.HexagonSettings.Count.ShouldBe(reference.HexagonSettings.Count);
    }

    [Test]
    public void Validate_CatchesZeroDuration()
    {
        var draft = SettingsDraft.FromDefaults();
        draft.Units[Unit.Dot].TrainingDuration = 0;
        draft.Validate().ShouldContain(problem => problem.Contains("training duration"));
    }

    [Test]
    public void Validate_CatchesDuplicatePlayers()
    {
        var draft = SettingsDraft.FromDefaults();
        draft.PlayerNames[1] = draft.PlayerNames[0];
        draft.Validate().ShouldContain(problem => problem.Contains("unique"));
    }

    [Test]
    public void Validate_CatchesMoreCenterLevelsThanDefined()
    {
        var draft = SettingsDraft.FromDefaults();
        draft.Map.DotUpgradeLevelsAtCenter = 5;
        draft.Validate().ShouldNotBeEmpty();
    }

    [Test]
    public void Build_ProducesPlayableGame()
    {
        var draft = SettingsDraft.FromDefaults();
        draft.PlayerNames.Add("Player 3");
        var state = new Simulturn.Core.Model.State.GameState(draft.Build());
        state.PlayerIds.Count.ShouldBe(3);
        state.IsValid().ShouldBeEmpty();
        state.NextTurn(new Dictionary<string, Dictionary<Hexagon, Simulturn.Core.Model.Commands.Command>>())
            .Turn.ShouldBe((ushort)1);
    }
}
