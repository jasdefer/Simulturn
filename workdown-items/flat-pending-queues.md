---
id: flat-pending-queues
title: Warteschlangen für Trainings und Konstruktionen ohne verschachtelte Dictionaries
status: done
created: 2026-11-10
completed: 2026-11-12
---
## Problem

`PlayerState.Trainings` und `PlayerState.Constructions` sind als `ImmutableDictionary<ushort, ImmutableDictionary<Hexagon, Army>>` bzw. `<…, Compound>>` modelliert: außen die Abschlussrunde, innen das Hexagon. Die Verschachtelung passt nur zu einer einzigen Operation, dem Entnehmen aller fälligen Einträge in `NextTurn`. Alle anderen Zugriffe laufen über das Hexagon und müssen beide Ebenen durchlaufen. Daraus entsteht ein Gerüst, das in keinem anderen Teil des Modells nötig ist:

- Der Builder-Typ `ImmutableDictionary<ushort, ImmutableDictionary<Hexagon, Army>.Builder>.Builder` steht achtmal im Code (`GameState`, `PlayerStateBuilder`, `DictionaryExtensions`). Die globalen Aliase `Trainings` und `Constructions` in `GlobalUsings.cs` existieren nur, weil der immutable Typ sonst unlesbar wäre.
- `GameState.GetNewTrainings` und `GameState.GetNewConstructions` sind zwei fast identische Methoden, die jeweils zwei Ebenen Builder anlegen.
- Die generische `Merge`-Extension über Builder-von-Buildern in `DictionaryExtensions` hat keinen anderen Nutzer.
- `PlayerStateBuilder.FromPlayerState` und `ToPlayerState` konvertieren die beiden Queues mit einem Lambda pro Ebene hin und zurück, bei jedem Zug für jeden Spieler.
- Beim Stornieren (Dots verlassen eine Baustelle, Gebäude wird zerstört) kürzt der Code den inneren Eintrag und muss danach prüfen, ob die äußere Ebene leer geworden ist. Dieser Aufräumschritt ist eine Fehlerquelle; [[pending-only-queues]] und [[training-cancellation-after-completion]] waren Fehler in genau diesem Bereich.
- Summen über die Queue (`UsedSpace`, Kosten, offene Trainings pro Einheit in `TurnPlan`) sind immer `SelectMany` über beide Ebenen.

Die Research-Queue aus [[research-cost-and-duration]] zeigt den Kontrast: `ImmutableDictionary<Hexagon, Research>` ist flach, und der Code darum herum ist deutlich kürzer.

Das Problem ist nicht die Unveränderlichkeit. `Armies`, `Compounds`, `Researches` und `UpgradeLevels` sind ebenfalls immutable und verursachen keinen Mehraufwand. Es ist die Wahl eines verschachtelten Dictionaries als Repräsentation einer Warteschlange mit zwei Zugriffspfaden.

## Was die Queue leisten muss

Aus dem heutigen Code abgelesen, in der Reihenfolge der Häufigkeit:

1. Lesen pro Hexagon: `Validate` und `IsValid` (fehlendes Gebäude, abgelaufene Einträge), Platzrechnung, `TurnPlan` und `RandomPlayer`, `Printer`, `GameStateSnapshot`, `StateInvariants`.
2. Stornieren pro Hexagon über alle Runden hinweg, mit einer festen Reihenfolge, welche Einträge zuerst gekürzt werden. Diese Reihenfolge ist Spielregel und darf sich nicht ändern.
3. Einreihen mit berechneter Abschlussrunde; gleiche Einheit auf gleichem Hexagon in gleicher Runde wird zusammengefasst.
4. Entnehmen aller Einträge einer Runde in `NextTurn`.

Die Queue ist klein, typischerweise unter zwanzig Einträge pro Spieler. Laufzeit spielt bei der Wahl der Struktur keine Rolle, Lesbarkeit und Determinismus (Snapshot, Gleichheit) schon.

## Betroffene Stellen

- `Source/Simulturn.Core/Model/State/GameState.cs`: Einreihen, Entnehmen, zwei Stornierungen, `IsValid`, `Validate`
- `Source/Simulturn.Core/Model/State/PlayerState.cs`, `PlayerStateBuilder.cs`, `GlobalUsings.cs`, `Extensions/DictionaryExtensions.cs`
- `Source/Simulturn.Core/Helper/Printer.cs`
- `Source/Simulturn.AI/Players/TurnPlan.cs`, `RandomPlayer.cs`
- `Test/Simulturn.Test.Common`: `GameStateSnapshot`, `StateInvariants`
- Tests: `GameStateTest`, `PendingQueueTest`, `ValidatePendingWorkTest`, zusammen rund 27 Zugriffe auf die Queues

## Ideen

Unverbindlich, der Implementierer entscheidet:

- Flacher Schlüssel aus Abschlussrunde und Hexagon statt zweier Ebenen. Als eigener `readonly record struct`, nicht als Tupel, wie `Research` auch.
- Ein eigener Wertetyp für die Queue, der die Repräsentation kapselt und nur die vier Operationen oben anbietet. Dann kann eine Implementierung beide Queues bedienen, weil `Army` und `Compound` denselben Additionsvertrag haben.
- Wenn die Queue so klein bleibt, kann sie rein immutabel sein und ohne Builder auskommen. Das passt dann nicht zum heutigen `PlayerStateBuilder`, der für alles Builder hält; entweder bekommt die Queue einen eigenen Builder, oder der `PlayerStateBuilder` hält sie als austauschbares Property.
- Die `Researches`-Queue nicht anfassen, sie hat das Problem nicht.

## Akzeptanzkriterien

- Kein Typ der Form `ImmutableDictionary<…, ImmutableDictionary<…>.Builder>.Builder` mehr im Code; die Aliase `Trainings` und `Constructions` in `GlobalUsings.cs` sind überflüssig oder zeigen auf den neuen Typ.
- Einreihen, Entnehmen und Stornieren existieren je einmal, nicht einmal pro Queue.
- Die Reihenfolge beim Stornieren ist unverändert. `PendingQueueTest`, die Stornierungstests in `GameStateTest` und `ValidatePendingWorkTest` laufen ohne Änderung an den Erwartungen durch.
- `GameStateSnapshot` liefert für denselben Zustand denselben String wie vorher, oder der Snapshot wird bewusst und dokumentiert geändert.
- Die AI-Spieler, die Arena und `EngineInvariantsTest` laufen unverändert.
