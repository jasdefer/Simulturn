using Simulturn.Core.Engine;
using Simulturn.Core.Extensions;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.State;

namespace Simulturn.Bots.Catalog;

/// <summary>
/// Rolls dice among the orders that are not plainly pointless. It spends all its matter every
/// turn and saves for one thing only, the plane of a new hexagon. Its dots harvest, one spare
/// dot at a time claims a hexagon with matter or upgrades, and its armies stay together and
/// walk from one such hexagon to the next to guard it. Where an army goes is rolled, so it
/// attacks by chance. The measure a bot with a plan has to beat.
/// </summary>
public sealed class RandomBot : IBot
{
    private static readonly ImmutableArray<Unit> _fighters = [.. Enum.GetValues<Unit>().Where(x => x != Unit.Dot)];
    private static readonly Army _oneDot = new() { Dot = 1 };
    private static readonly PossibleHexagonOrders _noOrders = new(Army.Empty, Compound.Empty, [], Army.Empty);

    private readonly Random _random;
    private readonly RandomBotOptions _options;
    private ImmutableDictionary<Hexagon, HexagonMemory> _memories = ImmutableDictionary<Hexagon, HexagonMemory>.Empty;
    private Expedition? _expedition;
    private Dictionary<Hexagon, Guard> _guards = [];

    public RandomBot(int seed, RandomBotOptions? options = null)
    {
        _random = new Random(seed);
        _options = options ?? new RandomBotOptions();
    }

    public IReadOnlyDictionary<Hexagon, Command> GetCommands(PlayerGameState view)
    {
        _memories = FogOfWar.UpdateMemories(_memories, view);
        CommandsBuilder commands = new();
        if (Standing.IsDefeated(view.PlayerState))
        {
            return commands.ToImmutable();
        }

        if (_expedition is { } expedition &&
            (expedition.Position == expedition.Target || view.PlayerState.Armies.GetValueOrDefault(expedition.Position).Dot <= 0))
        {
            // The dot has arrived or was lost on its way.
            _expedition = null;
        }
        List<Hexagon> claims = _expedition is null && !PlaneSites(view, commands).Any() ? Claims(view) : [];

        Spend(view, commands, sendsExpedition: claims.Count > 0);
        MoveExpedition(view, commands, claims);
        MoveArmies(view, commands);
        return commands.ToImmutable();
    }

    /// <summary>
    /// Buys order by order until nothing it wants is affordable: first what the economy needs,
    /// then a roll among researches and fighting units, at last another training building.
    /// </summary>
    private void Spend(PlayerGameState view, CommandsBuilder commands, bool sendsExpedition)
    {
        while (true)
        {
            PossibleOrders possible = Orders.Possible(view, commands.Commands);
            if (TryBuildPlane(view, commands, possible))
            {
                continue;
            }
            int matter = possible.Budget.Matter - PlaneReserve(view, commands, possible);
            // A dot earns its price, so the reserve does not keep it back.
            if (TryTrainDot(view, commands, possible, sendsExpedition) ||
                TryBuildAxis(view, commands, possible, matter) ||
                TryRollOrder(view, commands, possible, matter) ||
                TryBuildTrainingBuilding(view, commands, possible, matter))
            {
                continue;
            }
            return;
        }
    }

    /// <summary>
    /// The one thing the bot saves for: the price of a plane, kept back while there is a hexagon
    /// left to harvest. Without it the bot spends everything and has nothing when its matter runs out.
    /// Without the price and without income there is nothing to wait for.
    /// </summary>
    private int PlaneReserve(PlayerGameState view, CommandsBuilder commands, PossibleOrders possible)
    {
        int price = Costs.Construction(view.GameSettings, Building.Plane).Matter;
        if (!Fields(view, commands).Any())
        {
            return 0;
        }
        return possible.Budget.Matter >= price || Income(view, commands) > 0 ? price : 0;
    }

    /// <summary>
    /// The hexagons with matter worth a plane on which the player has none and no opponent
    /// is known to live.
    /// </summary>
    private IEnumerable<Hexagon> Fields(PlayerGameState view, CommandsBuilder commands)
    {
        PlayerState player = view.PlayerState;
        foreach (Hexagon hexagon in Ordered(view.GameSettings.HexagonSettings.Keys))
        {
            Compound planes = player.Compounds.GetValueOrDefault(hexagon) + player.Constructions.On(hexagon) + commands.On(hexagon).Construction;
            if (planes.Plane <= 0 && !IsForeign(view, hexagon) && IsWorthAPlane(view.GameSettings, hexagon, RemainingMatter(view, hexagon)))
            {
                yield return hexagon;
            }
        }
    }

