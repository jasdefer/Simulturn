namespace Simulturn.Core.Model.Commands;
public record Command(
    Compound Construction,
    MovementCommand MovementCommand,
    Army Training,
    UpgradeCommand UpgradeCommand
);
