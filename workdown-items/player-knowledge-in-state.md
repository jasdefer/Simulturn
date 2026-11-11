---
id: player-knowledge-in-state
title: Wissen der Spieler liegt im Regelzustand
status: in_progress
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

## Entscheidung zu `Visibilities`

Das Feld `PlayerState.Visibilities` entfällt. Die Sicht wird nie gespeichert: `FogOfWar.GetVisibility(armies, gameSettings)` berechnet sie dort, wo sie gebraucht wird, nämlich in `FogOfWar.GetPlayerGameState`. Damit kann die Sicht nicht von den Armeen abweichen, und wer einen `PlayerState` baut, muss nicht an sie denken. Die Invariante, die gespeicherte und berechnete Sicht verglichen hat, ist mit dem Feld entfallen.

## Entscheidung zu `Memories`

Der Zustand enthält nichts über Wissen. `PlayerState.Memories` und der Durchlauf `FogOfWar.UpdateMemories` nach jeder Auflösung entfallen.

- **Die Sicht zeigt nur den aktuellen Zug.** `HexagonObservation` trägt Restmaterie und gegnerische Gebäude für Hexagons ab teilweiser Sicht, gegnerische Einheiten ab voller Sicht, alles direkt aus dem Zustand gelesen. `LastSeenTurn` gibt es dort nicht mehr. Für Hexagons außer Sicht ist die Beobachtung leer.
- **Die Erinnerung berechnet ein Helfer aus den Sichten.** `FogOfWar.GetMemories(views)` nimmt die Sichten eines Spielers in aufsteigender Zugfolge, die letzte ist die aktuelle. Er geht Zug für Zug zurück und liefert pro Hexagon, was dort zuletzt gesehen wurde, und bricht ab, sobald jedes Hexagon geklärt ist.
- **Sichtbares wird nicht erinnert.** Der Helfer liefert nur, was die aktuelle Sicht nicht zeigt. Ein nie gesehenes Hexagon hat keinen Eintrag; dafür gilt die Startmaterie aus den Settings.
- **Zwei Sichtungen pro Hexagon.** `HexagonMemory.Static` hält Zug, Restmaterie und gegnerische Gebäude, `HexagonMemory.Units` hält Zug und Anzahl der gegnerischen Einheiten. Die Schwellen sind verschieden, deshalb können beide aus verschiedenen Zügen stammen: Ein Hexagon in teilweiser Sicht zeigt die Gebäude aktuell, die Einheiten nur aus der Erinnerung. Ein ohne Einheiten gesehenes Hexagon ist eine Sichtung mit leerer Anzahl.
- **Wer Erinnerung braucht, bewahrt seine Sichten auf.** Die Engine tut es nicht. Eine fehlende Sicht ist ein Zug ohne Wissen. Ein Client, der neu verbindet, braucht die früheren Sichten; wer die Orders aller Züge hält ([[game-persistence]]), kann sie neu berechnen.

### Kosten des Helfers

Der Helfer ist eine reine Funktion der Sichten; es gibt kein zweites Objekt, das fortgeschrieben werden muss und falsch werden kann. Sein Preis: Für ein Hexagon, das lange nicht oder nie gesehen wurde, läuft er entsprechend weit zurück, im schlechtesten Fall bis zum ersten Zug.

Gemessen an 75 Partien (fünf Spielertypen gegeneinander auf drei Karten, Zuglimit 250): 51.893 Aufrufe, zusammen 17,8 Sekunden von 33,6 Sekunden Laufzeit, rund 0,34 ms pro Aufruf. Fast alle Aufrufe stammen aus den Rollouts des `SimulatorPlayer`. In der normalen Testsuite ist der Unterschied nicht messbar.

Verworfen wurde eine Faltung, die pro Zug das bisherige Wissen und die aktuelle Sicht zum neuen Wissen verbindet. Sie liefert dasselbe Ergebnis und spart diese Zeit, wäre aber ein zweiter Mechanismus neben dem Helfer. Sie bleibt eine Option für das Redesign der KI-Spieler, falls die Rollouts dort schneller werden müssen.

### KI-Spieler

Die KI-Spieler werden neu entworfen und sind hier nur so weit angepasst, dass sie weiterlaufen. `Knowledge` in `Simulturn.AI` sammelt die Sichten eines Spielers und verbindet die aktuelle Sicht mit `GetMemories` zu `KnownHexagon` (die Sicht, sonst die letzte Sichtung von Restmaterie und Gebäuden). Erinnerte Einheiten nutzt noch kein Spieler. Der `SimulatorPlayer` gibt seinem ausgerollten Commander die bisherigen Sichten mit, weil dessen Erinnerung nicht mehr im geglaubten Zustand steckt.

Das Verhalten der Spieler ist bis auf eine Stelle unverändert, geprüft an denselben 75 Partien vor und nach der Änderung. Die Abweichung betrifft zwei Partien des `SimulatorPlayer` und ist die Korrektur eines Fehlers: Im ersten Zug eines Rollouts sah das Gegnermodell bisher weder Restmaterie noch Gebäude, auch nicht auf Hexagons in Sicht. Der geglaubte Zustand wurde ohne Erinnerung gebaut, und die Sicht las beides nur aus der Erinnerung. Jetzt liest die Sicht aus dem Zustand, das Gegnermodell sieht vom ersten Zug an, was in Sicht ist. Stellt man das alte Verhalten künstlich wieder her, sind alle 75 Partien identisch.