    /// <summary>
    /// The matter left on the hexagon as far as the player knows: what is in sight, else what was
    /// last seen, else what the map started with.
    /// </summary>
    private int RemainingMatter(PlayerGameState view, Hexagon hexagon)
    {
        return view.Observations.GetValueOrDefault(hexagon)?.RemainingMatter
            ?? _memories.GetValueOrDefault(hexagon)?.Static.RemainingMatter
            ?? view.GameSettings.HexagonSettings[hexagon].Matter;
    }

    /// <summary>
    /// Whether the hexagon belongs to an opponent as far as the player knows.
    /// </summary>
    private bool IsForeign(PlayerGameState view, Hexagon hexagon)
    {
        return !KnownBuildings(view, hexagon).IsEmpty;
    }

    /// <summary>
    /// The buildings of the opponents on the hexagon when it was last seen. A hexagon never seen
    /// holds what the map started with.
    /// </summary>
    private Compound KnownBuildings(PlayerGameState view, Hexagon hexagon)
    {
        if (_memories.GetValueOrDefault(hexagon) is { } memory)
        {
            return memory.Static.OpponentCompounds.Values.Sum();
        }
        return view.GameSettings.HexagonSettings[hexagon].PlayerInitialization is { } start && start.StartingPlayerId != view.PlayerId
            ? start.InitialCompound
            : Compound.Empty;
    }

    /// <summary>
    /// Whether somebody else holds the hexagon as far as the player knows, with units or with buildings.
    /// </summary>
    private bool IsContested(PlayerGameState view, Hexagon hexagon)
    {
        return KnownUnits(view, hexagon) > 0 || IsForeign(view, hexagon);
    }

    /// <summary>
    /// Whether the army has a point in going to a hexagon somebody else holds: it outnumbers
    /// the units it knows of, and it destroys at least one of the buildings it knows of.
    /// </summary>
    private bool CanTake(PlayerGameState view, Hexagon hexagon, Army army, bool daring)
    {
        Compound buildings = KnownBuildings(view, hexagon);
        return IsSafeFor(view, hexagon, army, daring) &&
            (buildings.IsEmpty || !Destruction.Of(view.GameSettings, army, buildings).IsEmpty);
    }

    private static bool TrainsFighters(Compound compound)
    {
        return !(Occupancy.TrainingSlots(compound) with { Dot = 0 }).IsEmpty;
    }

    /// <summary>
    /// The units of everybody else on the hexagon when it was last seen. A hexagon never seen
    /// holds what the map started with: a neutral army or the start army of an opponent.
    /// </summary>
    private int KnownUnits(PlayerGameState view, Hexagon hexagon)
    {
        if (_memories.GetValueOrDefault(hexagon)?.Units is { } units)
        {
            return units.OpponentUnitCounts.Values.Sum();
        }
        HexagonSettings hexagonSettings = view.GameSettings.HexagonSettings[hexagon];
        int startUnits = hexagonSettings.PlayerInitialization is { } start && start.StartingPlayerId != view.PlayerId ? start.InitialArmy.Total : 0;
        return hexagonSettings.NeutralArmy.Total + startUnits;
    }

    /// <summary>
    /// The hexagons that wait for a plane: an own dot is free there, there is matter worth a
    /// plane, and no plane of the player stands or is built there.
    /// </summary>
    private IEnumerable<Hexagon> PlaneSites(PlayerGameState view, CommandsBuilder commands)
    {
        PlayerState player = view.PlayerState;
        foreach (Hexagon hexagon in Ordered(player.Armies.Keys))
        {
            if (_expedition?.Position == hexagon)
            {
                // The dot there is only passing through.
                continue;
            }
            Compound constructions = player.Constructions.On(hexagon) + commands.On(hexagon).Construction;
            if (player.Compounds.GetValueOrDefault(hexagon).Plane > 0 ||
                constructions.Plane > 0 ||
                Occupancy.FreeDots(player.Armies[hexagon], constructions) <= 0 ||
                !IsWorthAPlane(view.GameSettings, hexagon, view.Observations[hexagon].RemainingMatter ?? 0))
            {
                continue;
            }
            yield return hexagon;
        }
    }

