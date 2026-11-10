---
id: dotnet-upgrade-and-union-types
title: Auf die nächste .NET-Version wechseln und neue Sprachfeatures nutzen
status: done
created: 2026-11-09
updated: 2026-11-10
completed: 2026-11-10
---
## Ziel

Alle sieben Projekte laufen auf der nächsten .NET-Version (.NET 11, C# 15) mit passendem SDK-Pin in `global.json`. Danach werden neue Sprachfeatures dort eingesetzt, wo sie das Modell klarer machen, allen voran Union Types.

## Ausgangslage

- `global.json` pinnte SDK 10.0.102 mit `rollForward: latestFeature`, alle Projekte zielten auf `net10.0`.
- Das Modell hat mehrere geschlossene Typmengen, die heute über Interfaces mit vielen Record-Implementierungen abgebildet sind: `IStateValidation` und `ICommandValidation` (je ein Record pro Regel), `IUpgradeLevel` (`DotUpgrade`, `UpgradeStartMatter`). Konsumenten unterscheiden sie per `switch` auf den Typ oder per `ShouldBeOfType` in Tests. Nichts erzwingt Vollständigkeit beim Verzweigen.
- Das Persistenz-Item hat festgehalten, dass `IUpgradeLevel` polymorph ohne Diskriminator ist und `System.Text.Json` daran scheitert ([[game-persistence]]). Ein Union Type ist eine mögliche Antwort darauf.

## Kandidaten für Union Types

- `IStateValidation` / `ICommandValidation`: geschlossene Menge, die Engine kennt alle Fälle. Ein `switch` in UI oder AI kann dann vollständig sein.
- `IUpgradeLevel`: zwei Fälle, die Settings-Tabelle `GameSettings.Upgrades` enthält sie gemischt.
- Prüfen, ob weitere geschlossene Mengen existieren (`GameEndReason`, Ergebnistypen der Arena).

Weitere Features nur, wo sie heutigen Code vereinfachen. Kein Umbau um des Features willen.

## Akzeptanzkriterien

- `dotnet build`, `dotnet test Simulturn.sln` und `dotnet format --verify-no-changes` laufen mit dem neuen SDK ohne Warnungen.
- `AGENTS.md` und `docs/test-strategy.md` nennen die neue Version und, falls sich Befehle ändern, die neuen Befehle.
- Jede Umstellung auf einen Union Type ersetzt ein Interface mit geschlossener Implementierungsmenge und entfernt mindestens eine Typprüfung zur Laufzeit oder macht ein `switch` vollständig. Öffentliche Namen bleiben, soweit die Syntax es erlaubt.
- Die AI-Spieler, die Arena und die Tests laufen unverändert durch.

## Ergebnis

- Alle Projekte zielen auf `net11.0`, `global.json` pinnt SDK 11.0.100 mit `rollForward: latestFeature`. Build, Tests und `dotnet format --verify-no-changes` laufen ohne Warnungen.
- `ICommandValidation`, `IStateValidation` und das interne `IPlayerStateValidation` sind durch die Union Types `CommandValidation` und `StateValidation` ersetzt (Namespace `Simulturn.Core.Model.State.Validation`, vorher `StateValidation`, weil der Namespace sonst mit dem Typ kollidiert). Die Regel-Records sind jetzt versiegelte Records ohne Interface; Fälle, die beide Unions teilen (`InvalidPlayerId`, `MissingBuildingForTraining`, `MissingDotsForConstruction`, `UpgradeNotResearchableOnHexagon`, `ResearchRequiresDot`), stehen in beiden Fall-Listen. Ein `switch` darüber ist vollständig, die Unions geben `ToString` an den Fall weiter, damit Fehlermeldungen (`InvalidCommandsException`, Konsole, `StateInvariants`) unverändert bleiben.
- Tests prüfen den Fall über `validation.Value.ShouldBeOfType<...>()`, weil die Union ein Struct ist und `ShouldBeOfType` auf dem Struct selbst fehlschlägt. `is`-Muster auf Unions sind in Expression Trees nicht erlaubt, daher `x => x.Value is ...` in `ShouldContain`/`ShouldAllBe`.
- `closed` gibt es in C# 15 nur für Klassen und Records, nicht für Interfaces. Für die Validierungen passt das nicht, weil mehrere Records in beiden Mengen liegen; die Union erlaubt das.

## Entschieden

- Der Wechsel geschah am Release-Tag (10.11.2026) mit SDK 11.0.100.
- `IUpgradeLevel` bleibt ein Interface. Eine Union würde für `Cost`, `Duration` und `Upgrade` je ein zweifaches `switch` brauchen und die Typprüfung in `PlayerState` nur durch ein Muster ersetzen, heutigen Code also nicht vereinfachen. Der Diskriminator kommt mit [[game-persistence]].
