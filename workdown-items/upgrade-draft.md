---
id: upgrade-draft
title: Spieler starten alle mit denselben Voraussetzungen
status: todo
created: 2026-11-15
---
## Problem

Upgrades sollen dem Spiel Asymmetrie geben (siehe [[upgrade-concept]]). Heute beginnen alle Spieler gleich, und ein Unterschied entsteht erst durch Forschung im Lauf des Spiels. Eine Wahl vor dem ersten Zug gibt es nicht.

## Funde

Stand 2026-11-15, vor der Umsetzung neu zu prüfen.

- `GameSettings.StartUpgrades` ist bereits pro Spieler, und `GameState` übernimmt die Einträge als abgeschlossene Stufen. Gefüllt wird die Einstellung nur in Tests.
- Wer die Start-Upgrades festlegt, ist offen: Sie stehen in den Settings, bevor das Spiel beginnt. Einen Schritt, in dem ein Spieler wählt, kennt die Engine nicht.
- `UpgradeStartMatter` wurde mit [[upgrade-concept]] entfernt, weil es nichts bewirkte. Mehr Startmaterie ergibt nur als Wahl zu Spielbeginn Sinn, nicht als Forschung.

## Ideen

- Ein Draft: Die Spieler wählen vor dem ersten Zug aus einer Auswahl.
- Upgrades, die es nur zu Spielbeginn gibt und die nicht erforschbar sind.
- Offen ist, ob die Gegner die Wahl sehen oder erst im Kampf davon erfahren.

## Woran man es merkt

- Zwei Spieler können ein Spiel mit unterschiedlichen Upgrades beginnen, und sie haben das selbst entschieden.
