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
- Das Upgrade `StartMatter` gibt 250 Matter mehr zum Start, aber nur als Start-Upgrade. In den Standardregeln bekommt es niemand, weil `StartUpgrades` leer ist.
- Das Modell unterscheidet nicht zwischen erforschbaren Upgrades und solchen, die nur zum Start zählen. `StartMatter` lässt sich erforschen, wenn eine Karte es anbietet, und bewirkt dann nichts. Kosten und Dauer seiner Stufe haben nur dafür eine Bedeutung.
- Start-Upgrades sind offen: Sie stehen in den Settings, und die Settings sind Teil jeder Sicht. Das ist als Regel festgelegt (siehe [[upgrade-concept]]).

## Ideen

- Ein Draft: Die Spieler wählen vor dem ersten Zug aus einer Auswahl.
- Upgrades, die es nur zu Spielbeginn gibt und die nicht erforschbar sind.
- Offen ist, ob die Spieler nacheinander oder gleichzeitig wählen und ob eine Wahl etwas kostet.

## Woran man es merkt

- Zwei Spieler können ein Spiel mit unterschiedlichen Upgrades beginnen, und sie haben das selbst entschieden.
