using System.Reflection;
using Simulturn.Core.Analysis;

namespace Simulturn.Lab.Test;

public sealed class LedgerEntriesTest
{
    /// <summary>
    /// The values a ledger stores. What it computes from them (<see cref="MatterLedger.Income"/>) has no setter.
    /// </summary>
    private static readonly ImmutableArray<PropertyInfo> _stored = [.. typeof(MatterLedger).GetProperties().Where(x => x.SetMethod is not null)];

    [Test]
    public void EveryStoredValueOfALedger_IsAnEntry()
    {
        LedgerEntries.All.Select(x => x.Name).Order().ShouldBe(_stored.Select(x => x.Name).Order());
    }

    [Test]
    public void Entry_ReadsTheValueItIsNamedAfter()
    {
        foreach (PropertyInfo property in _stored)
        {
            MatterLedger ledger = new();
            property.SetValue(ledger, 7);

            LedgerEntries.All.Single(x => x.Name == property.Name).Value(ledger).ShouldBe(7, property.Name);
        }
    }

    [Test]
    public void Sections_HoldEveryEntryOnce()
    {
        LedgerEntries.Sections.SelectMany(x => x.Entries).ShouldBe(LedgerEntries.All);
        LedgerEntries.All.Select(x => x.Name).ShouldBeUnique();
    }
}
