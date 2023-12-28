using SimulturnDomain.Model;
using SimulturnDomain.ValueTypes;
using System.Collections.Immutable;

namespace SimulturnDomain.Logic;
public static class StateHelper
{
    /// <summary>
    /// Compute the current state of the game based on all orders by each player.
    /// </summary>
    public static ImmutableDictionary<string, State> GetStatesPerPlayer(Game game)
    {
        ushort turns = game.TurnDictionary.NumberOfTurns;
        var armyMap = game.GameSettings.Coordinates.ToDictionary(x => x, x => new Army());
        var structureMap = game.GameSettings.Coordinates.ToDictionary(x => x, x => new Structure());

        var matter = game.TurnDictionary.PlayerIds.ToDictionary(x => x, x => game.GameSettings.StartMatter);
        var armies = game.TurnDictionary.PlayerIds.ToDictionary(x => x, x => armyMap.ToDictionary());
        var structures = game.TurnDictionary.PlayerIds.ToDictionary(x => x, x => structureMap.ToDictionary());
        var trainings = game.TurnDictionary.PlayerIds.ToDictionary(x => x, x => new Dictionary<ushort, Dictionary<Coordinates, Army>>());
        var constructions = game.TurnDictionary.PlayerIds.ToDictionary(x => x, x => new Dictionary<ushort, Dictionary<Coordinates, Structure>>());
        for (int i = 0; i < turns; i++)
        {

        }
    }
}