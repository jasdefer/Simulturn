---
id: pending-only-queues
title: Warteschlangen enthalten nur offene Aufträge
status: done
completed: 2026-11-09
created: 2026-11-09
updated: 2026-11-09
depends_on: [training-cancellation-after-completion]
---
## Problem

`PlayerState.Trainings`, `Constructions` und `Researches` sind nach Abschlussrunde indiziert, und `NextTurn` entfernt abgeschlossene Einträge nie. Jeder Leser muss selbst mit `Key >= Turn` filtern: `IsValid`, `Validate`, `Printer`, `StateInvariants` und drei AI-Spieler, zusammen über zwanzig Stellen. Ein falsch gesetzter Vergleich war die Ursache von [[training-cancellation-after-completion]].

Die liegen gebliebenen Einträge sind keine Historie: Der Schlüssel ist die Abschlussrunde, nicht die Startrunde; Abbrüche werden herausgerechnet oder entfernen den Eintrag ganz; Forschungen überschreiben sich bei gleichem Schlüssel; Kämpfe (`Losses`), Bewegungen und Einkommen hinterlassen ohnehin keine Spur. Niemand im Repo liest abgeschlossene Einträge.

## Entscheidung

Die Historie eines Spiels sind die Commands aller Züge ([[game-persistence]]) und das Ereignisprotokoll pro Zug ([[turn-report]]). Die Warteschlangen enthalten ausschließlich offene Aufträge. Die Datenstruktur und ihre Unveränderlichkeit bleiben; `NextTurn` entfernt den Schlüssel der aktuellen Runde im Builder, direkt nachdem die Einträge angewendet wurden.

## Akzeptanzkriterien

- Nach jedem Zug ist jeder Schlüssel der drei Warteschlangen größer oder gleich `Turn`. `IsValid` meldet einen Verstoß mit einer eigenen Validation, `StateInvariants` prüft es in den Zufallsspielen mit.
- Kein Leser filtert mehr nach `Key >= Turn`.
- Ein Auftrag mit Dauer 1 ist nach dem Zug angewendet und nicht mehr in der Warteschlange.
- Die bestehenden Tests, die AI-Tests und die Arena laufen weiter.

## Umsetzung

`NextTurn` entfernt den Schlüssel `Turn` aus den drei Builder-Dictionaries, sobald die Einträge angewendet sind. Damit laufen die Abbruch-Filter beim Verlust von Gebäuden und Dots ohne Vergleich. `IsValid` meldet liegen gebliebene Einträge als `ExpiredTraining`, `ExpiredConstruction` und `ExpiredResearch`; die Zufallsspiele prüfen das über `StateInvariants`. Tests in `PendingQueueTest`.
