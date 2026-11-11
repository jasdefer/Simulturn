---
id: player-knowledge-in-state
title: Wissen der Spieler liegt im Regelzustand
status: todo
created: 2026-11-11
updated: 2026-11-11
---
## Problem

`PlayerState` trägt zwei Felder, die keine Spielregel liest: `Visibilities` und `Memories`. Die Auflösung eines Zugs (Einkommen, Aufträge, Bewegung, Kampf, Zerstörung) braucht keines von beiden. Gelesen werden sie nur von `FogOfWar`, um die Sicht eines Spielers zu bauen, und von Tests.

Mit [[turn-result]] wurden `Losses` und `RevealedArmies` aus dem Zustand genommen, mit der Begründung, dass der Zustand das ist, was die Engine für den nächsten Zug braucht. Nach demselben Maßstab sind auch diese beiden Felder kein Regelzustand. Sie unterscheiden sich untereinander:

### `Visibilities` ist abgeleitet

Die Sicht ergibt sich vollständig aus den eigenen Armeen und zwei Werten der `GameSettings` (`VisibilityRange`, `PartialVisibilityRange`). Sie wird beim Bauen eines `PlayerState` berechnet und mitgespeichert, an drei Stellen: `PlayerStateBuilder.ToPlayerState`, im Konstruktor von `GameState` und in `BelievedState`.

- Das Feld ist ein Pflichtfeld. Wer einen `PlayerState` baut, kann eine Sicht eintragen, die nicht zu den Armeen passt; nichts erzwingt die Übereinstimmung.
- Jede Stelle, die einen `PlayerState` erzeugt, muss an die Berechnung denken.

### `Memories` ist angesammelt

Die Erinnerung hält pro Hexagon, was der Spieler dort zuletzt gesehen hat (Restmaterie, gegnerische Gebäude, Zug der Sichtung). Sie lässt sich nicht aus dem aktuellen Zustand ableiten, sondern nur aus dem Verlauf. Deshalb wird sie im Zustand mitgeführt und jeden Zug fortgeschrieben.

- Das Fortschreiben ist ein zweiter Durchlauf nach der Auflösung: `FogOfWar.UpdateMemories` läuft über alle fertigen `PlayerState` und ersetzt in jedem nur `Memories`. Es kann nicht beim Bauen des einzelnen `PlayerState` geschehen, weil die Erinnerung eines Spielers die Gebäude der anderen enthält.
- Jede Auflösung bezahlt diesen Durchlauf, auch dort, wo die Erinnerung niemand liest, etwa in den Rollouts des `SimulatorPlayer`. Wie viel das ausmacht, ist nicht gemessen.
- Wer einen Zustand von Hand baut (`BelievedState`, Tests, später ein geladener Spielstand), muss entscheiden, was er dort einträgt.
- Die Spielersicht liest gegnerische Gebäude und Restmaterie ausschließlich aus `Memories`, auch für Hexagons, die der Spieler gerade sieht. Das stimmt nur, weil `UpdateMemories` im selben Zug vorher gelaufen ist. Gegnerische Einheiten liest die Sicht dagegen direkt aus dem Zustand. Die beiden Wege sind verschieden, ohne dass man es der Sicht ansieht.

### Wer davon abhängt

`PlayerGameState.Observations` zeigt heute pro Hexagon die Erinnerung mit (`LastSeenTurn`, `RemainingMatter`, `OpponentCompounds`). `CommanderPlayer`, `StateMachinePlayer`, `RandomPlayer` und `BelievedState` lesen diese Werte. Die Fog-of-War-Invarianten (`StateInvariants`) und `PlayerGameStateTest` prüfen sie.

## Ideen

Keine davon ist entschieden.

- Der Zustand enthält nichts über Wissen. Die Sicht eines Spielers zeigt nur, was er im aktuellen Zug sieht; das berechnet `FogOfWar` aus dem Zustand, gespeichert wird es nicht.
- Die Erinnerung berechnet ein gemeinsamer Helfer: Aus den Sichten (oder Zuständen) eines Spielers über mehrere Züge liefert er für jedes Hexagon, das gerade nicht voll sichtbar ist, den letzten bekannten Stand. UI, KI-Spieler und Replay nutzen denselben Helfer, statt das Zurückgehen jeweils selbst zu bauen.
- Als Variante davon eine Faltung, die pro Zug das bisherige Wissen und die aktuelle Sicht zum neuen Wissen verbindet. Wer Erinnerung braucht, hält das Ergebnis; wer keine braucht, ruft sie nicht auf.
- Ein Client, der neu verbindet, bräuchte die früheren Sichten oder das angesammelte Wissen. Wer die Orders aller Züge hält ([[game-persistence]]), kann beides neu berechnen.
