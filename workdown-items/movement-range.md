---
id: movement-range
title: Bewegungsreichweite pro Einheitentyp
status: done
completed: 2026-11-13
created: 2026-10-14
updated: 2026-11-13
---
## Problem

Die Engine akzeptiert Bewegungen über jede Distanz. Eine Armee kann in einem Zug von einem Ende der Karte zum anderen springen. Auch ein Ziel, das gar nicht auf der Karte liegt, lehnt `CommandValidator.Validate` nicht ab.

Ein Client kann nicht erfragen, welche Felder eine Armee erreicht. Die alte UI hatte diese Logik selbst nachgebaut.

## Entscheidung

- **Distanz**: Es zählt die Hex-Distanz zwischen Start und Ziel. Was dazwischen liegt, spielt keine Rolle: Eine Armee zieht über andere Armeen und über Lücken in der Karte hinweg. Wegfindung und Abfangen sind nicht Teil der Regel.
- **Reichweite**: `GameSettings.MovementRange` ist ein Pflichtfeld mit einem Wert pro Einheitentyp. In `DefaultSettings` zieht jeder Typ ein Feld pro Zug. Die AI-Spieler ziehen ohnehin nur auf Nachbarfelder und bleiben damit gültig.
- **Gemischte Armeen**: Eine Bewegung ist ungültig, sobald ein beteiligter Typ die Distanz nicht schafft. Wer die schnellen Einheiten weiter schicken will, gibt dafür eine eigene Bewegung.
- **Eine Kopie der Regel**: `Movement.ReachableArmy` ist die einzige Stelle, die Reichweite und Distanz vergleicht. Die Validierung und die Feldliste für Clients fragen beide dort. Die Validierung zählt keine Felder auf.
- **Upgrades**: Die wirksame Reichweite kommt aus `Movement.Range`. Ein Upgrade, das die Reichweite ändert, wird dort eingebaut, siehe [[upgrade-concept]].
- **`TurnResolution.Resolve`** prüft die Reichweite nicht. Wie für jede andere Regel gilt die dokumentierte Vorbedingung, dass nur validierte Commands übergeben werden.

## Akzeptanzkriterien

- Eine Bewegung innerhalb der Reichweite aller beteiligten Einheitentypen erzeugt keine Validation.
- Eine Bewegung, bei der ein beteiligter Einheitentyp die Distanz nicht schafft, wird mit `MovementExceedsRange` abgelehnt, einmal pro betroffenem Typ, mit Unit, Distanz, Reichweite und Ziel.
- Eine Bewegung auf ein Feld außerhalb der Karte wird mit `MovementDestinationOffMap` abgelehnt.
- Ein Client erfährt ohne eigene Rechnung, welche Felder eine Armee von einem Hexagon aus erreicht und welcher Teil der Armee dort ankommt.
- Die bestehenden AI-Tests und die Arena laufen weiter.

## Umsetzung

`Movement` in `Simulturn.Core.Engine`:

- `Range(settings)`: die wirksame Reichweite pro Einheitentyp.
- `ReachableArmy(settings, army, distance)`: der Teil der Armee, dessen Typen die Distanz schaffen.
- `Destinations(settings, origin, army)`: jedes Feld der Karte, das mindestens eine Einheit erreicht, mit dem Teil der Armee, der dort ankommt. Die ganze Armee erreicht ein Feld, wenn der Teil gleich der Armee ist. Das Startfeld ist kein Ziel.

`CommandValidator` ruft für jede Bewegung `ReachableArmy` auf. Eine Bewegung ohne Einheiten wird nicht geprüft, die Zugauflösung überspringt sie. `StateInvariants.CheckTransition` prüft jede gemeldete Bewegung gegen die Reichweite, also auch in den Zufallsspielen. Tests in `MovementTest`, darunter einer, der für jedes Feld vergleicht, ob `Destinations` und `Validate` dasselbe sagen.

Zwei Tests in `GameRunnerTest` greifen den gegnerischen Start über zwei Felder direkt an. Sie spielen mit einer Reichweite von zwei für Squares.

## Referenz

Tag `archive/add-web-ui` (ehemaliger Branch), Commit 701b090 (Engine-Teil) und `Simulturn.Web/Services/MovementRange.cs`.
