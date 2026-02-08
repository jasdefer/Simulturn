# AGENTS Guide for Simulturn
This file is for coding agents working in `C:\Users\justu\source\repos\Simulturn`.
Use it as the default workflow and style baseline for this repository.

## Repository Snapshot
- Solution: `Simulturn.sln`
- Runtime: .NET 10 (`net10.0`)
- Core project: `Source/Simulturn.Core/Simulturn.Core.csproj`
- Test project: `Test/Simulturn.Core.Test/Simulturn.Core.Test.csproj`
- Console project: `Test/Simulturn.Core.Console/Simulturn.Core.Console.csproj`
- Test stack: TUnit + Shouldly
- Project settings: `Nullable=enable`, `ImplicitUsings=enable`

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
Preferred: filter by fully qualified name.
```bash
dotnet test Test/Simulturn.Core.Test/Simulturn.Core.Test.csproj --filter "FullyQualifiedName~Simulturn.Core.Test.State.GameStateTest.Initialize"
```

Alternative: filter by short name.
```bash
dotnet test Test/Simulturn.Core.Test/Simulturn.Core.Test.csproj --filter "Name~Initialize"
```

### List discovered tests
```bash
dotnet test Test/Simulturn.Core.Test/Simulturn.Core.Test.csproj --list-tests
```

### Lint / formatting
There is no dedicated linter config in repo (no `.editorconfig` or ruleset files).
Use `dotnet format` as the formatting/lint proxy:
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
- Game state transition logic lives in `Source/Simulturn.Core/Model/State/GameState.cs`
- Mutable transition helpers use builders in `PlayerStateBuilder`
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
- Reuse global aliases/usings (`Constructions`, `Trainings`) where applicable
- Prefer normal `using` imports over fully-qualified names in method bodies
- Add to `GlobalUsings.cs` only when a using is truly cross-cutting

### Types and immutability
- Use `record` for immutable state aggregates (`GameState`, `PlayerState` patterns)
- Use `readonly struct` for value objects (`Army`, `Hexagon` patterns)
- Keep domain objects strongly typed; avoid weakly typed dictionaries/arrays
- Preserve immutable collection boundaries for stored state
- Use `.ToBuilder()` only inside mutation-heavy logic, then convert back to immutable

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
- Keep command validation (`Validate`) separate from state validity checks (`IsValid`)
- Return domain validation objects for business-rule failures
- Throw exceptions only for invariant violations or impossible states

### Error handling
- Use specific exceptions (`InvalidOperationException`, `ArgumentOutOfRangeException`)
- Include clear messages for thrown exceptions
- Do not silently swallow errors in core game logic
- Keep failures deterministic and reproducible where possible

### Collections and LINQ
- Prefer existing helper extensions (`Merge`, `MergeOrOverwrite`, `Sum`)
- Keep LINQ readable; split long chains if intent is hard to parse
- Avoid repeated expensive enumerations in hot paths
- Favor clarity over terse one-liners in state transition code

### Testing conventions
- Use TUnit attributes (`[Test]`, hooks) and Shouldly assertions
- Keep Arrange/Act/Assert structure, especially in larger scenarios
- Reuse helper setup methods for repeated game state construction
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
