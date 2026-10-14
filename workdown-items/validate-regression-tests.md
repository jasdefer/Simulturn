---
id: validate-regression-tests
title: Regressionstests für Validate mit Warteschlangen auf anderen Hexagons
status: todo
created: 2026-10-14
---
## Hintergrund

`GameState.Validate` warf früher `KeyNotFoundException` statt eine Validation zu liefern, wenn der Spieler laufende Trainings oder Bauaufträge auf einem anderen Hexagon hatte als dem gerade kommandierten, und ebenso bei einem Bauauftrag auf einem Hexagon ganz ohne eigene Armee.

Der Fix ist bereits in `develop` (über PR #5 und #7). Es gibt aber keine Tests, die ein Wiederauftreten verhindern.

## Ziel

Tests, die absichern, dass `Validate` in diesen drei Fällen nicht wirft, sondern die passende Validation liefert:

- Laufendes Training an einem Hexagon, Trainingscommand an einem anderen.
- Laufender Bau an einem Hexagon, Baucommand an einem anderen.
- Baucommand auf einem Hexagon ohne eigene Armee.

## Hinweis

Die Settings-Fixture ist in den Tests bereits mehrfach kopiert. Nicht noch eine Kopie anlegen, siehe [[map-generator-and-default-settings]].

## Referenz

Tag `archive/add-web-ui` (ehemaliger Branch), Datei `Test/Simulturn.Core.Test/State/ValidateRegressionTest.cs`.
