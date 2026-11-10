# Test Strategy

This document defines which kinds of tests exist in Simulturn, what each one protects, and where it lives. Every new test belongs to exactly one layer. What is not listed here is not built until there is a need for it.

## Principles

The engine (`Simulturn.Core`) is deterministic and pure: `NextTurn` depends only on the state and the commands. Everything builds on that.

- Every test is deterministic. There are no retries. A red test is a bug or a wrong test, never bad luck.
- Every test reads on its own: Arrange, Act, Assert, one scenario per test, the name describes the scenario.
- Test data comes from one source (`Simulturn.Test.Common`). No test class carries its own copy of the game settings.

## Layers

| Layer | What it checks | Oracle | Project |
|---|---|---|---|
| 1 Unit | One rule, one turn: a single validation type of `Validate`, a single phase of `NextTurn`, the value objects `Army`, `Compound`, `Hexagon`, fog of war | Exact values | `Simulturn.Core.Test` |
| 2 Scenario | Several scripted turns whose interplay has rules (a fight while a training is pending, a research completing and then a fight) | Exact values | `Simulturn.Core.Test` |
| 3 Invariants | Random games over many seeds, maps and player options | Invariants after every turn, no exact values | `Simulturn.AI.Test` |
| 4 Game flow | `GameRunner`, `Arena`, and the strength of the AI players | Plausible outcome (winner, turn count, win rate) | `Simulturn.AI.Test` |

Not planned: integration tests across process or system boundaries. There is no API, no database, no UI. When they arrive, they get their own tests there.

### Layer 1: Unit

Goal: every rule of the engine has at least one test that shows it in isolation.

- Every case of the `CommandValidation` union has a test that produces exactly that validation, and a neighbour that shows when it does not appear.
- Every phase of `NextTurn` (income, training, construction, research, movement, fight, building destruction, cancellation of constructions and trainings) has a test with exactly one turn.
- One class per rule area, for example `ValidatePendingWorkTest`, `NextTurnIncomeTest`, `FightTest`. The existing `GameStateTest` is not split in one go. When an area is touched, its tests move into the matching class.

### Layer 2: Scenario

Goal: rules that only show over several turns. The turns are scripted explicitly, the state after every turn is checked exactly. Existing examples: `Fight_Army_vs_Army_InTwoTurns`, `Scouting_MemoryPersistsAndGetsStaleAfterLosingVision`.

### Layer 3: Invariants

Goal: find what nobody scripted. `RandomPlayer` plays against `RandomPlayer` over many seeds, maps and option presets. After every turn, invariants are checked that must hold in every reachable state.

State invariants (`StateInvariants.CheckState`, hold for a single state):

- `IsValid` yields nothing.
- Space: `UsedSpace` equals armies plus pending trainings times `RequiredSpace`. `AvailableSpace` equals compounds times `ProvidedSpace`.
- Matter, used space and available space are never negative.
- Fog of war: the player view matches the player's visibility. Hidden hexagons carry no current data, visible hexagons carry the true data.

Transition invariants (`StateInvariants.CheckTransition`, need the previous state and the commands):

- The turn counter advances by one. Players and hexagons do not change.
- Matter conservation: the matter all players gained equals the matter harvested from the map minus the cost of all training, construction and research commands.

Determinism (own tests):

- The same settings and the same commands produce the same state, also when the command dictionaries are enumerated in a different order. `GameStateSnapshot` renders a state as a canonical string for this comparison.

The invariants are test code, not part of `IsValid`. `IsValid` checks what a client must be able to check on a single state. The invariants may know the previous state and the commands, and they may be expensive.

Scale: the number of seeds and turns is a constant in the test. The whole test run stays well under a minute. Whoever wants more raises the constant locally.

### Layer 4: Game flow

Goal: `GameRunner` and `Arena` keep their contract, and the AI players have the strength their documentation claims. These tests are probabilistic in outcome but deterministic in execution (fixed seeds). Today the whole suite runs in about 25 seconds, most of it in the random games of layer 3. If a test ever takes more than a few seconds, it gets a TUnit category so it can be excluded from the default run.

## Known bugs

A test that fails because of a known, not yet fixed bug is written anyway and marked `[Skip("<work-item-id>: <reason>")]`. The fix removes the skip. This way the suite describes the intended behaviour, and the fix already has its test.

Example: until `research-cost-and-duration` was fixed, matter conservation failed in games with research because research cost nothing. The invariant test carried the skip, the fix removed it.

## Projects

```
Test/
  Simulturn.Test.Common/    class library, referenced by both test projects
    TestSettings.cs         the standard rules for tests, delegates to DefaultSettings in Simulturn.Core
    StateInvariants.cs      the invariants of layer 3, also usable in scenario tests
    GameStateSnapshot.cs    canonical string of a state for determinism checks
    GameStateExtensions.cs  PlayTurn: validate the commands, resolve the turn, check the state invariants
  Simulturn.Core.Test/      layers 1 and 2
  Simulturn.AI.Test/        layers 3 and 4, plus ScriptedPlayer and IdlePlayer based helpers
  Simulturn.Core.Console/   not a test, manual play
```

`Simulturn.Test.Common` references `Simulturn.Core` and Shouldly, never `Simulturn.AI`. Production code gets no test dependency. The standard rules and the maps live in production code, in `Simulturn.Core/Setup`: `DefaultSettings` is the only copy of the rules, `Maps` builds fixed maps by hand, `MapGenerator` builds fair maps from `MapGeneratorOptions` (types `Open`, `HomeOnly`, `Expansions`). `TestSettings` delegates to `DefaultSettings`; console, arena and tests share that source.

## Conventions

- TUnit and Shouldly. No `Retry`.
- Test names: `Scenario_Expectation`, for example `TrainingWithoutBuilding_YieldsMissingBuildingForTraining`.
- A test builds its state with `TestSettings` and `Maps` (a small hand-built map for layers 1 and 2) or with `DefaultSettings.Create(MapGeneratorOptions)` (a generated map for layers 3 and 4), changes it only through `NextTurn` (via `PlayTurn`), and validates commands before resolving them, unless the test checks the opposite.
- A test asserts validation types exactly (`ShouldBeOfType<...>`), not just "not empty".

## Commands

```bash
dotnet test Simulturn.sln
dotnet test Test/Simulturn.Core.Test/Simulturn.Core.Test.csproj -- --treenode-filter "/*/*/*/ValidatePendingWorkTest"
```
