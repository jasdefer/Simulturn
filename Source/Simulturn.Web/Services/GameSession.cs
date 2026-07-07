using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.State;
using Simulturn.Core.Model.State.StateValidation;
using Simulturn.Web.Models;

namespace Simulturn.Web.Services;

/// <summary>
/// Single source of truth for one game session. Scoped: one Blazor Server circuit = one browser
/// tab = one game; hot seat works because all players share the circuit. All mutations happen on
/// the circuit's dispatcher (UI event handlers), so no locking is required.
/// </summary>
public sealed class GameSession
{
    public event Action? Changed;

    public GameSettings? Settings { get; private set; }

    /// <summary>All states of the current game; index 0 is turn 0. Immutable states make history cheap.</summary>
    public List<GameState> History { get; } = [];

    /// <summary>Reports[i] describes the resolution History[i] → History[i+1].</summary>
    public List<TurnReport> Reports { get; } = [];

    /// <summary>Index into <see cref="History"/> currently shown; the last index is the live state.</summary>
    public int ViewedIndex { get; private set; }

    public GameMode Mode { get; private set; }

    /// <summary>Whose orders are being edited (god mode) or whose turn it is (hot seat).</summary>
    public string? ActivePlayerId { get; private set; }

    public HashSet<string> SubmittedPlayers { get; } = [];

    public bool ShowHandoff { get; private set; }

    /// <summary>Staged draft commands per player per hexagon.</summary>
    public Dictionary<string, Dictionary<Hexagon, DraftCommand>> Drafts { get; } = [];

    /// <summary>Incremented on every draft change; lets consumers cache projections cheaply.</summary>
    public int DraftsVersion { get; private set; }

    public Dictionary<string, List<ICommandValidation>> ValidationResults { get; } = [];

    /// <summary>Set when the engine's Validate threw instead of yielding validation errors.</summary>
    public string? ValidationCrash { get; private set; }

    /// <summary>Engine consistency violations found by IsValid() after the last resolution — engine bugs.</summary>
    public List<IStateValidation> StateIssues { get; private set; } = [];

    public bool HasGame => History.Count > 0;

    public GameState Current => History[^1];

    public GameState Viewed => History[ViewedIndex];

    public bool IsViewingPast => ViewedIndex < History.Count - 1;

    public IReadOnlyList<string> PlayerIds { get; private set; } = [];

    public void StartNewGame(GameSettings settings, GameMode mode)
    {
        Settings = settings;
        Mode = mode;
        History.Clear();
        Reports.Clear();
        History.Add(new GameState(settings));
        ViewedIndex = 0;
        PlayerIds = [.. Current.PlayerIds.Order()];
        Drafts.Clear();
        DraftsVersion++;
        ValidationResults.Clear();
        ValidationCrash = null;
        StateIssues = [];
        SubmittedPlayers.Clear();
        ActivePlayerId = PlayerIds.FirstOrDefault();
        ShowHandoff = Mode == GameMode.HotSeat;
        RaiseChanged();
    }

    public void RestartSameSettings()
    {
        if (Settings is not null)
        {
            StartNewGame(Settings, Mode);
        }
    }

    public void SetActivePlayer(string playerId)
    {
        if (ActivePlayerId != playerId && PlayerIds.Contains(playerId))
        {
            ActivePlayerId = playerId;
            RaiseChanged();
        }
    }

    /// <summary>
    /// Hot seat: the active player is done — hand off to the next unsubmitted player, or resolve
    /// the turn once everyone has submitted. Always raises the hand-off interstitial so the board
    /// is covered before the next player's view renders.
    /// </summary>
    public void SubmitActivePlayer()
    {
        if (Mode != GameMode.HotSeat || ActivePlayerId is null || IsViewingPast)
        {
            return;
        }
        SubmittedPlayers.Add(ActivePlayerId);
        string? next = PlayerIds.FirstOrDefault(playerId => !SubmittedPlayers.Contains(playerId));
        if (next is null)
        {
            ResolveTurn(); // resets ActivePlayerId to the first player and clears submissions
            ShowHandoff = true;
            RaiseChanged();
        }
        else
        {
            ActivePlayerId = next;
            ShowHandoff = true;
            RaiseChanged();
        }
    }

