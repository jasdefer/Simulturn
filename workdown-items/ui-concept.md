---
id: ui-concept
title: UI-Konzept erarbeiten
status: todo
created: 2026-10-14
updated: 2026-11-14
parent: ui
---
## Ziel

Grundsätzliche Planung der neuen UI, bevor Umsetzungs-Items angelegt werden. Der alte Blazor-Ansatz aus `archive/add-web-ui` wird nicht fortgeführt.

## Feststehend

Am 2026-11-14 festgelegt:

- Die erste UI dient dazu, zu testen, wie sich das Spiel anfühlt.
- Sie hält alles im Speicher. [[game-persistence]] ist keine Voraussetzung.
- Vor der UI müssen das Spielprinzip und die Grundlagen stehen, auch das, was die UI von der Engine abfragt: [[upgrade-concept]] und [[engine-rules-for-clients]]. Gegner, gegen die sich ein Spiel lohnt, behandelt [[ai-opponents-too-weak]].

## Zu klären

- Zweck der UI: Testwerkzeug für die Engine, spielbares Spiel, oder beides in Stufen.
- Technologie, Plattform und Projektstruktur.
- Spielmodi: Hot-Seat mit Fog of War, Gott-Modus zum Testen, Spiel gegen die AI-Gegner aus `Simulturn.AI`, später eventuell Netzwerk.
- Welche Engine-Funktionen die UI braucht und daher nicht in der UI nachgebaut werden dürfen. Kandidaten: [[combat-forecast-core]], [[movement-range]], [[turn-report]], [[map-generator-and-default-settings]], [[game-persistence]].
- Umgang mit Engine-Fehlern: Eine Exception aus `NextTurn` oder `Validate` darf die Sitzung nicht beenden und keine eingegebenen Orders verlieren.

## Beobachtungen aus der alten UI

Zeitreise durch die Zughistorie, Debug-Export des Zustands und die Anzeige der durch Kämpfe aufgedeckten Gegnerarmeen waren beim Testen nützlich. Der Rest wurde ohne Konzept gebaut und ist keine Grundlage.

## Ergebnis

Ein Konzept-Dokument und daraus abgeleitete Umsetzungs-Items unter [[ui]].
