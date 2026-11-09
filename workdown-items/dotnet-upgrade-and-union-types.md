---
id: dotnet-upgrade-and-union-types
title: Auf die nächste .NET-Version wechseln und neue Sprachfeatures nutzen
status: todo
created: 2026-11-09
---
## Ziel

Alle sieben Projekte laufen auf der nächsten .NET-Version (.NET 11, C# 15) mit passendem SDK-Pin in `global.json`. Danach werden neue Sprachfeatures dort eingesetzt, wo sie das Modell klarer machen, allen voran Union Types.

## Ausgangslage

- `global.json` pinnt SDK 10.0.102 mit `rollForward: latestFeature`, alle Projekte zielen auf `net10.0`.
- Das Modell hat mehrere geschlossene Typmengen, die heute über Interfaces mit vielen Record-Implementierungen abgebildet sind: `IStateValidation` und `ICommandValidation` (je ein Record pro Regel), `IUpgrade` (`DotUpgrade`, `UpgradeStartMatter`). Konsumenten unterscheiden sie per `switch` auf den Typ oder per `ShouldBeOfType` in Tests. Nichts erzwingt Vollständigkeit beim Verzweigen.
- Das Persistenz-Item hat festgehalten, dass `IUpgrade` polymorph ohne Diskriminator ist und `System.Text.Json` daran scheitert ([[game-persistence]]). Ein Union Type ist eine mögliche Antwort darauf.

## Kandidaten für Union Types

- `IStateValidation` / `ICommandValidation`: geschlossene Menge, die Engine kennt alle Fälle. Ein `switch` in UI oder AI kann dann vollständig sein.
- `IUpgrade`: zwei Fälle, die Settings-Tabelle `GameSettings.Upgrades` enthält sie gemischt.
- Prüfen, ob weitere geschlossene Mengen existieren (`GameEndReason`, Ergebnistypen der Arena).

Weitere Features nur, wo sie heutigen Code vereinfachen. Kein Umbau um des Features willen.

## Akzeptanzkriterien

- `dotnet build`, `dotnet test Simulturn.sln` und `dotnet format --verify-no-changes` laufen mit dem neuen SDK ohne Warnungen.
- `AGENTS.md` und `docs/test-strategy.md` nennen die neue Version und, falls sich Befehle ändern, die neuen Befehle.
- Jede Umstellung auf einen Union Type ersetzt ein Interface mit geschlossener Implementierungsmenge und entfernt mindestens eine Typprüfung zur Laufzeit oder macht ein `switch` vollständig. Öffentliche Namen bleiben, soweit die Syntax es erlaubt.
- Die AI-Spieler, die Arena und die Tests laufen unverändert durch.

## Zu entscheiden

- Ob der Wechsel sofort beim Release geschieht oder erst mit dem ersten Patch des SDK.
- Ob `IUpgrade` ein Union Type wird oder im Zuge von [[game-persistence]] einen Diskriminator bekommt. Beides zusammen ist nicht nötig.
