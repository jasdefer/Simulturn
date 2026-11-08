---
id: training-cancellation-after-completion
title: Zerstörte Gebäude brechen bereits fertige Trainings ab
status: todo
created: 2026-11-08
---
## Problem

`GameState.NextTurn` übernimmt Trainings mit Abschlussrunde gleich `Turn` zuerst in die Armee. Wird im selben Zug ein Gebäude zerstört, durchsucht der Abbruch der Trainings `Trainings.Where(x => x.Key >= Turn)` und erwischt damit auch das gerade fertige Training: Die Einheit wurde im Kampf bereits als Verlust abgezogen, und der Abbruch schreibt `UsedSpace` ein zweites Mal gut. Dasselbe `>= Turn` steht im Abbruch von Bauaufträgen nach Dot-Verlusten.

Gefunden durch die Space-Invariante in `StateInvariants` im Test `CancelTrainingsAfterCompoundDestruction`: `UsedSpace` ist 3, Armeen und laufende Trainings brauchen 6. Der Test ist bis zum Fix mit `Skip` markiert und beschreibt das Sollverhalten.

## Ziel

Ein Training oder Bau, der in diesem Zug abgeschlossen wurde, kann nicht mehr abgebrochen werden. Nach jedem Zug entspricht `UsedSpace` den Armeen plus laufenden Trainings.

## Akzeptanzkriterien

- `CancelTrainingsAfterCompoundDestruction` läuft ohne `Skip` und mit der Space-Invariante durch.
- Nur Trainings mit Abschlussrunde größer als die aktuelle Runde werden abgebrochen. Gleiches gilt für Bauaufträge nach Dot-Verlusten.
- Die Zufallsspiele in `EngineInvariantsTest` bleiben grün.

## Zu entscheiden

Ob abgeschlossene Einträge aus `Trainings`, `Constructions` und `Researches` entfernt werden. Heute bleiben sie dauerhaft im Zustand, und jeder Leser filtert mit `Key >= Turn`.
