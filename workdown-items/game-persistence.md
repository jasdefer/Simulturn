---
id: game-persistence
title: Spielstände speichern und wiederherstellen
status: todo
created: 2026-10-14
---
## Ziel

Ein laufendes Spiel soll gespeichert und später exakt wiederhergestellt werden können. Anwendungsfälle: Spielstand fortsetzen, Zustand für einen Bug-Report exportieren, Partie als Replay durchlaufen.

## Was feststeht

- Die Engine ist deterministisch: `NextTurn` hängt nur vom Zustand und den Commands ab, `GameSettings.Seed` wird nirgends gelesen. Settings plus die Orders aller Züge bestimmen jeden Zustand vollständig.
- `Validate` und `NextTurn` sind heute unabhängig von der Enumerationsreihenfolge der Dictionaries: Jeder Command wird für sich geprüft, Kampfverluste werden über alle Paarungen summiert, und zwei Spieler können nicht gleichzeitig auf demselben Hexagon ernten. Das muss so bleiben; der Review zu PR #8 hat gezeigt, wie leicht ein Akkumulator über Commands das bricht (siehe [[research-level-validation]]). Es gibt bislang keinen Test, der die Replay-Eigenschaft der Engine absichert.
- Die alte UI hatte nur einen Einweg-JSON-Export über anonyme DTOs. Dabei fiel auf, dass `System.Text.Json` am Modell scheitert: `Hexagon` als Dictionary-Schlüssel, `PlayerInitialization` als Tupel, `IUpgrade` polymorph ohne Diskriminator. Das ist eine Eigenschaft des Modells, keine Vorgabe für die Lösung.

## Zu entscheiden

- Was wird gespeichert: der vollständige Zustand, nur Settings und Orders mit Rekonstruktion per Replay, oder beides.
- Format und Ablage (Datei, Datenbank, anderes).
- Wo die Verantwortung liegt. Persistenz ist keine Spiellogik und gehört vermutlich nicht in `Simulturn.Core`. Der Core muss aber das liefern, was eine Persistenzschicht braucht.
- Verhalten bei Versionswechseln der Engine oder der Settings.

## Akzeptanzkriterien

- Ein Spiel nach mehreren Zügen mit laufenden Warteschlangen, Forschungen und Fog-of-War-Daten lässt sich speichern und ergibt nach dem Wiederherstellen denselben Zustand.
- Das Ergebnis ist deterministisch, damit Diffs zwischen Spielständen lesbar sind.
- Ein Test belegt, dass dieselben Settings und dieselben Orders in jedem Lauf denselben Zustand ergeben, auch über Prozessgrenzen hinweg (String-Hashes sind in .NET pro Prozess randomisiert, `PlayerIds` und `PlayerStates` sind nach Spieler-Id gehasht).
