using SimulturnDomain.Model;
using SimulturnDomain.Settings;

namespace SimulturnDomain.Ai;
public interface IAi
{
    Order GetOrder(PlayerState playerState,
                   GameSettings gameSettings);
}
