using SimulturnDomain.Model;
using System.Collections.Immutable;

namespace SimulturnDomain.DataStructures;
public class TurnDictionary
{
    private readonly Dictionary<string, List<Order>> _ordersPerPlayer = new Dictionary<string, List<Order>>();
    public TurnDictionary(IEnumerable<string> playerIds)
    {
        PlayerIds = playerIds.ToImmutableHashSet();
        _ordersPerPlayer = PlayerIds.ToDictionary(x => x, x => new List<Order>());
    }

    public void AddTurn(string playerId, Order order)
    {
        if (!PlayerIds.Contains(playerId))
        {
            throw new ArgumentException("Invalid player id", nameof(playerId));
        }
        _ordersPerPlayer[playerId].Add(order);
    }

    public Order GetOrder(string playerId, ushort turn)
    {
        return _ordersPerPlayer[playerId][turn];
    }

    public IReadOnlyList<Order> GetOrders(string playerId)
    {
        return _ordersPerPlayer[playerId];
    }

    public ImmutableDictionary<string, Order> GetOrders(ushort turn)
    {
        return _ordersPerPlayer
             .Select(x => new { PlayerId = x.Key, Order = x.Value[turn] })
             .ToImmutableDictionary(x => x.PlayerId, x => x.Order);
    }

    public ImmutableHashSet<string> PlayerIds { get; }
}