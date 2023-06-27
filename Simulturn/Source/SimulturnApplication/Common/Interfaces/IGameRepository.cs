using SimulturnDomain.Entities;

namespace SimulturnApplication.Common.Interfaces;
public interface IGameRepository
{
    Task Add(Game game);
}
