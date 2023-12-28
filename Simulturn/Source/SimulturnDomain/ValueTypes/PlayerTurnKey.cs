
namespace SimulturnDomain.ValueTypes;
public readonly struct PlayerTurnKey : IEquatable<PlayerTurnKey>
{
    public string PlayerId { get; }
    public ushort TurnNumber { get; }

    public PlayerTurnKey(string playerId, ushort turnNumber)
    {
        PlayerId = playerId;
        TurnNumber = turnNumber;
    }

    public override bool Equals(object? obj)
    {
        return obj is PlayerTurnKey key && Equals(key);
    }

    public bool Equals(PlayerTurnKey other)
    {
        return PlayerId == other.PlayerId &&
               TurnNumber == other.TurnNumber;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(PlayerId, TurnNumber);
    }

    public static bool operator ==(PlayerTurnKey left, PlayerTurnKey right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(PlayerTurnKey left, PlayerTurnKey right)
    {
        return !(left == right);
    }
}
