---
id: split-game-state
title: GameState vereint Zustand, Zugauflösung und Validierung in einer Datei
status: done
created: 2026-11-10
updated: 2026-11-11
completed: 2026-11-11
---
## Problem

`Source/Simulturn.Core/Model/State/GameState.cs` hat 757 Zeilen und drei Rollen:

- **Zustand**: das Record mit seinen Properties, der Konstruktor aus den Settings, `FromParts`.
- **Zugauflösung**: `NextTurn` mit rund 290 Zeilen in einer Methode, dazu `Fight`, `GetNewTrainings` und `GetNewConstructions`.
- **Validierung**: `Validate` für Commands und `IsValid` für den Zustand, zusammen rund 300 Zeilen.

Die Phasen der Auflösung existieren nur als Kommentarüberschriften in `NextTurn`: `// Income`, `// Trainigs`, `// Constructions`, `// Researches`, `// Movements`, `// Fights`, `// Destroy Buildings`. Die Phasen teilen sich Lokalvariablen der einen Methode (`playerStates`, `remainingMatter`, `potentialFightHexagons`), deshalb lassen sie sich nicht ohne Weiteres in Methoden ziehen.

Folgen:

- Wer eine Regel sucht, liest die ganze Datei. Die Kürzung von Baustellen nach Dot-Verlusten steht zum Beispiel in der Kampfphase, vier Ebenen tief hinter den Verlusten. Der Abbruch von Trainings nach Gebäudeverlust steht in der Zerstörungsphase, fünf Ebenen tief. Beide Stellen waren Fehlerquellen ([[pending-only-queues]], [[training-cancellation-after-completion]]).
- Die Kampfrechnung `Fight` ist privat und von außen nicht erreichbar; [[combat-forecast-core]] braucht sie öffentlich, ohne Kopie.
- Jede Änderung an der Auflösung ([[turn-result]], [[turn-report]], [[flat-pending-queues]]) streut Zeilen über die eine Methode statt eine Phase zu ändern. Ein Report, den jede Phase selbst schreibt, hat in dieser Form keinen natürlichen Ort.
- `Validate` und `IsValid` liefern die Unions aus `Model/State/Validation/`, liegen aber nicht dort.

## Ideen

Unverbindlich, der Implementierer entscheidet:

- Drei Typen für drei Rollen: `GameState` behält die Properties und dünne öffentliche Einstiege; die Auflösung und die beiden Validierungen bekommen eigene interne Typen. Die Validierungen gehören neben die Unions in `Validation`.
- Die Auflösung als Instanz pro Zug, die den Arbeitszustand (Builder je Spieler, Matter, Kampfhexagons, später der Report) als Felder hält. Dann kann jede Phase eine Methode mit Namen sein, ohne dass fünf Parameter durchgereicht werden. Die Stornierungen (Baustellen nach Dot-Verlust, Trainings nach Gebäudeverlust) werden eigene Methoden statt Teil der Kampf- und Zerstörungsphase.
- Die Kampfrechnung als reine Funktion zweier Armeen und ihrer Exponenten, getrennt von der Frage, wer auf dem Hexagon steht. Das ist der Teil, den [[combat-forecast-core]] veröffentlichen will.
- Keine Verhaltensänderung. Reihenfolge der Phasen, Rundungen, Stornierungsreihenfolge bleiben; die bestehende Suite ist das Netz.

## Entscheidung

Entschieden am 2026-11-11, abweichend von der ersten Idee: Die öffentliche API darf sich ändern. `GameState` hält nur Daten, alles, was Regeln anwendet, liegt in `Source/Simulturn.Core/Engine/` (Namespace `Simulturn.Core.Engine`). `Model/` bleibt der Ort für Daten, die Unions in `Model/State/Validation/` sind Ergebnisdaten und bleiben dort.

| Vorher | Nachher |
|---|---|
| `state.NextTurn(commands)` | `TurnResolution.Resolve(state, commands)` |
| `state.Validate(playerId, commands)` | `CommandValidator.Validate(state, playerId, commands)` |
| `state.IsValid()` | `StateValidator.Validate(state)` |
| `state.GetPlayerGameState(playerId)` | `FogOfWar.GetPlayerGameState(state, playerId)` |
| `GameState.Fight` (privat) | `Combat.Fight(army1, exponent1, army2, exponent2)` |
| `PlayerStateBuilder.GetVisibility` | `FogOfWar.GetVisibility` |

- `TurnResolution` und `CommandValidator` haben einen statischen Einstieg und bauen intern eine Instanz pro Aufruf, die den Kontext als Felder hält. `StateValidator`, `Combat` und `FogOfWar` sind statisch.
- Die Phasen Einkommen bis Bewegung laufen weiter pro Spieler am Stück (`ResolveOrders`), danach Kämpfe, Gebäudezerstörung und Forschungsabbruch über alle Spieler.
- `PlayerStateBuilder` ist Arbeitszustand der Auflösung und jetzt `internal`.
- Kein Extension-Block, der `state.NextTurn(...)` als Syntax erhält, und keine Fassade: Beides würde die Trennung verschleiern.

Offen geblieben: Der Konstruktor `GameState(GameSettings)` baut den Startzustand und ruft dafür `FogOfWar` auf. Das ist die einzige Abhängigkeit von `Model` auf `Engine`. Eine Factory in der Engine würde sie auflösen, trifft aber 64 Aufrufstellen und ist ein eigener Schritt.

## Reihenfolge

Vor [[turn-result]] und [[flat-pending-queues]]: Die Aufteilung ist verhaltensneutral und macht die beiden Umbauten zu kleineren Diffs auf kleineren Methoden. Andersherum verschiebt man Code, der gerade erst geändert wurde.

## Akzeptanzkriterien

- `GameState` enthält keine Regel-Logik mehr: nur Properties, `IsGameOver`, den Konstruktor aus den Settings und `FromParts`.
- Auflösung, Kampfrechnung, beide Validierungen und die Spielersicht liegen in `Engine/`.
- Jede Phase der Auflösung ist eine Methode mit Namen; in der Auflösung stehen keine Kommentarüberschriften mehr.
- Die Kampfrechnung ist eine eigene Funktion, die nur Armeen und Exponenten kennt.
- `GameStateTest`, `PendingQueueTest`, `ResearchResolutionTest`, `ResearchValidationTest`, `ValidatePendingWorkTest`, `EngineInvariantsTest` und die Arena laufen ohne Änderung an den Erwartungen.
