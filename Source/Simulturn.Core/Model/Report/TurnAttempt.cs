using Simulturn.Core.Model.State.Validation;

namespace Simulturn.Core.Model.Report;

/// <summary>
/// What <see cref="Engine.Game.PlayTurn"/> makes of the commands of all players: the resolved
/// turn, or the reasons why the commands were refused. A refused turn changes nothing.
/// </summary>
public union TurnAttempt(TurnResult, RefusedCommands)
{
    public override string ToString() => Value?.ToString() ?? nameof(TurnAttempt);
}

/// <summary>
/// The rules the commands break, keyed by the id of the player who issued them. Only players
/// with at least one broken rule have an entry.
/// </summary>
public sealed record RefusedCommands(ImmutableDictionary<string, ImmutableArray<CommandValidation>> Validations);
