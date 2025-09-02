namespace Simulturn.Core.Model.State.StateValidation;
public interface IStateValidation;

public record NegativeRemainingMatter(Hexagon Hexagon, int RemainingMatter) : IStateValidation;

public record HexagonMissingInSettings(Hexagon Hexagon) : IStateValidation;