using SimulturnDomain.DataStructures;
using SimulturnDomain.Model;
using SimulturnDomain.Settings;
using SimulturnDomain.ValueTypes;

namespace SimulturnDomain.Ai;
public interface IAi
{
    Order GetOrder(PlayerState playerState,
                   GameSettings gameSettings,
                   TurnMap<HexMap<PlayerMap<Fight>>> fightTurnPlayerMap,
                   TurnMap<HexMap<PlayerMap<Army>>> armyTurnHexPlayerMap,
                   TurnMap<HexMap<PlayerMap<Structure>>> structureTurnHexPlayerMap);
}
