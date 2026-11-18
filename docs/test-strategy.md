# Test Strategy

This document defines which kinds of tests exist in Simulturn, what each one protects, and where it lives. Every new test belongs to exactly one layer. What is not listed here is not built until there is a need for it.

## Principles

The engine (`Simulturn.Core`) is deterministic and pure: `TurnResolution.Resolve` depends only on the state and the commands. Everything builds on that.

- Every test is deterministic. There are no retries. A red test is a bug or a wrong test, never bad luck.
- Every test reads on its own: Arrange, Act, Assert, one scenario per test, the name describes the scenario.
- Test data comes from one source (`Simulturn.Test.Common`). No test class carries its own copy of the game settings.

## Layers

| Layer | What it checks | Oracle | Project |
|---|---|---|---|
| 1 Unit | One rule, one turn: a single validation type of `CommandValidator.Validate`, a single phase of `TurnResolution.Resolve`, the value objects `Army`, `Compound`, `Hexagon`, fog of war | Exact values | `Simulturn.Core.Test` |
| 2 Scenario | Several scripted turns whose interplay has rules (a fight while a training is pending, a research completing and then a fight) | Exact values | `Simulturn.Core.Test` |
| 3 Invariants | Random games over many seeds, maps and option presets | Invariants after every turn, no exact values | `Simulturn.Core.Test` |
| 4 Bots | `Match`, the arena of the lab, the bots themselves (`RandomBotTest`) | Exact outcome of scripted bots (winner, turn count, rows of a run) | `Simulturn.Bots.Test` (`Match`), `Simulturn.Lab.Test` (arena) |

Not planned: integration tests across process or system boundaries. There is no API, no database, no UI. When they arrive, they get their own tests there.

### Layer 1: Unit

Goal: every rule of the engine has at least one test that shows it in isolation.

- Every case of the `CommandValidation` union has a test that produces exactly that validation, and a neighbour that shows when it does not appear.
- Every phase of `TurnResolution.Resolve` (income, training, construction, research, movement, fight, building destruction, cancellation of constructions and trainings, defeat) has a test with exactly one turn.
- Every rule a client can ask (`Costs`, `Harvest`, `Occupancy`, `Schedule`, `Orders`, `Destruction`, `Standing`, `Game`) has tests of the function itself and one test that shows that `TurnResolution.Resolve` comes to the same result.
- One class per rule area, for example `ValidatePendingWorkTest`, `NextTurnIncomeTest`, `FightTest`. The existing `GameStateTest` is not split in one go. When an area is touched, its tests move into the matching class.

### Layer 2: Scenario

Goal: rules that only show over several turns. The turns are scripted explicitly, the state after every turn is checked exactly. Existing examples: `Fight_Army_vs_Army_InTwoTurns`, `Memory_GetsStaleWhileOutOfSightAndIsRenewedBySeeingAgain`.

### Layer 3: Invariants

Goal: find what nobody scripted. `RandomCommands` gives the commands of both players over many seeds, maps (one of them with neutral armies) and option presets. After every turn, invariants are checked that must hold in every reachable state. `RandomCommands` only gets the view of its player and only produces valid commands, so every failure is an engine bug. It is a test tool, not a bot.

State invariants (`StateInvariants.CheckState`, hold for a single state):

- `StateValidator.Validate` yields nothing.
- Space: `UsedSpace` equals armies plus pending trainings times `RequiredSpace`. `AvailableSpace` equals compounds times `ProvidedSpace`.
- Matter, used space and available space are never negative.
- A defeated player has no pending trainings and no pending researches.
- No neutral army is empty, and none stands on the buildings of a player.
- Fog of war: the player view matches the visibility that follows from the player's armies. Hidden hexagons carry no data, visible hexagons carry the true data. What a player saw in earlier turns is not part of the view; `FogOfWar.UpdateMemories` and `FogOfWar.GetMemories` have their own unit tests (`MemoryTest`).

Transition invariants (`StateInvariants.CheckTransition`, need the previous state, the commands and the `TurnResult`):

- The turn counter advances by one. Players and hexagons do not change.
- Matter conservation: the matter all players gained equals the matter harvested from the map minus the cost of all training, construction and research commands.
- The report explains the transition: previous state plus commands plus `TurnReport` gives the armies, compounds, matter, space, pending orders and upgrade levels of the next state, per player and hexagon. The comparison only checks the report; the engine never derives the report from it.
- Every fight can be computed again from its report: `Combat.Fight` with the armies and the upgrade levels of a reported fight gives the reported losses, and no army loses more than it has. The reported upgrade levels are the ones from the start of the turn that act on the army (`UpgradeRules.ActingOn`).
- A neutral army only changes by its losses in a reported fight.
- The reported defeats are the players that are defeated after the turn and were not before.
- Every reported movement stays within the range: `Movement.ReachableArmy` with the distance of the movement gives the whole moved army.

