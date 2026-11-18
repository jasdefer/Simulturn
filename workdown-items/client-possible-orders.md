---
id: client-possible-orders
title: Ein Client kann nicht fragen, was möglich ist
status: in_progress
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

## Umsetzung

Stand 2026-11-18, auf dem Branch `feature/bot-foundation`. Vorarbeit war [[occupancy-rules-in-one-place]].

`Orders.Possible(view, commands)` sagt einem Client, was er nach den bisher eingegebenen Befehlen noch befehlen kann: das Restbudget und je Hexagon ein `PossibleHexagonOrders`.

| Feld | Bedeutung | Woher die Grenze kommt |
|---|---|---|
| `Training` | je Einheitentyp: so viele gehen noch | `Occupancy.FreeTrainingSlots`, Matter und Platz aus `Costs.Budget` |
| `Construction` | je Gebäude: so viele gehen noch | `Occupancy.FreeDots` der Dots, die bleiben, Matter, `IsBuildable` |
| `Research` | diese Upgrades kann man hier starten | der `CommandValidator` selbst (`AcceptsResearch`) |
| `Movable` | diese Einheiten dürfen das Hexagon noch verlassen | `Occupancy.FreeDots`, `Occupancy.HoldsResearch` |

Entschieden:

- **Die Antwort hat die Form eines Commands,** mit Grenzen statt Mengen. Es gibt keinen eigenen Typ für eine einzelne Order.
- **Grenzen statt Aufzählung.** Jede Grenze gilt für ihre Order allein: Jede einzelne Order bis zu ihrer Grenze lässt die Befehle gültig. Zwei zusammen können das Budget oder die Dots sprengen. Alle gültigen Kombinationen lassen sich nicht sinnvoll als Daten herausgeben.
- **Iterativ.** Ein Client nimmt aus einer Antwort eine Order, legt sie mit `Orders.Train`, `Construct`, `Research` oder `Move` zu den Befehlen und fragt neu. Wer einen ganzen Zug auf einmal baut, prüft ihn weiter mit `CommandValidator.Validate`.
- **Jeder Zwischenstand ist ein gültiger Zug.** Der letzte Dot einer Forschung ist deshalb erst beweglich, wenn ein anderer dorthin unterwegs ist. Ein Tausch der Dots zweier Forschungs-Hexagone ist über Schritte nicht erreichbar.
- **Bewegung als bewegliche Armee,** nicht als Liste aller Ziele. Wohin ein Einheitentyp kommt, sagt `Movement.Destinations`.
- **Keine zweite Kopie der Regeln.** `Orders` schreibt keinen Regelvergleich aus. In den Zufallsspielen prüft `OrderInvariants` nach jedem Zug: Jede Order wird bis zu ihrer Grenze angenommen und mit einer mehr abgelehnt.

Noch offen aus diesem Item:

- Reichweite und Reisezeit über mehrere Züge, und daraus die Bedrohung durch gesehene Armeen.
- Was ein Upgrade ändert, als Antwort der Engine.
- Eine Order wieder herausnehmen. Das kann die Befehle ungültig machen (der Dot, der eine Forschung hält).
- `Orders.Possible` rechnet bei jedem Aufruf alles neu. Ob das für Bots reicht, zeigt die Arena.
- `RandomCommands` hält die Regeln noch selbst ein, statt aus den möglichen Orders zu würfeln.
