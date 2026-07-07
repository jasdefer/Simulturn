using Simulturn.AI;
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

    /// <summary>Which AI (if any) controls each player id. Absent = human.</summary>
    private readonly Dictionary<string, AiKind> _aiAssignments = [];

    /// <summary>Live AI instances for the current game (stateful across turns; recreated per game).</summary>
    private readonly Dictionary<string, IArtificialPlayer> _aiPlayers = [];

    /// <summary>Per-player message when an AI failed to produce valid commands last resolution.</summary>
    public Dictionary<string, string> AiTurnErrors { get; } = [];

    public bool IsAi(string playerId) => _aiAssignments.ContainsKey(playerId);

    public string? AiName(string playerId) => _aiAssignments.GetValueOrDefault(playerId)?.Name;

    public IEnumerable<string> HumanPlayerIds => PlayerIds.Where(playerId => !IsAi(playerId));

    public bool HasGame => History.Count > 0;

    public GameState Current => History[^1];

    public GameState Viewed => History[ViewedIndex];

    public bool IsViewingPast => ViewedIndex < History.Count - 1;

    public IReadOnlyList<string> PlayerIds { get; private set; } = [];

    public void StartNewGame(GameSettings settings, GameMode mode,
        IReadOnlyDictionary<string, AiKind>? aiAssignments = null)
    {
        Settings = settings;
        Mode = mode;
        History.Clear();
        Reports.Clear();
        History.Add(new GameState(settings));
        ViewedIndex = 0;
        PlayerIds = [.. Current.PlayerIds.Order()];

        // (Re)create fresh AI instances — they may keep internal state across turns.
        _aiAssignments.Clear();
        _aiPlayers.Clear();
        AiTurnErrors.Clear();
        if (aiAssignments is not null)
        {
            int index = 0;
            foreach (var playerId in PlayerIds)
            {
                if (aiAssignments.TryGetValue(playerId, out var kind))
                {
                    _aiAssignments[playerId] = kind;
                    _aiPlayers[playerId] = kind.Create(settings.Seed + index);
                }
                index++;
            }
        }

        Drafts.Clear();
        DraftsVersion++;
        ValidationResults.Clear();
        ValidationCrash = null;
        StateIssues = [];
        SubmittedPlayers.Clear();
        // The active player is always a human; AI players never take a seat.
        ActivePlayerId = HumanPlayerIds.FirstOrDefault();
        ShowHandoff = Mode == GameMode.HotSeat && ActivePlayerId is not null;
        RaiseChanged();
    }

    public void RestartSameSettings()
    {
        if (Settings is not null)
        {
            // Snapshot: StartNewGame clears _aiAssignments before rebuilding from the argument.
            var assignments = new Dictionary<string, AiKind>(_aiAssignments);
            StartNewGame(Settings, Mode, assignments);
        }
    }

    public void SetActivePlayer(string playerId)
    {
        // Only human players are editable; AI players issue their own orders.
        if (ActivePlayerId != playerId && PlayerIds.Contains(playerId) && !IsAi(playerId))
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
        // Only humans take a seat; AI players' commands are generated at resolution.
        string? next = HumanPlayerIds.FirstOrDefault(playerId => !SubmittedPlayers.Contains(playerId));
        if (next is null)
        {
            ResolveTurn(); // resets ActivePlayerId to the first human and clears submissions
            ShowHandoff = ActivePlayerId is not null;
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
        ActivePlayerId = HumanPlayerIds.FirstOrDefault();
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

    /// <summary>
    /// The commands resolved this turn: human players' staged drafts, plus each AI player's
    /// commands generated fresh from its fog-of-war view. AI failures are caught and surfaced
    /// via <see cref="AiTurnErrors"/> so a misbehaving AI never breaks the turn.
    /// </summary>
    private Dictionary<string, Dictionary<Hexagon, Command>> BuildCommands()
    {
        var commands = Drafts
            .Where(player => !IsAi(player.Key))
            .ToDictionary(
                player => player.Key,
                player => player.Value
                    .Where(draft => !draft.Value.IsEmpty)
                    .ToDictionary(draft => draft.Key, draft => draft.Value.ToCommand()));

        AiTurnErrors.Clear();
        foreach ((string playerId, IArtificialPlayer ai) in _aiPlayers)
        {
            commands[playerId] = GenerateAiCommands(playerId, ai);
        }
        return commands;
    }

    private Dictionary<Hexagon, Command> GenerateAiCommands(string playerId, IArtificialPlayer ai)
    {
        try
        {
            var view = Current.GetPlayerGameState(playerId);
            var produced = ai.GetCommands(view);
            var commands = produced.ToDictionary(entry => entry.Key, entry => entry.Value);
            var errors = Current.Validate(playerId, commands).ToList();
            if (errors.Count > 0)
            {
                AiTurnErrors[playerId] = $"{ai.Name} produced {errors.Count} invalid command(s); skipped this turn.";
                return [];
            }
            return commands;
        }
        catch (Exception exception)
        {
            AiTurnErrors[playerId] = $"{ai.Name} failed ({exception.GetType().Name}: {exception.Message}); skipped this turn.";
            return [];
        }
    }

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
