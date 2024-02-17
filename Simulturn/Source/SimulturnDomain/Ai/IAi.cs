using SimulturnDomain.DataStructures;
using SimulturnDomain.Model;
using SimulturnDomain.Settings;
using SimulturnDomain.ValueTypes;

namespace SimulturnDomain.Ai;
public interface IAi
{
    Order GetOrder(PlayerState playerState,
                   GameSettings gameSettings);
}
