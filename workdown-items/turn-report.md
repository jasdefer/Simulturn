---
id: turn-report
title: Nachvollziehbarkeit der Zugauflösung
status: todo
created: 2026-10-14
updated: 2026-11-10
depends_on: [turn-result]
---
## Problem

Nach `NextTurn` gibt es nur den neuen Zustand. Was in der Auflösung passiert ist, lässt sich daraus nur teilweise rekonstruieren. Bewegungen zeichnet die Engine gar nicht auf, Aufträge mit Dauer 1 tauchen nie in einer Warteschlange auf, und ein gewonnener Kampf ohne eigene Verluste hinterlässt keinen `Losses`-Eintrag.

## Ziel

Ein Spieler kann nach jedem Zug erfahren, was ihn betroffen hat: Kämpfe mit Gegnern und beidseitigen Verlusten, fertige Trainings, Bauten und Forschungen, verlorene Gebäude, eigene Bewegungen, Einkommen, Matter vorher und nachher.

## Entscheidung

Die Ereignisse schreibt `NextTurn` selbst, an der Stelle, an der sie passieren, in den Report aus [[turn-result]]. Kein Vergleich zweier Zustände: Ein Diff sieht nur das Netto und müsste die Auflösung rückwärts nachbauen (Begründung in [[turn-result]]). Die Struktur und die zwei vorhandenen Ereignisse (Verluste, aufgedeckte Armeen) liefert [[turn-result]]; dieses Item füllt den Report mit dem Rest.

## Ereignisse

Pro Phase von `NextTurn`, in der Reihenfolge der Auflösung:

- Einkommen pro Hexagon, mit der Zahl der erntenden Dots.
- Fertige Trainings und Bauten pro Hexagon, auch die mit Dauer 1.
- Abgeschlossene Forschung mit der erreichten Stufe.
- Eigene Bewegungen, Start und Ziel mit Armee.
- Kämpfe pro Hexagon: Beteiligte, aufgedeckte Armeen, Verluste beider Seiten, auch der verlustfreie Sieg.
- Zerstörte Gebäude pro Hexagon, mit dem Angreifer.
- Stornierte Bauten (Dot-Verluste) und Trainings (Gebäudeverlust), mit dem erstatteten Platz.
- Abgebrochene Forschung (kein Dot mehr auf dem Hexagon).
- Matter vorher und nachher.

## Akzeptanzkriterien

- Jedes der genannten Ereignisse ist für den betroffenen Spieler auffindbar, auch der verlustfreie Sieg.
- Fog of War bleibt gewahrt: Ein Spieler sieht nur, was er wissen darf. Was ein Kampf aufdeckt, darf er sehen; die Bewegungen und das Einkommen anderer Spieler nicht.
- Die Information ist unabhängig von einer UI nutzbar, etwa auch für die AI-Analyse oder für Replays ([[game-persistence]]).
- Ein Test in Layer 3 (`StateInvariants.CheckTransition`) vergleicht Zustand vorher und nachher und prüft, dass der Report jede Änderung an Armeen, Gebäuden, Matter und Platz erklärt. Der Diff ist Prüfung, nicht Quelle.

## Referenz

`Simulturn.Web/Services/TurnReportService.cs` und `Models/TurnReport.cs` im Tag `archive/add-web-ui` (ehemaliger Branch). Zur Orientierung, nicht als Vorlage.
