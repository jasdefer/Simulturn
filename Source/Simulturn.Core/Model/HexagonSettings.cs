namespace Simulturn.Core.Model;

public record HexagonSettings(
    int Matter,
    Army MaxNumberOfUnitsGeneratingIncome,
    bool IsBuildable,
    string? StartingPlayerId
);