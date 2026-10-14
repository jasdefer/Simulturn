---
id: movement-range
title: Bewegungsreichweite pro Einheitentyp
status: todo
created: 2026-10-14
---
## Problem

Die Engine akzeptiert Bewegungen über jede Distanz. Eine Armee kann in einem Zug von einem Ende der Karte zum anderen springen.

## Ziel

Jeder Einheitentyp hat eine maximale Reichweite pro Zug, die über die `GameSettings` konfiguriert wird. Bewegungen darüber hinaus sind ungültig.

## Akzeptanzkriterien

- Eine Bewegung innerhalb der Reichweite aller beteiligten Einheitentypen erzeugt keine Validation.
- Eine Bewegung, bei der mindestens ein beteiligter Einheitentyp die Distanz nicht schafft, wird von `Validate` mit einer eigenen Command-Validation abgelehnt, aus der Unit, Distanz, Reichweite und Ziel hervorgehen.
- Ein Client kann ohne eigene Rechnung erfahren, welche Felder eine gegebene Armee von einem Hexagon aus erreichen kann und welcher Teil der Armee eine bestimmte Distanz schafft. Die alte UI hatte diese Logik selbst nachgebaut.
- Die bestehenden AI-Tests und die Arena laufen weiter. Die AI-Spieler kennen heute keine Reichweite.

## Zu entscheiden

- Wie sich `NextTurn` bei Bewegungen außerhalb der Reichweite verhält. Heute validiert `NextTurn` nichts; die AI (`SimulatorPlayer`, `BelievedState`) ruft `NextTurn` direkt ohne `Validate` auf. Der Review zu PR #8 hat bemängelt, dass die Regel dort nicht gilt und die Vorbedingung auch nicht dokumentiert ist.
- Ob die Reichweite ein Pflichtfeld der Settings wird (dann müssen alle sechs Settings-Fixtures nachgezogen werden, siehe [[map-generator-and-default-settings]]) oder einen Default hat.

## Referenz

Tag `archive/add-web-ui` (ehemaliger Branch), Commit 701b090 (Engine-Teil) und `Simulturn.Web/Services/MovementRange.cs`. Zur Orientierung, nicht als Vorlage.
