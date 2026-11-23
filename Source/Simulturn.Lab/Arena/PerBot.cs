using System.Collections;
using System.Runtime.CompilerServices;

namespace Simulturn.Lab.Arena;

/// <summary>
/// One value per bot of a pairing, in the order of the pairing: a bot has the same place in
/// every one of them (its name, its ledger, its wins). Two are equal when their values are, so a
/// record that holds one can be compared and grouped.
/// </summary>
[CollectionBuilder(typeof(PerBot), nameof(PerBot.Create))]
public readonly struct PerBot<T> : IReadOnlyList<T>, IEquatable<PerBot<T>>
{
    private readonly ImmutableArray<T> _values;

    internal PerBot(ImmutableArray<T> values)
    {
        _values = values;
    }

    private ImmutableArray<T> Values => _values.IsDefault ? [] : _values;

    /// <summary>The number of bots.</summary>
    public int Count => Values.Length;

    /// <summary>The value of the bot at the given place of the pairing, 0 is the first bot.</summary>
    public T this[int bot] => Values[bot];

    /// <summary>The places of the bots, to go through the bots of a pairing.</summary>
    public IEnumerable<int> Places => Enumerable.Range(0, Count);

    public IEnumerator<T> GetEnumerator()
    {
        return ((IEnumerable<T>)Values).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public bool Equals(PerBot<T> other)
    {
        return Values.SequenceEqual(other.Values);
    }

    public override bool Equals(object? obj)
    {
        return obj is PerBot<T> other && Equals(other);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();
        foreach (T value in Values)
        {
            hash.Add(value);
        }
        return hash.ToHashCode();
    }

    public static bool operator ==(PerBot<T> left, PerBot<T> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(PerBot<T> left, PerBot<T> right)
    {
        return !left.Equals(right);
    }
}

/// <summary>
/// Builds a <see cref="PerBot{T}"/> from a collection expression.
/// </summary>
public static class PerBot
{
    public static PerBot<T> Create<T>(ReadOnlySpan<T> values)
    {
        return new PerBot<T>([.. values]);
    }
}
