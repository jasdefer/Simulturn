---
id: upkeep
title: Upkeep steht im Konzept, aber nicht in der Engine
status: todo
created: 2026-11-18
---
## Problem

Das Konzept in der `README.md` (Abschnitt „Resource Gathering and Supply Cap") sieht Upkeep vor: Wer viel Supply verbraucht, bekommt nur noch einen Teil dessen, was seine Dots ernten. Das Vorbild ist Warcraft 3. Die Engine kennt davon nichts. Eine große Armee kostet heute nur ihren Preis und ihren Space, danach nichts mehr; wer einmal vorne liegt, hat keinen laufenden Nachteil aus seiner Größe.

## Funde

Stand 2026-11-18, vor der Umsetzung neu zu prüfen.

- Die README beschreibt: mehrere Upkeep-Stufen, die Stufe folgt aus dem verbrauchten Supply des Spielers, jede Stufe zieht einen Anteil vom Geernteten ab, bevor es dem Spieler gutgeschrieben wird. Der abgezogene Teil ist trotzdem geerntet und fehlt dem Hexagon.
- `Harvest.Of` kennt keinen Abzug, `GameSettings` keine Einstellung dafür. In der Git-Historie kam Upkeep in keiner `.cs`-Datei vor.
- Supply heißt im Code Space: `PlayerState.UsedSpace` und `AvailableSpace`, die Preise in `GameSettings.RequiredSpace`, die Quellen in `ProvidedSpace`. Der Space ist heute nur eine harte Grenze für das Training.
- `Harvest.Of` bekommt die Werte eines Hexagons, nicht den Spieler. Für die Stufe braucht es den verbrauchten Space.
- `IncomeReport` hält die Ernter und die Matter. Mit Upkeep sind es zwei Zahlen: was das Hexagon verliert und was der Spieler bekommt.
- `Harvest.Forecast` ist die Stelle, an der ein Client sein Einkommen erfragt. Liegt der Abzug in `Harvest`, bekommen ihn UI und Bots ohne eigene Rechnung.
- `UsedSpace` steigt mit dem Trainingsauftrag, nicht erst mit der fertigen Einheit.

## Ideen

Keine Vorgaben.

- Die Stufen als Liste in den `GameSettings`: ab welchem verbrauchten Space welcher Anteil bleibt. Eine leere Liste heißt kein Upkeep.
- Eine Zahl aus dem Gespräch vom 2026-11-18 als Startwert: ab 50 Space bleiben 70 %.
- Ein Upgrade, das die Stufen verschiebt oder den Abzug mildert, passt zu [[upgrade-variety]].
- Der Zufallsbot aus [[bot-strength-measurement]] beachtet Upkeep nie. Ob ein Bot absichtlich unter einer Stufe bleibt, ist eine Strategie für spätere Bots und in der Arena messbar.

## Offene Fragen

- Zählt der verbrauchte Space zu Beginn des Zugs oder nach den Aufträgen des Zugs?
- Wird gerundet, und zu wessen Gunsten?
- Zählen Dots zum Supply, der die Stufe bestimmt? Dann bestraft Upkeep auch den, der nur seine Wirtschaft ausbaut.
- Wie viele Stufen, und welche Zahlen? Das ist eine Frage des Balancings und braucht Spiele.
- Sieht ein Spieler die Stufe eines Gegners? Sie verrät dessen Armeegröße.

## Woran man es merkt

- Ein Spieler über der Schwelle bekommt weniger Matter gutgeschrieben, als seine Dots ernten, und das Hexagon verliert die volle Ernte.
- Der Zugbericht zeigt beide Zahlen.
- `Harvest.Forecast` sagt einem Client das Einkommen nach Abzug voraus.
- Die README und die Engine sagen dasselbe.
