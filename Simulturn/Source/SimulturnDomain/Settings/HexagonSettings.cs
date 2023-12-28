using SimulturnDomain.ValueTypes;

namespace SimulturnDomain.Settings;
public record HexagonSettings(ushort Matter,
    Army MaxNumberOfUnitsGeneratingIncome,
    bool Buildable,
    bool IsStartHexagon);