---
id: combat-forecast-core
title: Kampfausgang vorab berechnen können
status: todo
created: 2026-10-14
---
## Problem

Die Kampfauflösung steckt in der privaten Methode `GameState.Fight` und ist von außen nicht erreichbar. Die alte UI brauchte eine Kampfprognose für den Spieler und hat die Mathematik kopiert (`Simulturn.Web/Services/CombatForecast.cs`). Zwei Implementierungen derselben Regel driften auseinander.

## Ziel

Ein Client kann für zwei Armeen den Ausgang eines Kampfes vorab bestimmen, und zwar garantiert mit derselben Rechnung, die `NextTurn` anwendet.

## Akzeptanzkriterien

- Prognose und tatsächliche Auflösung liefern für dieselben Armeen und Upgrade-Boni identische Verluste beider Seiten.
- Die Kampfregel existiert genau einmal im Code.
- Fog of War bleibt gewahrt: Die Prognose eines Spielers darf nur auf dem beruhen, was dessen Spieleransicht kennt. Wo diese Verantwortung liegt, ist Teil der Lösung.

## Referenz

`GameState.Fight` in `develop`; `CombatForecast.cs` im Tag `archive/add-web-ui` (ehemaliger Branch) zur Orientierung, nicht als Vorlage.