    /// <summary>Dismisses the hand-off interstitial; the active player takes control.</summary>
    public void BeginPlayerTurn()
    {
        if (ShowHandoff)
        {
            ShowHandoff = false;
            RaiseChanged();
        }
    }

    /// <summary>Whether the active player's staged orders are currently valid (hot-seat submit gate).</summary>
    public bool CanSubmitActivePlayer =>
        HasGame && !IsViewingPast && ActivePlayerId is not null && ValidationCrash is null &&
        ValidationResults.GetValueOrDefault(ActivePlayerId, []).Count == 0;

    public DraftCommand GetOrCreateDraft(string playerId, Hexagon hexagon)
    {
        var playerDrafts = GetOrCreatePlayerDrafts(playerId);
        if (!playerDrafts.TryGetValue(hexagon, out var draft))
        {
            draft = new DraftCommand();
            playerDrafts[hexagon] = draft;
        }
        return draft;
    }

    public DraftCommand? GetDraft(string playerId, Hexagon hexagon) =>
        Drafts.GetValueOrDefault(playerId)?.GetValueOrDefault(hexagon);

    /// <summary>Mutates a draft, prunes empty drafts, revalidates, and notifies subscribers.</summary>
    public void UpdateDraft(string playerId, Hexagon hexagon, Action<DraftCommand> mutate)
    {
        mutate(GetOrCreateDraft(playerId, hexagon));
        PruneAndRevalidate(playerId);
    }

    public void RemoveDraft(string playerId, Hexagon hexagon)
    {
        if (Drafts.TryGetValue(playerId, out var playerDrafts) && playerDrafts.Remove(hexagon))
        {
            PruneAndRevalidate(playerId);
        }
    }

    /// <summary>Discards every staged order of the player — reset and start the turn over.</summary>
    public void ClearDrafts(string playerId)
    {
        if (Drafts.Remove(playerId))
        {
            PruneAndRevalidate(playerId);
        }
    }

    /// <summary>
    /// The matter the player would harvest next resolution, given the current state and staged
    /// orders. Mirrors the engine's income phase: dots idle on a hex with an own Plane harvest,
    /// newly staged constructions each occupy a dot, capped by the hex limit and remaining matter.
    /// </summary>
    public int ProjectedIncome(string playerId)
    {
        if (!HasGame || !Current.PlayerStates.TryGetValue(playerId, out var playerState))
        {
            return 0;
        }
        var settings = Current.GameSettings;
        int total = 0;
        foreach ((Hexagon hexagon, Army army) in playerState.Armies)
        {
            if (!playerState.Compounds.TryGetValue(hexagon, out var compound) || compound.Plane == 0)
            {
                continue;
            }
            int stagedConstructions = GetDraft(playerId, hexagon)?.Construction.ToCompound().Sum() ?? 0;
            var idleArmy = army with { Dot = (short)Math.Max(0, army.Dot - stagedConstructions) };
            idleArmy = Army.Min(idleArmy, settings.HexagonSettings[hexagon].MaxNumberOfUnitsGeneratingMatter);
            int hexagonIncome = settings.Income * idleArmy;
            hexagonIncome = Math.Min(hexagonIncome, Current.RemainingMatter.GetValueOrDefault(hexagon));
            total += hexagonIncome;
        }
        return total;
    }

    public bool CanEndTurn
    {
        get
        {
            if (!HasGame || IsViewingPast)
            {
                return false;
            }
            return ValidationCrash is null && ValidationResults.Values.All(results => results.Count == 0);
        }
    }

