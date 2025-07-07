using Simulturn.Core.Model.Commands;

public record Command(
    ConstructionCommand ConstructionCommand,
    MovementCommand MovementCommand,
    TrainingCommand TrainingCommand,
    UpgradeCommand UpgradeCommand
);