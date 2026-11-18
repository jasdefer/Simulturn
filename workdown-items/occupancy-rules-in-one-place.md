---
id: occupancy-rules-in-one-place
title: Belegungsregeln stehen mehrfach im Code
status: done
created: 2026-11-18
completed: 2026-11-18
---
## Problem

Seit [[engine-rules-for-clients]] steht jede Regel, mit der die Zugauflösung etwas ausrechnet, an einer Stelle: Preis, Reichweite, Einkommen, Kampf. Eine zweite Art von Regeln hat dieser Umbau ausgelassen: die Belegungsregeln. Sie sagen, was ein laufender Auftrag bindet, bis er fertig ist.

- Jede Einheit im Training braucht ihr eigenes Gebäude.
- Jede Konstruktion braucht ihren eigenen Dot.
- Eine Forschung braucht einen Dot auf ihrem Hexagon.

Diese Regeln standen als ausgeschriebene Vergleiche im `CommandValidator` und ein zweites Mal im `StateValidator`, die Dot-Regel der Forschung ein drittes Mal in der `TurnResolution`. Jede Stelle rechnete für sich.

Am 2026-11-18 ist daraus ein Fehler geworden: Ein Dot konnte ein Hexagon mit laufender Konstruktion verlassen. Der `CommandValidator` nahm die Befehle an, und der Zustand danach fiel beim `StateValidator` durch. Die Zufallsspiele fanden es nicht, weil `RandomCommands` gebundene Dots von sich aus stehen lässt.

Mit [[client-possible-orders]] kommen weitere Leser dieser Regeln dazu: die möglichen Orders, die Bots, die UI. Vorher sollte jede Regel nur einmal dastehen.

## Ergebnis

Der Fehler ist behoben: Je laufender oder im selben Zug bestellter Konstruktion bleibt ein Dot stehen (`MovementLeavesConstructionWithoutDot`). Ein ankommender Dot zählt nicht; der Dot, der baut, bleibt. Bei der Forschung zählt ein ankommender Dot weiterhin.

Die Belegungsregeln liegen in `Engine/Occupancy.cs`. Eine Funktion bekommt die Werte eines Hexagons und sagt, was frei ist; ein negativer Wert heißt, die Regel ist gebrochen.

| Regel | Stelle | Leser |
|---|---|---|
| Training braucht ein Gebäude | `Occupancy.TrainingSlots`, `Occupancy.FreeTrainingSlots` | `CommandValidator`, `StateValidator` |
| Konstruktion braucht einen Dot | `Occupancy.FreeDots` | `CommandValidator` (Bestellen, Wegziehen), `StateValidator` |
| Forschung braucht einen Dot | `Occupancy.HoldsResearch` | `CommandValidator` (Bestellen, Wegziehen), `StateValidator`, `TurnResolution` |
| Platz, den Gebäude bringen | `Occupancy.ProvidedSpace` | `TurnResolution`, Startzustand in `GameState` |
| Zug, in dem ein Auftrag fertig ist | `Schedule` | `TurnResolution` |

Der `StateValidator` fragt mit dem Zustand, der `CommandValidator` mit dem Zustand plus den Befehlen. Keiner vergleicht mehr selbst. Das Verhalten hat sich dabei nicht geändert.

Außerdem umbenannt: Die Bewegung in einem `Command` heißt `MovementOrder` (vorher `MovementCommand`), die Eigenschaft `Command.Movements`. Ein Command ist alles, was ein Spieler auf einem Hexagon befiehlt, eine Order ein einzelner Auftrag darin.

## Bewusst nicht geändert

- **Der Abbruch in der Zugauflösung.** Jeder im Kampf verlorene Dot bricht eine Konstruktion ab, jedes zerstörte Gebäude ein Training, auch wenn danach noch genug Dots oder Gebäude frei wären. Das ist eine eigene Regel (die Verluste treffen zuerst, wer arbeitet) und kein Vergleich mit der Belegung. Siehe offene Fragen.
- **`RandomCommands`** hält die Belegungsregeln weiter selbst ein. Sobald es die möglichen Orders gibt, kann es daraus würfeln, und diese Kopie entfällt.
- **`StateInvariants`** rechnet den Platz weiter selbst nach. Als zweite Meinung im Test ist das gewollt.
- **Die übrigen Regeln der Forschung** (eine je Upgrade, eine je Hexagon, nicht über die Tabelle hinaus) lesen schon gemeinsame Stellen (`UpgradesInResearch`, `MaxUpgradeLevel`, `NextUpgradeLevel`).
- **Die Ernte.** Eine in diesem Zug begonnene Konstruktion nimmt einen Dot aus der Ernte, eine laufende nicht. Das steht in `Harvest.Of` und ist eine andere Regel als die Belegung.

## Offene Fragen

- Soll ein verlorener Dot nur dann eine Konstruktion abbrechen, wenn danach zu wenige Dots übrig sind? Heute bricht er immer eine ab. Dasselbe gilt für zerstörte Gebäude und Trainings. Bei der Bewegung darf der Spieler wählen, welcher Dot geht; im Kampf entscheidet heute die Regel gegen ihn.
- Die Zufallsspiele reizen die Regeln nicht aus, weil `RandomCommands` vorsichtig würfelt. Eine Funktion verhindert, dass zwei Stellen verschieden rechnen, aber nicht, dass eine Stelle vergisst zu fragen.
