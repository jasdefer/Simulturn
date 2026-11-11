---
id: turn-result
title: NextTurn liefert Zustand und Ereignisse getrennt
status: done
created: 2026-11-10
updated: 2026-11-11
completed: 2026-11-11
---
## Problem

`GameState.NextTurn` gibt nur den neuen Zustand zurück. Zwei Ereignisse der Auflösung werden trotzdem aufgezeichnet, aber als Felder des Zustands: `PlayerState.Losses` (eigene Verluste pro Hexagon) und `PlayerState.RevealedArmies` (gegnerische Armeen, gegen die gekämpft wurde). Beide befüllt die Kampfphase, und `PlayerStateBuilder.FromPlayerState` setzt beide zu Beginn des nächsten Zugs auf leer. Kein Teil von `NextTurn` liest sie; gelesen werden sie nur von der KI über die Spielersicht (`CommanderPlayer.UpdateEnemyEstimate`), von Tests und vom `GameStateSnapshot`.

Damit trägt `PlayerState` Daten, die nur für genau einen Zug gelten:

- Wer ein `PlayerState` baut (`GameState.FromParts`, `BelievedState`), füllt Felder, die kein Zustand sind.
- Der Builder muss sie zurücksetzen. Ein drittes Ereignisfeld, das beim Zurücksetzen vergessen wird, wandert in den nächsten Zug.
- Für Replay und Anzeige braucht man die Ereignisse aller Züge; im Zustand steht immer nur der letzte.
- Alles, was sonst im Zug passiert (Einkommen, fertige Aufträge, Bewegungen, zerstörte Gebäude, Stornierungen), wird gar nicht aufgezeichnet. Die alte Web-UI hat das aus einem Vergleich zweier Zustände nachgerechnet, mit den in [[turn-report]] genannten Lücken.

## Entscheidung

Die Engine ist eine Faltung: Zustand plus Orders ergibt den nächsten Zustand. Die Ereignisse einer Auflösung sind die zweite Ausgabe dieser Faltung, nicht Teil des Akkumulators. `NextTurn` liefert beides getrennt, etwa als `TurnResult(GameState State, TurnReport Report)`.

Der Report wird von `NextTurn` selbst geschrieben, an der Stelle, an der das Ereignis passiert. Er wird nicht aus einem Diff zweier Zustände errechnet: Ein Diff sieht nur das Netto (Training mit Dauer 1, verlustfreier Sieg, Armee zieht und stirbt im selben Zug, Baustelle gekürzt und neu eröffnet), und jede Lücke müsste die Auflösung rückwärts nachbauen. Das ist dasselbe Driftproblem wie in [[combat-forecast-core]].

Der Report ist deterministisch wie die Engine und wird nie gespeichert; wer die Orders aller Züge hält ([[game-persistence]]), rechnet ihn beim Falten mit. Ein Diff kann später als Prüfung dienen (alles, was der Diff sieht, muss der Report erklären), nicht als Quelle.

Dieses Item ändert nur die Struktur und verschiebt die beiden Ereignisse, die es schon gibt. Alle weiteren Ereignisse füllt [[turn-report]].

## Umsetzung

Entschieden am 2026-11-11:

- `TurnResolution.Resolve` liefert `TurnResult(GameState State, TurnReport Report)`, ein Record und kein Tuple.
- Der `TurnReport` ist allwissend und hält jede Tatsache einmal: `Fights` pro Hexagon mit den Armeen, wie sie in den Kampf gingen, und den Verlusten je Spieler (`FightReport`). Ein Hexagon hat höchstens einen Kampf pro Zug. Die Typen liegen in `Model/Report/`.
- Den Ausschnitt eines Spielers schneidet `FogOfWar`, wie beim Zustand: `GetPlayerTurnReport(report, playerId)` liefert den `PlayerTurnReport` mit `Losses` und `RevealedArmies` in den bisherigen Formen. Die Engine entscheidet beim Schreiben nicht, wer ein Ereignis sehen darf.
- `FogOfWar.GetPlayerGameState(turnResult, playerId)` schneidet Zustand und Report; `PlayerGameState.Report` trägt den Ausschnitt. `GetPlayerGameState(state, playerId)` bleibt für einen Zustand ohne aufgelösten Zug und liefert einen leeren Report.
- Server, lokales Spiel und Replay halten das `TurnResult`; ein Client bekommt nur das `PlayerGameState`.
- `GameRunner.Run` gibt dem `onTurnCompleted`-Callback das `TurnResult`. Die Rollouts im `SimulatorPlayer` reichen den Report an die simulierten Spieler weiter.

## Akzeptanzkriterien

- `NextTurn` liefert Zustand und Report getrennt. `PlayerState` hat kein Feld mehr, das pro Zug zurückgesetzt wird; `PlayerStateBuilder.FromPlayerState` setzt nichts mehr auf leer.
- `Losses` und `RevealedArmies` liegen im Report, pro Spieler getrennt. Fog of War bleibt gewahrt: Der Ausschnitt eines Spielers enthält nur, was er wissen darf.
- `PlayerGameState` trägt den Ausschnitt des Reports des letzten Zugs. `CommanderPlayer` liest die aufgedeckten Armeen von dort, nicht mehr aus `PlayerState`.
- `Memories` bleibt im Zustand: Was ein Spieler zuletzt gesehen hat, ist Eingabe der nächsten Sicht, kein Ereignis.
- `GameStateSnapshot` rendert den Zustand ohne die beiden Felder; die Tests, die Verluste und aufgedeckte Armeen prüfen (`GameStateTest`), lesen sie aus dem Report. Ihre Erwartungen ändern sich nicht.
- Die AI-Spieler, `EngineInvariantsTest` und die Arena laufen unverändert.
