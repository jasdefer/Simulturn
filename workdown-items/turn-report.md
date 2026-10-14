---
id: turn-report
title: Nachvollziehbarkeit der Zugauflösung
status: todo
created: 2026-10-14
---
## Problem

Nach `NextTurn` gibt es nur den neuen Zustand. Was in der Auflösung passiert ist, lässt sich daraus nur teilweise rekonstruieren. Bewegungen zeichnet die Engine gar nicht auf, Aufträge mit Dauer 1 tauchen nie in einer Warteschlange auf, und ein gewonnener Kampf ohne eigene Verluste hinterlässt keinen `Losses`-Eintrag.

## Ziel

Ein Spieler kann nach jedem Zug erfahren, was ihn betroffen hat: Kämpfe mit Gegnern und beidseitigen Verlusten, fertige Trainings, Bauten und Forschungen, verlorene Gebäude, eigene Bewegungen, Einkommen, Matter vorher und nachher.

## Akzeptanzkriterien

- Jedes der genannten Ereignisse ist für den betroffenen Spieler auffindbar, auch der verlustfreie Sieg.
- Fog of War bleibt gewahrt: Ein Spieler sieht nur, was er wissen darf. Was ein Kampf aufdeckt (`RevealedArmies`), darf er sehen.
- Die Information ist unabhängig von einer UI nutzbar, etwa auch für die AI-Analyse oder für Replays ([[game-persistence]]).

## Zu entscheiden

Woher die Information kommt: aus einem Vergleich von Zustand vorher, Commands und Zustand nachher (so hat es die alte UI gemacht, mit den oben genannten Lücken), aus einem Ereignisprotokoll, das `NextTurn` selbst erzeugt, oder aus etwas anderem. Diese Entscheidung ist Teil der Umsetzung.

## Referenz

`Simulturn.Web/Services/TurnReportService.cs` und `Models/TurnReport.cs` im Tag `archive/add-web-ui` (ehemaliger Branch). Zur Orientierung, nicht als Vorlage.
