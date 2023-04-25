using SimulturnDomain.Entities;

namespace SimulturnDomain.Settings;
public record HexagonSettings(ushort Mass,
    Army MaxNumberOfUnitsGeneratingIncome,
    bool Buildable);