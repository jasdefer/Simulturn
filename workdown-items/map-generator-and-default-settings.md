---
id: map-generator-and-default-settings
title: Karten erzeugen und Settings nicht sechsfach pflegen
status: todo
created: 2026-10-14
---
## Problem

1. Die Standardwerte für `GameSettings` (Kosten, Dauern, Armor, Einkommen, Upgrades, Startkarte) stehen als sechs fast identische Objektinitialisierer im Code: `GameStateTest`, `PlayerGameStateTest`, `ConsoleGameSettings`, `GameSettingsFactory` in Simulturn.AI, `GameRunnerTest`, `RandomPlayerTest`. Die Klasse selbst existiert nur einmal. Jedes neue Pflichtfeld (siehe [[movement-range]]) muss sechsmal nachgezogen werden, und die Werte driften auseinander.
2. Es gibt keine Möglichkeit, eine Karte zu erzeugen, außer sie Hexagon für Hexagon von Hand zu definieren. Die alte UI hatte dafür einen eigenen Generator, der nur dort existierte.

## Ziel

- Tests, Console, Arena und eine künftige UI greifen auf eine gemeinsame Quelle für Standard-Settings zu.
- Aus wenigen Parametern (Spieleranzahl, Größe, Seed, Ressourcen) entsteht eine spielbare, faire Karte, ohne UI-Abhängigkeit.

## Akzeptanzkriterien

- Gleicher Seed und gleiche Parameter ergeben dieselbe Karte.
- Jeder Spieler hat ein Startfeld, und die Ausgangslage ist für alle Spieler gleichwertig.
- Die generierte Karte ist mit den Standard-Settings ein gültiger `GameState` (`IsValid` leer).
- Die Fixture-Kopien sind abgebaut; AI-Tests, deren Verhalten von bestimmten Werten abhängt, laufen weiter.

## Zu entscheiden

Wo das lebt (Core oder ein eigenes Projekt ohne UI-Bezug), welche Parameter sinnvoll sind, und was Fairness konkret heißt (Symmetrie, gleiche Ressourcen, gleiche Distanzen).

## Referenz

`Simulturn.Web/Services/MapGeneratorService.cs`, `Models/MapGeneratorOptions.cs` und `Services/DefaultSettings.cs` im Tag `archive/add-web-ui` (ehemaliger Branch). Zur Orientierung, nicht als Vorlage.
