namespace Simulturn.Core.Model.State.StateValidation;

public interface IStateValidation;

public sealed record NegativeRemainingMatter(Hexagon Hexagon, int RemainingMatter) : IStateValidation;

public sealed record HexagonMissingInSettings(Hexagon Hexagon) : IStateValidation;