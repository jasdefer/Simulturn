namespace Simulturn.Core.Model;

public record HexagonSettings(
    int Matter,
    Army MaxNumberOfUnitsGeneratingMatter,
    bool IsBuildable,
    (string StartingPlayerId, Army InitialArmy, Compound InitialCompound)? PlayerInitialization
);