Order invariants (`OrderInvariants.Check`, need the view of a player and the commands entered so far):

- What `Orders.Possible` offers is what `CommandValidator.Validate` accepts: every training, construction and movement is accepted up to its limit and refused with one more, and a research is offered exactly when it is accepted. The commands of the random player serve as a turn half entered, the empty commands as its start.
- The budget is the matter and the space of the state minus the cost of the commands.

Determinism (own tests):

- The same settings and the same commands produce the same state, also when the command dictionaries are enumerated in a different order. `GameStateSnapshot` renders a state as a canonical string for this comparison.

The invariants are test code, not part of `StateValidator.Validate`. `StateValidator.Validate` checks what a client must be able to check on a single state. The invariants may know the previous state and the commands, and they may be expensive.

Scale: the number of seeds and turns is a constant in the test. The whole suite runs in a few seconds. If a test ever takes more than a few seconds, it gets a TUnit category so it can be excluded from the default run. Whoever wants more raises the constant locally.

### Layer 4: Bots

Goal: `Match` and the arena keep their contract. The tests use scripted bots (`ScriptedBot`) on small hand-built maps, so every outcome is exact: who wins, after how many turns, which rows a run has. One test plays the bots of the catalog on the maps of the arena, with small limits.

A test of a bot shows a behaviour of that bot on a scripted situation (`RandomBotTest`: where it trains a dot, what it keeps back, that its army moves as a whole). A bot that rolls dice also gets a test that the same seed plays the same match, and one that a mirror match on every map type issues only valid commands. How strong a bot is against another one is not a test: that is a run of the lab (`Source/Simulturn.Lab`), read by a human.

## Known bugs

A test that fails because of a known, not yet fixed bug is written anyway and marked `[Skip("<work-item-id>: <reason>")]`. The fix removes the skip. This way the suite describes the intended behaviour, and the fix already has its test.

Example: until `research-cost-and-duration` was fixed, matter conservation failed in games with research because research cost nothing. The invariant test carried the skip, the fix removed it.

## Projects

```
Test/
  Simulturn.Test.Common/    class library, referenced by the test projects
    TestSettings.cs         the standard rules for tests, delegates to DefaultSettings in Simulturn.Core
    StateInvariants.cs      the invariants of layer 3, also usable in scenario tests
    GameStateSnapshot.cs    canonical string of a state for determinism checks
    RandomCommands.cs       valid random commands for one player, the command source of layer 3
    GameStateExtensions.cs  PlayTurn: validate the commands, resolve the turn, check the state invariants; ResolveTurn does the same and also returns the report
  Simulturn.Core.Test/      layers 1, 2 and 3
  Simulturn.Bots.Test/      layer 4, tests Simulturn.Bots; holds ScriptedBot
  Simulturn.Lab.Test/       layer 4, tests Simulturn.Lab; compiles ScriptedBot as a linked file
  Simulturn.Core.Console/   not a test, manual play
```

`Simulturn.Test.Common` references `Simulturn.Core` and Shouldly, never a bot project. Production code gets no test dependency. The standard rules and the maps live in production code, in `Simulturn.Core/Setup`: `DefaultSettings` is the only copy of the rules, `Maps` builds fixed maps by hand, `MapGenerator` builds fair maps from `MapGeneratorOptions` (types `Open`, `HomeOnly`, `Expansions`, `Guarded`). `TestSettings` delegates to `DefaultSettings`; console and tests share that source.

## Conventions

- TUnit and Shouldly. No `Retry`.
- Test names: `Scenario_Expectation`, for example `TrainingWithoutBuilding_YieldsMissingBuildingForTraining`.
- A test builds its state with `TestSettings` and `Maps` (a small hand-built map for layers 1 and 2) or with `DefaultSettings.Create(MapGeneratorOptions)` (a generated map for layer 3), changes it only through `TurnResolution.Resolve` (via `PlayTurn`), and validates commands before resolving them, unless the test checks the opposite.
- A test asserts validation types exactly (`ShouldBeOfType<...>`), not just "not empty".

## Commands

```bash
dotnet test Simulturn.sln
dotnet test Test/Simulturn.Bots.Test/Simulturn.Bots.Test.csproj
dotnet test Test/Simulturn.Core.Test/Simulturn.Core.Test.csproj -- --treenode-filter "/*/*/*/ValidatePendingWorkTest"
```
