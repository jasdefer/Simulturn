namespace Simulturn.Core.Model.State;

public record PlayerState(
    int Matter,
    int UsedSupply,
    int AvailableSupply
);