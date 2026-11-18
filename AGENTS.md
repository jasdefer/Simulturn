# AGENTS Guide for Simulturn
This file is for coding agents working in `C:\Users\justu\source\repos\Simulturn`.
Use it as the default workflow and style baseline for this repository.

## Repository Snapshot
- Solution: `Simulturn.sln`
- Runtime: .NET 11 (`net11.0`, C# 15), SDK pinned in `global.json`
- Core project: `Source/Simulturn.Core/Simulturn.Core.csproj`
- Bots: `Source/Simulturn.Bots` (library: bot contract, catalog, one match)
- Lab: `Source/Simulturn.Lab` (console app for studies that are not tests; the first one is the arena)
- Test projects: `Test/Simulturn.Core.Test` (engine, including the random games against the invariants), `Test/Simulturn.Bots.Test` (match), `Test/Simulturn.Lab.Test` (arena), `Test/Simulturn.Test.Common` (shared fixtures)
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

### Run the lab
The first argument is the study, `--profile` picks a profile of `Source/Simulturn.Lab/appsettings.json` (default `quick`), any other `--Key value` replaces a value of the profile. The result goes to `measurements/` (not in git).
```bash
dotnet run --project Source/Simulturn.Lab -c Release -- arena
dotnet run --project Source/Simulturn.Lab -c Release -- arena --profile full --MapSeeds 500
```

## Current Build Health
At generation time, `dotnet build Simulturn.sln` and
`dotnet test Simulturn.sln` passed.

## Project Structure Expectations
- Core game model types live in `Source/Simulturn.Core/Model/`
- `Source/Simulturn.Core/Model/` holds data only: `GameState` and `PlayerState` have no rule logic
- Everything that applies rules lives in `Source/Simulturn.Core/Engine/` (namespace `Simulturn.Core.Engine`): `TurnResolution.Resolve` (one turn, one named method per phase), `Combat.Fight` (the fight rule, armies and upgrade levels only), `Movement` (the movement range), `Costs` (the price of orders), `Harvest` (the income), `Occupancy` (what a pending order keeps busy), `Schedule` (when an order completes), `Orders` (what a player can still order), `Destruction` (destroyed buildings), `Standing` (defeat and the outcome of the game), `Game` (start a game, play a turn), `CommandValidator.Validate`, `StateValidator.Validate`, `FogOfWar` (player view, visibility)
- A rule a client needs is a static function in `Engine` that gets the settings and the values it needs, nothing else. The turn resolution, the command validation and a client (a UI, a bot) call the same function; none of them computes the rule itself. A client that only holds its view can call every one of them
- `Combat.Fight(settings, combatants)` is the only way to compute a fight. The turn resolution passes the armies of a hexagon with the true upgrade levels, a client that forecasts a fight passes what its player knows; both get a `FightReport`. Everything an upgrade does to a fight is applied inside `Combat`
- `Costs` holds the price of orders as an `OrderCost(Matter, Space)`: `Training`, `Construction` and `Research` for one order, `Costs.Of` for a command or for all commands of a turn. `CommandValidator` checks it, `TurnResolution` pays it, a client budgets with it. An upgrade that changes a price is applied there
- `Harvest.Of` holds the income rule for one hexagon (a plane is needed, a started construction takes a dot off the harvest, `Harvest.Capacity` limits the harvesters, the hexagon never yields more than it has left). `Harvest.Forecast(view, commands)` gives a client the income of the turn with the orders entered so far
- `Occupancy` holds the occupancy rules, what a pending order keeps busy until it completes: `FreeTrainingSlots` (every unit in training needs its own building), `FreeDots` (every construction needs its own dot, which neither builds a second time nor leaves), `HoldsResearch` (a research needs a dot on its hexagon) and `ProvidedSpace`. A function gets the values of one hexagon and says what is free; a negative value means the rule is broken. `StateValidator` asks with the state, `CommandValidator` with the state plus the commands. No validator writes out a comparison that is a rule: it asks the function of the rule
- `Orders.Possible(view, commands)` tells a client what it can still order after the commands entered so far: the budget left (`Costs.Budget`) and per hexagon a `PossibleHexagonOrders`, which has the shape of a `Command` with limits in place of amounts (`Training`, `Construction`, the upgrades of `Research`, the `Movable` army; the destinations come from `Movement.Destinations`). Every limit stands for its order alone: any single order up to its limit keeps the commands valid, two together may not. So a client takes one order, adds it to its `CommandsBuilder` and asks again. `Orders` writes out no rule: the limits come from `Occupancy` and `Costs`, a research is put to the validator (`CommandValidator.AcceptsResearch`). A new command rule needs its limit there, and `OrderInvariants` in the random games fails when what is offered and what is valid differ
- `Schedule` gives the turn in which an order completes (`TrainingCompletion`, `ConstructionCompletion`, `ResearchCompletion`); the turn an order is given in is the first turn of its duration
- A `Command` is everything a player orders on one hexagon in one turn, an order is a single entry of it (a training, a construction, a research, a `MovementOrder`)
- Commands are mutable only while a client enters them: a `CommandsBuilder` (`Model/Commands`) collects the orders of a turn (`Train`, `Construct`, `Research`, `Move`), `Commands` is its read-only view to ask the engine with, `ToImmutable` is what is handed in. The builder checks no rule. Every engine function takes the commands as `IReadOnlyDictionary<Hexagon, Command>`, which fits the view of a builder and the commands handed in alike; none changes them
- `Destruction.Of(settings, army, compound)` gives the buildings an army destroys; the turn resolution calls it for the armies left after the fights
- `Standing.IsDefeated` and `Standing.Of(state)` hold the rule of defeat: a player without a building and without a pending construction is defeated, the game runs while at least two players are not. The defeat is final and never stored: a defeated player takes no more orders (`PlayerDefeated`), so nothing can be built again. The pending researches end, the armies stay where they are and fight when attacked. `GameState` has no `IsGameOver`; the standing is part of every `PlayerGameState`
- `Game.Start(settings)` and `Game.PlayTurn(state, commands)` are the steps of running a game: `PlayTurn` validates the commands of all players and resolves the turn, or returns the broken rules per player (`TurnAttempt`). The console harness uses them. Nothing is kept there: a state follows from the settings and the commands of all turns before it
- A neutral army (`HexagonSettings.NeutralArmy`, `GameState.NeutralArmies`) guards its hexagon: it never moves, has no upgrades, destroys no buildings and is no player. Whoever enters the hexagon fights it through `Combat.Fight`; wherever armies are keyed by player id (combatants, `FightReport`, `OpponentUnitCounts`) it has the reserved id `Neutral.Id`. The start armies are in the settings and open to all players, during the game a player sees the unit count like the units of an opponent. No start hexagon holds a neutral army
- A setting of `GameSettings` or `HexagonSettings` is never accepted and then ignored: it has an effect in `Engine` or it does not exist
- `Movement` holds the movement rule: `GameSettings.MovementRange` is the range per unit type, the distance is the hexagon distance and nothing in between blocks. `Movement.ReachableArmy(settings, army, distance)` is the only place that compares a range with a distance; `CommandValidator` and `Movement.Destinations` (what a client offers as destinations) both call it. The effective range comes from `Movement.Range`, an upgrade that changes it is applied there
- An upgrade belongs to a player (`PlayerState.UpgradeLevels`), not to a unit. `GameSettings.Upgrades` holds one `UpgradeDefinition` per `Upgrade` (`Source/Simulturn.Core/Model/Upgrades/`): `AffectedUnits` (the unit types it acts on) and `Levels`, each an `UpgradeLevel(Cost, Duration, Effect)`. The effect of a level is absolute, nothing adds up over the levels. `UpgradeEffect` is the union of effect kinds; a kind is applied at the one place of its rule (`FightExponentBonus` in `Combat.GetExponent`, `StartMatterBonus` in `UpgradeRules.StartMatter`); the place asks `UpgradeRules.Effects` for the effects of a player and picks its kind. A unit effect reads `AffectedUnits`. A simple new upgrade is an enum value and an entry in `DefaultSettings`, a new kind of effect a union case and its line at the rule
- Dots have no fight exponent and get no exponent upgrade: `Army.GetStrengthOver` counts them one by one
- A player starts with the levels of `GameSettings.StartUpgrades` (`UpgradeRules.StartLevels`). The settings are part of every player view, so start upgrades are open to all players; what is researched during the game is not. An effect on the start state (`StartMatterBonus`) counts only as a start upgrade, researched later it changes nothing
- `GameSettings.NextUpgradeLevel(upgrade, completedLevels)` is the only place that reads the level a research buys (cost, duration)
- A player learns the upgrades of an opponent only where the composition of an army is recognized. `UpgradeRules.ActingOn(settings, upgradeLevels, army)` gives the upgrades that act on an army; `Combat.Fight` writes them into `FightReport.UpgradeLevels`, so a fight shows them and can be computed again from the report alone. The sight shows only the number of units and therefore no upgrades; whatever shows a composition later calls `ActingOn` too
- `TurnResolution.Resolve` returns a `TurnResult(State, Report)`. The state is the input of the next turn; the `TurnReport` (`Source/Simulturn.Core/Model/Report/`) holds what happened, each fact once and without fog of war, and never goes back into the engine. `PlayerState` holds no per-turn data
- The `TurnReport` has four parts: `Activities` (per player, what concerns only that player: income, completed and canceled orders, movements, matter), `Fights` (per hexagon), `Destructions` and `Defeats`. `TurnResolution` writes an event where it happens and never decides who may see it. A new event also needs a line in `StateInvariants.CheckTransition`
- The cut of a player (`PlayerTurnReport`) is the own activity, the complete fights the player took part in, the destructions as attacker or owner and the defeats of all players
- A player only gets the cut made by `FogOfWar`: `GetPlayerGameState(turnResult, playerId)` cuts state and report, the `PlayerTurnReport` sits in `PlayerGameState.Report`. `GetPlayerGameState(state, playerId)` is for a state without a resolved turn (start of the game) and yields an empty report
- The game state holds nothing about what a player knows. The visibility follows from the armies (`FogOfWar.GetVisibility`) and is never stored. A view (`PlayerGameState.Observations`) shows one turn only. What a player last saw is a `HexagonMemory` per hexagon, each part with the turn it was seen; a sighting of the current turn is what the player sees now. `FogOfWar.UpdateMemories(memories, view)` carries the memories into the next turn, `FogOfWar.GetMemories(views)` applies the same rule to all views a client has kept. The engine keeps neither views nor memories; a client does
- Pending trainings and constructions are flat: `ImmutableDictionary<TurnHexagon, Army>` and `<TurnHexagon, Compound>`, keyed by completion turn and hexagon. Write them with `Merge(key, value)`, which drops an entry once it is empty; read a hexagon with `On(hexagon)`. A dictionary has no order: sort explicitly wherever the order matters (cancellation, output)
- `Engine` depends on `Model`; the only edge back is the `GameState` constructor building the start state
- Standard rules and maps live in `Source/Simulturn.Core/Setup/`: `DefaultSettings` (the only copy of the rules), `Maps` (fixed maps), `MapGenerator` (seeded, symmetric maps)
- Mutable transition helpers use builders in `PlayerStateBuilder` (internal working state of `TurnResolution`)
- Cross-cutting helpers are in `Source/Simulturn.Core/Extensions/`
- Global aliases/usings are in `Source/Simulturn.Core/GlobalUsings.cs`
- A bot is a client: `IBot.GetCommands(PlayerGameState)` in `Source/Simulturn.Bots` gets the view of its player and nothing else, and asks every rule from the engine. One instance plays one game on one seat and holds its memories itself
- `Match.Play(settings, bots, limits)` is the only place that plays a game between bots. A `MatchResult` is built with `MatchResult.WonBy` or `MatchResult.Drawn`, so only a victory has a winner. It ends with `Victory`, `MutualDefeat`, `Stall` (nothing happened for `MatchLimits.StallTurns` turns, `Match.IsStill`) or `TurnLimit`; invalid commands of a bot are a bug and throw `InvalidBotCommandsException`. The engine knows no turn limit
- The engine and the maps do not roll dice. A match follows from the settings and the bots; the spread of a run comes from the map seeds and from bots that roll dice with the seed they are created with
- `BotCatalog.All` lists the bots by name. A new bot is a class in `Source/Simulturn.Bots/Catalog/` and a line there; nothing in the lab changes
- `RandomBot` is the measure for bots with a plan: it rolls dice among the orders that are not plainly pointless. It spends everything and keeps back only the price of a plane while a hexagon is left to harvest, trains the dots a hexagon pays for, sends one spare dot at a time to claim a hexagon, keeps its training buildings and so its army together, and leaves with the army only for a hexagon it can take. A variant is another `RandomBotOptions` and its own line in the catalog. It asks every limit and price from the engine; its heuristics are no rules and stay in the bot
- A bot that rolls dice sorts what it rolls among (a dictionary has no order), so that a seed always gives the same play
- The arena gives the bots of a game their seeds through `ArenaStudy.BotSeed`, never as numbers a fixed distance apart: `System.Random` gives such seeds first rolls that follow from each other
- `Source/Simulturn.Lab` holds what only serves to examine the game, one folder per study. `Source/Simulturn.Bots` holds what a UI needs too. `ArenaStudy.Run(options, bots, maps)` plays every pairing on every map type with both seat orders and gives an `ArenaResult` with one `ArenaRow` per pairing, map type and seat; the options name bots and maps, the lists passed in provide them

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
- An `Army` or a `Compound` computed per type is built with `Army.PerUnit` or `Compound.PerBuilding`, not with a loop over the enum
- Keep domain objects strongly typed; avoid weakly typed dictionaries/arrays
- Preserve immutable collection boundaries for stored state
- Use `.ToBuilder()` only inside mutation-heavy logic, then convert back to immutable
- Data that is built once and read every turn (`GameSettings` maps) uses `FrozenDictionary`; state that changes per turn uses `ImmutableDictionary`
- Accept `IReadOnlyDictionary`/`IReadOnlyList` as parameters, never store them as properties
- Static enum mappings are switch expressions (`GameSettings.TrainingBuilding`), not dictionaries
- Closed sets of result types are `union` types (C# 15): `CommandValidation` and `StateValidation` in `Model/State/Validation/`, `UpgradeEffect` in `Model/Upgrades/`, `GameOutcome` in `Model/State/`, `TurnAttempt` in `Model/Report/`. A `switch` over them is exhaustive, the cases are plain sealed records without a shared interface. In tests, match the case through `.Value` (`validation.Value.ShouldBeOfType<...>()`), the union itself is a struct

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