    public void ResolveTurn()
    {
        if (!HasGame || IsViewingPast)
        {
            return;
        }
        var commands = BuildCommands();
        var before = Current;
        var next = before.NextTurn(commands);
        Reports.Add(TurnReportService.Build(before, commands, next));
        History.Add(next);
        ViewedIndex = History.Count - 1;
        StateIssues = [.. next.IsValid()];
        Drafts.Clear();
        DraftsVersion++;
        ValidationResults.Clear();
        ValidationCrash = null;
        SubmittedPlayers.Clear();
        ActivePlayerId = PlayerIds.FirstOrDefault();
        RaiseChanged();
    }

    public void ViewTurn(int index)
    {
        if (index >= 0 && index < History.Count && index != ViewedIndex)
        {
            ViewedIndex = index;
            RaiseChanged();
        }
    }

    /// <summary>Discards all turns after the currently viewed one and continues play from there.</summary>
    public void BranchFromViewed()
    {
        if (!IsViewingPast)
        {
            return;
        }
        History.RemoveRange(ViewedIndex + 1, History.Count - ViewedIndex - 1);
        Reports.RemoveRange(ViewedIndex, Reports.Count - ViewedIndex);
        Drafts.Clear();
        DraftsVersion++;
        ValidationResults.Clear();
        ValidationCrash = null;
        StateIssues = [];
        SubmittedPlayers.Clear();
        RaiseChanged();
    }

    /// <summary>
    /// Units still available to move from a hex: the current army minus movements already staged
    /// from it. When editing an existing order, pass its destination so it doesn't count against itself.
    /// </summary>
    public Army AvailableArmyForMovement(string playerId, Hexagon source, Hexagon? editedDestination = null)
    {
        Army army = Current.PlayerStates.TryGetValue(playerId, out var playerState)
            ? playerState.Armies.GetValueOrDefault(source, Army.Empty)
            : Army.Empty;
        var draft = GetDraft(playerId, source);
        if (draft is null)
        {
            return army;
        }
        foreach (var movement in draft.Movements)
        {
            if (editedDestination is { } edited && movement.Destination == edited)
            {
                continue;
            }
            army -= movement.Army.ToArmy();
        }
        return army;
    }

    private Dictionary<string, Dictionary<Hexagon, Command>> BuildCommands() =>
        Drafts.ToDictionary(
            player => player.Key,
            player => player.Value
                .Where(draft => !draft.Value.IsEmpty)
                .ToDictionary(draft => draft.Key, draft => draft.Value.ToCommand()));

    private Dictionary<Hexagon, DraftCommand> GetOrCreatePlayerDrafts(string playerId)
    {
        if (!Drafts.TryGetValue(playerId, out var playerDrafts))
        {
            playerDrafts = [];
            Drafts[playerId] = playerDrafts;
        }
        return playerDrafts;
    }

    private void PruneAndRevalidate(string playerId)
    {
        if (Drafts.TryGetValue(playerId, out var playerDrafts))
        {
            foreach (var hexagon in playerDrafts.Where(draft => draft.Value.IsEmpty).Select(draft => draft.Key).ToList())
            {
                playerDrafts.Remove(hexagon);
            }
        }
        DraftsVersion++;
        Validate(playerId);
        RaiseChanged();
    }

    private void Validate(string playerId)
    {
        var commands = Drafts.GetValueOrDefault(playerId)?
            .Where(draft => !draft.Value.IsEmpty)
            .ToDictionary(draft => draft.Key, draft => draft.Value.ToCommand()) ?? [];
        try
        {
            ValidationResults[playerId] = [.. Current.Validate(playerId, commands)];
            ValidationCrash = null;
        }
        catch (Exception exception)
        {
            // Known engine issue: Validate can throw (e.g. KeyNotFoundException when pending
            // queues exist at other hexagons) instead of yielding a validation record.
            ValidationResults[playerId] = [];
            ValidationCrash = $"Engine validation threw {exception.GetType().Name}: {exception.Message}";
        }
    }

    private void RaiseChanged() => Changed?.Invoke();
}
