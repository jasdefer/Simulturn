using System.Collections.Immutable;

namespace Simulturn.Core.Model.State;

public record GameState
{
    private GameSettings _gameSettings;


    public GameState(GameSettings gameSettings)
    {
        _gameSettings = gameSettings;
        Initialize();
    }

    private void Initialize()
    {
        Dictionary<string, 
        return;
    }

    public GameState NextTurn(ImmutableDictionary<string, ImmutableDictionary<Hexagon, Command>> commandsPerPlayerAndHexagon)
    {
        throw new NotImplementedException();
    }

    public GameState GetFromThePerspectiveOf(string player)
    {
        throw new NotImplementedException();  
    }
}