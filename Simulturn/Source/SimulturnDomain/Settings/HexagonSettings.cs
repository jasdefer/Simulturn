using SimulturnDomain.Entities;

namespace SimulturnDomain.Settings;
public record HexagonSettings(ushort Matter,
    Army MaxNumberOfUnitsGeneratingIncome,
    bool Buildable);