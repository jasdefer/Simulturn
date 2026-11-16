---
id: client-possible-orders
title: Ein Client kann nicht fragen, was möglich ist
status: todo
created: 2026-11-16
---
## Problem

Wer einen Zug plant, fragt zuerst, was überhaupt geht: Was kann ich auf diesem Hexagon trainieren, bauen, erforschen, und wie viel davon? Was kann ich mir danach noch leisten? Wohin kommt diese Armee, in diesem Zug und in den nächsten? Welche Armee des Gegners kann mich wann erreichen?

Eine UI braucht die Antworten, um nur anzubieten, was erlaubt ist. Ein Bot braucht sie, um die Züge aufzuzählen, zwischen denen er wählt. Heute kann ein Client fertige Befehle prüfen lassen, aber er kann nicht fragen, welche Befehle es gibt. Er muss raten und prüfen oder die Regeln nachbauen, und genau das sollte seit [[engine-rules-for-clients]] nicht mehr nötig sein.

## Funde

Stand 2026-11-16, vor der Umsetzung neu zu prüfen.

- `CommandValidator.Validate(view, commands)` prüft fertige Befehle aus der Sicht eines Spielers und nennt die gebrochenen Regeln.
- Für die Bewegung gibt es die Frage bereits: `Movement.Destinations` liefert die Ziele einer Armee in einem Zug.
- Für Training, Konstruktion und Forschung gibt es `Costs` (der Preis) und `GameSettings.NextUpgradeLevel`, aber keine Stelle, die sagt, was auf einem Hexagon gerade möglich ist.
- `Costs.Of` liefert den Preis aller Befehle eines Zugs. Was danach an Matter und Platz bleibt, rechnet der Client selbst.
- Die Bewegung über mehrere Züge (wie viele Züge braucht eine Armee von A nach B, was erreicht sie in n Zügen) gibt es nicht.

## Ideen

- Die möglichen Aufträge eines Hexagons aus der Sicht des Spielers, mit den schon eingegebenen Befehlen des Zugs.
- Das Restbudget nach den eingegebenen Befehlen.
- Reichweite und Reisezeit über mehrere Züge. Daraus folgt auch, welche gesehenen Armeen des Gegners ein Hexagon in n Zügen erreichen; in einer UI wäre das eine Anzeige der Bedrohung.
- Was ein Upgrade ändert, als Antwort der Engine statt als Text: der Kampf mit und ohne.

## Offene Fragen

- Der Raum aller Befehle eines Zugs ist riesig (jede Aufteilung jeder Armee auf jedes Ziel). Welche Fragen lassen sich überhaupt als Aufzählung beantworten, welche nur als Grenze („höchstens so viele")?
- Gehören Fragen über mehrere Züge in die Engine, oder nur die Regel eines Zugs?
- Was braucht die UI wirklich? Das hängt an [[ui-concept]].

## Woran man es merkt

- Ein Client kann einen gültigen Zug zusammenstellen, ohne eine Regel selbst zu rechnen und ohne ungültige Befehle zu probieren.
- UI und Bots stellen dieselben Fragen an dieselbe Stelle.