    private static bool IsWorthAPlane(GameSettings settings, Hexagon hexagon, int remainingMatter)
    {
        return settings.HexagonSettings.TryGetValue(hexagon, out HexagonSettings? hexagonSettings) &&
            hexagonSettings.IsBuildable &&
            remainingMatter > Costs.Construction(settings, Building.Plane).Matter;
    }

    private bool TryBuildPlane(PlayerGameState view, CommandsBuilder commands, PossibleOrders possible)
    {
        foreach (Hexagon hexagon in PlaneSites(view, commands))
        {
            if (Limit(possible, hexagon).Construction.Plane > 0)
            {
                commands.Construct(hexagon, Compound.FromBuilding(Building.Plane, 1));
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// A hexagon gets the dots that can harvest there, the ones its orders keep busy, and one
    /// to spare while there is a hexagon to claim.
    /// </summary>
    private bool TryTrainDot(PlayerGameState view, CommandsBuilder commands, PossibleOrders possible, bool sendsExpedition)
    {
        PlayerState player = view.PlayerState;
        foreach (Hexagon hexagon in Ordered(possible.Hexagons.Keys))
        {
            int dots = player.Armies.GetValueOrDefault(hexagon).Dot + player.Trainings.On(hexagon).Dot + commands.On(hexagon).Training.Dot;
            int wanted = DotNeed(view, commands, hexagon) + (sendsExpedition ? 1 : 0);
            if (possible.Hexagons[hexagon].Training.Dot > 0 && dots < wanted)
            {
                commands.Train(hexagon, _oneDot);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// The dots a hexagon has work for: as many as harvest at the same time, but only while the
    /// matter left there pays every dot twice, one per construction and one for a research.
    /// </summary>
    private int DotNeed(PlayerGameState view, CommandsBuilder commands, Hexagon hexagon)
    {
        PlayerState player = view.PlayerState;
        Command command = commands.On(hexagon);
        int remainingMatter = view.Observations.GetValueOrDefault(hexagon)?.RemainingMatter ?? 0;
        int harvesters = Math.Min(Harvest.Capacity(view.GameSettings, hexagon).Dot, remainingMatter / Math.Max(1, _options.DotPayback * Costs.Training(view.GameSettings, Unit.Dot).Matter));
        int builders = (player.Constructions.On(hexagon) + command.Construction).Sum();
        int researchers = command.Research is not null || player.Researches.ContainsKey(hexagon) ? 1 : 0;
        return harvesters + builders + researchers;
    }

    /// <summary>
    /// An axis as soon as the free space no longer covers what the buildings train at once.
    /// </summary>
    private bool TryBuildAxis(PlayerGameState view, CommandsBuilder commands, PossibleOrders possible, int matter)
    {
        GameSettings settings = view.GameSettings;
        Compound pending = view.PlayerState.Constructions.Values.Sum() + Ordered(commands);
        int demand = Costs.Training(settings, Occupancy.TrainingSlots(view.PlayerState.Compounds.Values.Sum() + pending)).Space;
        int supply = possible.Budget.Space + Occupancy.ProvidedSpace(settings, pending);
        return supply < demand && TryBuildOnABase(view, commands, possible, matter, [Building.Axis], together: false);
    }

    /// <summary>
    /// One research or one fighting unit, rolled among everything that is possible.
    /// </summary>
    private bool TryRollOrder(PlayerGameState view, CommandsBuilder commands, PossibleOrders possible, int matter)
    {
        GameSettings settings = view.GameSettings;
        List<(Hexagon Hexagon, Unit? Unit, Upgrade? Upgrade)> orders = [];
        foreach (Hexagon hexagon in Ordered(possible.Hexagons.Keys))
        {
            PossibleHexagonOrders limit = possible.Hexagons[hexagon];
            orders.AddRange(limit.Research
                .Where(upgrade => Costs.Research(settings, view.PlayerState.UpgradeLevels, upgrade).Matter <= matter)
                .Select(upgrade => (hexagon, (Unit?)null, (Upgrade?)upgrade)));
            orders.AddRange(_fighters
                .Where(unit => limit.Training[unit] > 0 && Costs.Training(settings, unit).Matter <= matter)
                .Select(unit => (hexagon, (Unit?)unit, (Upgrade?)null)));
        }
        if (orders.Count == 0)
        {
            return false;
        }
        (Hexagon orderHexagon, Unit? orderUnit, Upgrade? orderUpgrade) = orders[_random.Next(orders.Count)];
        if (orderUnit is Unit trained)
        {
            commands.Train(orderHexagon, Army.FromUnit(trained, 1));
        }
        else if (orderUpgrade is Upgrade researched)
        {
            commands.Research(orderHexagon, researched);
        }
        return true;
    }

    /// <summary>
    /// Another training building only while the ones there cannot spend the matter: the income
    /// of a turn is more than they take per turn, or the matter left fills all of them at once
    /// and still pays the new building with its first unit.
    /// </summary>
    private bool TryBuildTrainingBuilding(PlayerGameState view, CommandsBuilder commands, PossibleOrders possible, int matter)
    {
        GameSettings settings = view.GameSettings;
        Compound buildings = view.PlayerState.Compounds.Values.Sum() + view.PlayerState.Constructions.Values.Sum() + Ordered(commands);
        int spentPerTurn = _fighters.Sum(unit =>
            buildings[GameSettings.TrainingBuilding(unit)] * Costs.Training(settings, unit).Matter / Math.Max(1, (int)settings.TrainingDuration[unit]));
        int spentAtOnce = Costs.Training(settings, Occupancy.TrainingSlots(buildings) with { Dot = 0 }).Matter;
        bool outgrown = Income(view, commands) > spentPerTurn;
        ImmutableArray<Building> wanted =
        [
            .. _fighters
                .Where(unit => outgrown || matter >= spentAtOnce + Costs.Construction(settings, GameSettings.TrainingBuilding(unit)).Matter + Costs.Training(settings, unit).Matter)
                .Select(GameSettings.TrainingBuilding)
        ];
        return TryBuildOnABase(view, commands, possible, matter, wanted, together: true);
    }

    /// <summary>
    /// Starts one of the buildings on a hexagon that already holds buildings of the player,
    /// rolled among all that are possible. Training buildings stand together, so that what
    /// they train is one army.
    /// </summary>
    private bool TryBuildOnABase(PlayerGameState view, CommandsBuilder commands, PossibleOrders possible, int matter, ImmutableArray<Building> buildings, bool together)
    {
        PlayerState player = view.PlayerState;
        Compound BuildingsOn(Hexagon hexagon) => player.Compounds.GetValueOrDefault(hexagon) + player.Constructions.On(hexagon) + commands.On(hexagon).Construction;

        together = together && player.Compounds.Keys.Union(player.Armies.Keys).Any(hexagon => TrainsFighters(BuildingsOn(hexagon)));
        List<(Hexagon Hexagon, Building Building)> sites = [];
        foreach (Hexagon hexagon in Ordered(possible.Hexagons.Keys))
        {
            Compound present = BuildingsOn(hexagon);
            if (together ? TrainsFighters(present) : !present.IsEmpty)
            {
                sites.AddRange(buildings
                    .Where(building => possible.Hexagons[hexagon].Construction[building] > 0 && Costs.Construction(view.GameSettings, building).Matter <= matter)
                    .Select(building => (hexagon, building)));
            }
        }
        if (sites.Count == 0)
        {
            return false;
        }
        (Hexagon site, Building built) = sites[_random.Next(sites.Count)];
        commands.Construct(site, Compound.FromBuilding(built, 1));
        return true;
    }

    /// <summary>
    /// The hexagons worth sending a dot to: matter worth a plane or an upgrade still to research,
    /// nothing of the player there yet, and nothing of anybody else as far as the player knows.
    /// </summary>
    private List<Hexagon> Claims(PlayerGameState view)
    {
        GameSettings settings = view.GameSettings;
        PlayerState player = view.PlayerState;
        List<Hexagon> claims = [];
        foreach (Hexagon hexagon in Ordered(settings.HexagonSettings.Keys))
        {
            bool isOwn = player.Armies.GetValueOrDefault(hexagon).Dot > 0 ||
                !(player.Compounds.GetValueOrDefault(hexagon) + player.Constructions.On(hexagon)).IsEmpty;
            if (isOwn || IsForeign(view, hexagon) || KnownUnits(view, hexagon) > 0)
            {
                continue;
            }
            bool offersResearch = settings.HexagonSettings[hexagon].ResearchableUpgrades.Any(upgrade =>
                settings.NextUpgradeLevel(upgrade, player.UpgradeLevels.GetValueOrDefault(upgrade)) is not null);
            if (offersResearch || IsWorthAPlane(settings, hexagon, RemainingMatter(view, hexagon)))
            {
                claims.Add(hexagon);
            }
        }
        return claims;
    }

    /// <summary>
    /// One dot at a time walks to a hexagon to claim. It leaves a hexagon with a plane that has
    /// more dots than work, for the nearest claim.
    /// </summary>
    private void MoveExpedition(PlayerGameState view, CommandsBuilder commands, List<Hexagon> claims)
    {
        PlayerState player = view.PlayerState;
        PossibleOrders possible = Orders.Possible(view, commands.Commands);
        if (_expedition is null && claims.Count > 0)
        {
            List<Hexagon> origins = Ordered(possible.Hexagons.Keys)
                .Where(hexagon => possible.Hexagons[hexagon].Movable.Dot > 0 &&
                    player.Compounds.GetValueOrDefault(hexagon).Plane > 0 &&
                    player.Armies.GetValueOrDefault(hexagon).Dot > DotNeed(view, commands, hexagon))
                .ToList();
            if (origins.Count > 0 && _random.NextDouble() < _options.ExpeditionProbability)
            {
                Hexagon origin = origins[_random.Next(origins.Count)];
                int distance = claims.Min(origin.DistanceTo);
                _expedition = new Expedition(origin, Roll(claims.Where(x => origin.DistanceTo(x) == distance).ToList()));
            }
        }

        if (_expedition is { } expedition &&
            Limit(possible, expedition.Position).Movable.Dot > 0 &&
            Step(view, expedition.Position, expedition.Target, _oneDot, daring: false) is Hexagon step)
        {
            commands.Move(expedition.Position, step, _oneDot);
            _expedition = expedition with { Position = step };
        }
    }

    /// <summary>
    /// The fighting units of a hexagon are one army and never split. An army guards its hexagon
    /// for a rolled number of turns, then it walks to the next hexagon to guard and takes along
    /// whatever was trained in the meantime.
    /// </summary>
    private void MoveArmies(PlayerGameState view, CommandsBuilder commands)
    {
        PossibleOrders possible = Orders.Possible(view, commands.Commands);
        // An army that gets no more units has nothing to wait for: it attacks whatever the odds.
        bool daring = Income(view, commands) <= 0 &&
            view.PlayerState.Trainings.IsEmpty &&
            commands.Commands.Values.All(x => x.Training.IsEmpty);
        Dictionary<Hexagon, Guard> guards = [];
        foreach (Hexagon hexagon in Ordered(possible.Hexagons.Keys))
        {
            Army army = possible.Hexagons[hexagon].Movable with { Dot = 0 };
            if (army.IsEmpty)
            {
                continue;
            }
            Guard guard = _guards.TryGetValue(hexagon, out Guard known) ? known : new Guard(hexagon, null);
            if (guard.Target != hexagon && IsContested(view, guard.Target) && !CanTake(view, guard.Target, army, daring))
            {
                // What it set out for has grown too strong since: it stays where it is.
                guard = new Guard(hexagon, null);
            }
            if (guard.Target == hexagon)
            {
                guard = guard with { Until = guard.Until ?? view.Turn + _random.Next(_options.MinGuardTurns, _options.MaxGuardTurns + 1) };
                if (view.Turn < guard.Until)
                {
                    guards.TryAdd(hexagon, guard);
                    continue;
                }
                guard = new Guard(RollGuardTarget(view, hexagon, army, daring), null);
            }
            if (Step(view, hexagon, guard.Target, army, daring) is Hexagon step)
            {
                commands.Move(hexagon, step, army);
                guards.TryAdd(step, guard);
            }
            else
            {
                guards.TryAdd(hexagon, guard);
            }
        }
        _guards = guards;
    }

    /// <summary>
    /// The next hexagon to guard, or the origin if there is none. A hexagon somebody else holds
    /// is only a target for an army that can take it, and weighs most. Where its units are
    /// trained an army gathers: it leaves for nothing else. Once on its way it also guards the
    /// hexagons with own buildings and the free ones with matter or upgrades.
    /// </summary>
    private Hexagon RollGuardTarget(PlayerGameState view, Hexagon origin, Army army, bool daring)
    {
        bool gathers = TrainsFighters(view.PlayerState.Compounds.GetValueOrDefault(origin));
        List<(Hexagon Hexagon, double Weight)> targets = [];
        foreach (Hexagon hexagon in Ordered(view.GameSettings.HexagonSettings.Keys))
        {
            HexagonSettings hexagonSettings = view.GameSettings.HexagonSettings[hexagon];
            if (hexagon == origin)
            {
                continue;
            }
            if (IsContested(view, hexagon))
            {
                if (CanTake(view, hexagon, army, daring))
                {
                    targets.Add((hexagon, _options.OpponentHexagonWeight));
                }
            }
            else if (gathers)
            {
                continue;
            }
            else if (!view.PlayerState.Compounds.GetValueOrDefault(hexagon).IsEmpty)
            {
                targets.Add((hexagon, _options.OwnHexagonWeight));
            }
            else if (hexagonSettings.Matter > 0 || !hexagonSettings.ResearchableUpgrades.IsEmpty)
            {
                targets.Add((hexagon, 1));
            }
        }
        if (targets.Count == 0)
        {
            return origin;
        }
        double roll = _random.NextDouble() * targets.Sum(x => x.Weight);
        foreach ((Hexagon hexagon, double weight) in targets)
        {
            roll -= weight;
            if (roll < 0)
            {
                return hexagon;
            }
        }
        return targets[^1].Hexagon;
    }

    /// <summary>
    /// The hexagon the whole army reaches this turn that lies nearest to the target, or null
    /// if none brings it closer.
    /// </summary>
    private Hexagon? Step(PlayerGameState view, Hexagon origin, Hexagon target, Army army, bool daring)
    {
        List<Hexagon> steps = Ordered(Movement.Destinations(view.GameSettings, origin, army)
            .Where(x => x.Value == army && (x.Key == target || IsSafeFor(view, x.Key, army, daring)))
            .Select(x => x.Key)).ToList();
        if (steps.Count == 0)
        {
            return null;
        }
        // A step to the side is taken too: it leads around a hexagon that is not safe.
        int distance = steps.Min(target.DistanceTo);
        if (distance > origin.DistanceTo(target))
        {
            return null;
        }
        return Roll(steps.Where(x => target.DistanceTo(x) == distance).ToList());
    }

    /// <summary>
    /// Whether the army outnumbers the units of everybody else the player knows of on the hexagon.
    /// A daring army asks no more.
    /// </summary>
    private bool IsSafeFor(PlayerGameState view, Hexagon hexagon, Army army, bool daring)
    {
        return daring || KnownUnits(view, hexagon) < army.Total;
    }

    private Hexagon Roll(List<Hexagon> hexagons)
    {
        return hexagons[_random.Next(hexagons.Count)];
    }

    private static int Income(PlayerGameState view, CommandsBuilder commands)
    {
        return Harvest.Forecast(view, commands.Commands).Values.Sum(x => x.Matter);
    }

    private static Compound Ordered(CommandsBuilder commands)
    {
        return commands.Commands.Values.Select(x => x.Construction).Sum();
    }

    private static PossibleHexagonOrders Limit(PossibleOrders possible, Hexagon hexagon)
    {
        return possible.Hexagons.GetValueOrDefault(hexagon) ?? _noOrders;
    }

    /// <summary>
    /// A dictionary has no order; the dice need one, so that a seed always gives the same play.
    /// </summary>
    private static IEnumerable<Hexagon> Ordered(IEnumerable<Hexagon> hexagons)
    {
        return hexagons.OrderBy(x => x.X).ThenBy(x => x.Y);
    }

    /// <summary>
    /// The dot on its way to claim the target.
    /// </summary>
    private readonly record struct Expedition(Hexagon Position, Hexagon Target);

    /// <summary>
    /// The hexagon an army guards or walks to, and until which turn it stays once it is there.
    /// </summary>
    private readonly record struct Guard(Hexagon Target, int? Until);
}
