# AGENTS Guide for Simulturn
This file is for coding agents working in `C:\Users\justu\source\repos\Simulturn`.
Use it as the default workflow and style baseline for this repository.

## Repository Snapshot
- Solution: `Simulturn.sln`
- Runtime: .NET 11 (`net11.0`, C# 15), SDK pinned in `global.json`
- Core project: `Source/Simulturn.Core/Simulturn.Core.csproj`
- Test projects: `Test/Simulturn.Core.Test` (engine), `Test/Simulturn.AI.Test` (players, runner, invariants), `Test/Simulturn.Test.Common` (shared fixtures)
- Test strategy: `docs/test-strategy.md` (read before adding tests)
- Console project: `Test/Simulturn.Core.Console/Simulturn.Core.Console.csproj`
- Test stack: TUnit + Shouldly
- Shared project settings (`TargetFramework`, `Nullable`, `ImplicitUsings`, `EnforceCodeStyleInBuild`) live in `Directory.Build.props`; package versions in `Directory.Packages.props` (central package management)

## Cursor and Copilot Rules
No agent rules files were found when this file was generated:
- No `.cursorrules`
- No `.cursor/rules/`
- No `.github/copilot-instructions.md`

If any of these appear later, treat them as higher-priority constraints and update this file.

## Build / Lint / Test Commands
Run commands from repo root.

### Restore
```bash
dotnet restore Simulturn.sln
```

### Build
```bash
dotnet build Simulturn.sln
dotnet build Simulturn.sln -c Release
```

### Test all
```bash
dotnet test Simulturn.sln
dotnet test Test/Simulturn.Core.Test/Simulturn.Core.Test.csproj
```

### Run one test (important)
TUnit ignores `--filter`. Use the tree node filter after `--`; the pattern is `/Assembly/Namespace/Class/Method` and `*` matches a whole segment.
```bash
dotnet test Test/Simulturn.Core.Test/Simulturn.Core.Test.csproj -- --treenode-filter "/*/*/*/CancelTrainingsAfterCompoundDestruction"
```

All tests of one class:
```bash
dotnet test Test/Simulturn.Core.Test/Simulturn.Core.Test.csproj -- --treenode-filter "/*/*/GameStateTest/*"
```

### List discovered tests
```bash
dotnet test Test/Simulturn.Core.Test/Simulturn.Core.Test.csproj --list-tests
```

### Lint / formatting
`.editorconfig` at the repo root enables CA1852 (seal internal types) and the file-scoped namespace rule; `EnforceCodeStyleInBuild` turns style violations into build warnings.
Use `dotnet format` for formatting:
```bash
dotnet format Simulturn.sln
dotnet format Simulturn.sln --verify-no-changes
```

### Run console harness
```bash
dotnet run --project Test/Simulturn.Core.Console/Simulturn.Core.Console.csproj
```

## Current Build Health
At generation time, `dotnet build Simulturn.sln` and
`dotnet test Test/Simulturn.Core.Test/Simulturn.Core.Test.csproj` passed.

## Project Structure Expectations
- Core game model types live in `Source/Simulturn.Core/Model/`
- `Source/Simulturn.Core/Model/` holds data only: `GameState` and `PlayerState` have no rule logic
- Everything that applies rules lives in `Source/Simulturn.Core/Engine/` (namespace `Simulturn.Core.Engine`): `TurnResolution.Resolve` (one turn, one named method per phase), `Combat.Fight` (the fight rule, armies and upgrade levels only), `CommandValidator.Validate`, `StateValidator.Validate`, `FogOfWar` (player view, visibility)
- `Combat.Fight(settings, combatants)` is the only way to compute a fight. The turn resolution passes the armies of a hexagon with the true upgrade levels, a client that forecasts a fight passes what its player knows; both get a `FightReport`. Everything an upgrade does to a fight is applied inside `Combat`
- `TurnResolution.Resolve` returns a `TurnResult(State, Report)`. The state is the input of the next turn; the `TurnReport` (`Source/Simulturn.Core/Model/Report/`) holds what happened, each fact once and without fog of war, and never goes back into the engine. `PlayerState` holds no per-turn data
- The `TurnReport` has three parts: `Activities` (per player, what concerns only that player: income, completed and canceled orders, movements, matter), `Fights` (per hexagon) and `Destructions`. `TurnResolution` writes an event where it happens and never decides who may see it. A new event also needs a line in `StateInvariants.CheckTransition`
- The cut of a player (`PlayerTurnReport`) is the own activity, the complete fights the player took part in and the destructions as attacker or owner
- A player only gets the cut made by `FogOfWar`: `GetPlayerGameState(turnResult, playerId)` cuts state and report, the `PlayerTurnReport` sits in `PlayerGameState.Report`. `GetPlayerGameState(state, playerId)` is for a state without a resolved turn (start of the game) and yields an empty report
- The game state holds nothing about what a player knows. The visibility follows from the armies (`FogOfWar.GetVisibility`) and is never stored. A view (`PlayerGameState.Observations`) shows one turn only. What a player last saw is a `HexagonMemory` per hexagon, each part with the turn it was seen; a sighting of the current turn is what the player sees now. `FogOfWar.UpdateMemories(memories, view)` carries the memories into the next turn, `FogOfWar.GetMemories(views)` applies the same rule to all views a client has kept. The engine keeps neither views nor memories; a client does (the AI players hold their memories themselves)
- Pending trainings and constructions are flat: `ImmutableDictionary<TurnHexagon, Army>` and `<TurnHexagon, Compound>`, keyed by completion turn and hexagon. Write them with `Merge(key, value)`, which drops an entry once it is empty; read a hexagon with `On(hexagon)`. A dictionary has no order: sort explicitly wherever the order matters (cancellation, output)
- `Engine` depends on `Model`; the only edge back is the `GameState` constructor building the start state
- Standard rules and maps live in `Source/Simulturn.Core/Setup/`: `DefaultSettings` (the only copy of the rules), `Maps` (fixed maps), `MapGenerator` (seeded, symmetric maps)
- Mutable transition helpers use builders in `PlayerStateBuilder` (internal working state of `TurnResolution`)
- Cross-cutting helpers are in `Source/Simulturn.Core/Extensions/`
- Global aliases/usings are in `Source/Simulturn.Core/GlobalUsings.cs`

## Code Style Guidelines
Follow local conventions in touched files first.

### Language and formatting
- Keep file-scoped namespaces: `namespace X.Y;`
- Keep nullable-safe code; do not disable nullable checks casually
- Use collection expressions (`[]`) where existing code already uses them
- Do not introduce unrelated reformatting in files you touch

### Imports and usings
- Keep usings minimal and relevant to the file
- Reuse the global usings in `GlobalUsings.cs`; do not add type aliases for nested collections, flatten the collection instead
- Prefer normal `using` imports over fully-qualified names in method bodies
- Add to `GlobalUsings.cs` only when a using is truly cross-cutting

### Types and immutability
- Use `record` for immutable state aggregates (`GameState`, `PlayerState` patterns)
- Use `readonly struct` for value objects (`Army`, `Hexagon` patterns)
- Seal every class and record that has no derived types, public ones too; CA1852 only warns for internal types, public types are sealed by convention
- Value objects implement `IEquatable<T>`, `GetHashCode` and `==`/`!=` (`Army`, `Compound` patterns)
- Keep domain objects strongly typed; avoid weakly typed dictionaries/arrays
- Preserve immutable collection boundaries for stored state
- Use `.ToBuilder()` only inside mutation-heavy logic, then convert back to immutable
- Data that is built once and read every turn (`GameSettings` maps) uses `FrozenDictionary`; state that changes per turn uses `ImmutableDictionary`
- Accept `IReadOnlyDictionary`/`IReadOnlyList` as parameters, never store them as properties
- Static enum mappings are switch expressions (`GameSettings.TrainingBuilding`), not dictionaries
- Closed sets of result types are `union` types (C# 15): `CommandValidation` and `StateValidation` in `Model/State/Validation/`. A `switch` over them is exhaustive, the cases are plain sealed records without a shared interface. In tests, match the case through `.Value` (`validation.Value.ShouldBeOfType<...>()`), the union itself is a struct

### Naming conventions
- Public types, methods, properties: PascalCase
- Locals and parameters: camelCase
- Private static readonly fields: `_camelCase`
- Keep domain naming consistent (`Armies`, `Compounds`, `Trainings`, `Researches`)
- Test names should be explicit scenario descriptions

### `var` and explicit types
- Prefer explicit types in core domain logic when readability benefits
- Use `var` when RHS type is obvious or in LINQ/anonymous projections
- Explicit tuple typing in loops is common and acceptable in this codebase

### Control flow and validation
- Use guard clauses and early `continue`/`return` to keep nesting shallow
- Keep command validation (`CommandValidator.Validate`) separate from state validity checks (`StateValidator.Validate`)
- Return domain validation objects for business-rule failures (cases of the `CommandValidation` union)
- Throw exceptions only for invariant violations or impossible states

### Error handling
- Use specific exceptions (`InvalidOperationException`, `ArgumentOutOfRangeException`)
- Include clear messages for thrown exceptions
- Do not silently swallow errors in core game logic
- Keep failures deterministic and reproducible where possible

### Collections and LINQ
- Prefer existing helper extensions (`Merge`, `MergeOrOverwrite`, `Sum`)
- New extension members go into `extension(...)` blocks (C# 14), not `this`-parameter methods
- Keep LINQ readable; split long chains if intent is hard to parse
- Avoid repeated expensive enumerations in hot paths
- Favor clarity over terse one-liners in state transition code

### Testing conventions
- Follow `docs/test-strategy.md`: pick the layer, reuse `Simulturn.Test.Common`, no new settings copies
- Use TUnit attributes (`[Test]`, hooks) and Shouldly assertions
- Keep Arrange/Act/Assert structure, especially in larger scenarios
- Reuse helper setup methods for repeated game state construction; never copy the game settings, start from `DefaultSettings`
- Assert exact outcomes and validation types when behavior is strict
- For new game mechanics, add both happy-path and invalid-command tests

### Comments and docs
- Add comments only for non-obvious invariants/algorithms
- Prefer expressive naming over explanatory comments
- Add XML docs for subtle public behavior

## Agent Working Agreement
- Keep changes minimal, focused, and architecture-consistent
- Avoid introducing new dependencies unless clearly necessary
- Run targeted tests for impacted behavior whenever possible
- If full test suite cannot run, report what was run and why it was limited
- Update `AGENTS.md` when commands, tooling, or style conventions change
