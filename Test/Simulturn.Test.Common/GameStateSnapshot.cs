using Simulturn.Core.Model;
using Simulturn.Core.Model.State;
using System.Globalization;
using System.Text;

namespace Simulturn.Test.Common;

/// <summary>
/// Renders a game state as a canonical string. Two states with the same snapshot are the same
/// state, independent of dictionary enumeration order. Used to compare the outcome of games.
/// </summary>
public static class GameStateSnapshot
{
    public static string Of(GameState state)
    {
        StringBuilder text = new();
        text.Append(CultureInfo.InvariantCulture, $"Turn {state.Turn}\n");
        foreach ((Hexagon hexagon, int matter) in Sorted(state.RemainingMatter))
        {
            text.Append(CultureInfo.InvariantCulture, $"Matter {hexagon} {matter}\n");
        }
        foreach ((string playerId, PlayerState player) in state.PlayerStates.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            text.Append(CultureInfo.InvariantCulture, $"Player {playerId} matter={player.Matter} used={player.UsedSpace} available={player.AvailableSpace}\n");
            AppendPerHexagon(text, "  Army", player.Armies);
            AppendPerHexagon(text, "  Compound", player.Compounds);
            AppendPerTurn(text, "  Training", player.Trainings);
            AppendPerTurn(text, "  Construction", player.Constructions);
            AppendPerHexagon(text, "  Research", player.Researches);
            foreach ((Upgrade upgrade, byte level) in player.UpgradeLevels.OrderBy(x => x.Key))
            {
                text.Append(CultureInfo.InvariantCulture, $"  Upgrade {upgrade} {level}\n");
            }
        }
        return text.ToString();
    }

    private static void AppendPerHexagon<TValue>(StringBuilder text, string label, IReadOnlyDictionary<Hexagon, TValue> values)
    {
        foreach ((Hexagon hexagon, TValue value) in Sorted(values))
        {
            text.Append(CultureInfo.InvariantCulture, $"{label} {hexagon} {value}\n");
        }
    }

    private static void AppendPerTurn<TValue>(StringBuilder text, string label, IReadOnlyDictionary<ushort, ImmutableDictionary<Hexagon, TValue>> queue)
    {
        foreach ((ushort turn, var perHexagon) in queue.OrderBy(x => x.Key))
        {
            AppendPerHexagon(text, $"{label} turn={turn}", perHexagon);
        }
    }

    private static IEnumerable<KeyValuePair<Hexagon, TValue>> Sorted<TValue>(IReadOnlyDictionary<Hexagon, TValue> values)
    {
        return values.OrderBy(x => x.Key.X).ThenBy(x => x.Key.Y);
    }
}
