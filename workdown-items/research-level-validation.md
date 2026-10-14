---
id: research-level-validation
title: Forschung kann über die Upgrade-Tabelle hinaus gestartet werden
status: todo
created: 2026-10-14
---
## Problem

`GameState.Validate` prüft eine Forschung nur gegen `PlayerState.UpgradeLevels`, also gegen abgeschlossene Stufen. Laufende Forschungen (`PlayerState.Researches`, Schlüssel = Abschlussrunde) zählen nicht mit. Wer dieselbe Forschung in aufeinanderfolgenden Runden startet, überschreitet die Upgrade-Tabelle `GameSettings.Upgrades[upgrade]`. Beim nächsten Kampf liest `PlayerState.ExponentBonusFromUpgrades` außerhalb des Arrays, und `NextTurn` bricht mit `IndexOutOfRangeException` ab.

Dasselbe passiert, wenn eine Karte mehr Vorkommen eines Upgrades in `HexagonSettings.ResearchableUpgrades` anbietet, als die Tabelle Stufen definiert.

## Ziel

Ein Spieler kann zu keinem Zeitpunkt mehr Stufen eines Upgrades abgeschlossen oder in Arbeit haben, als die Tabelle definiert und die genutzten Hexagons anbieten. `Validate` lehnt solche Commands ab, `IsValid` erkennt solche Zustände.

## Akzeptanzkriterien

- Laufen bereits so viele Forschungen eines Upgrades, dass alle definierten Stufen belegt sind, wird eine weitere abgelehnt.
- Zwei Forschungen desselben Upgrades im selben Zug auf verschiedenen Hexagons teilen sich die verbleibenden Stufen. Das Ergebnis ist unabhängig von der Reihenfolge, in der die Commands enumeriert werden. Der Review zu PR #8 fand eine Umsetzung, die je nach Hash-Reihenfolge des Dictionaries unterschiedlich entschied.
- `Validate` und `IsValid` wenden dieselbe Regel an. Ein Zustand, der nur über akzeptierte Commands erreicht wurde, ist gültig. Gegenbeispiel aus dem Review: Hexagon A bietet eine Stufe, Hexagon B zwei; Forschung auf A, nächste Runde auf B, beides akzeptiert, trotzdem meldete `IsValid` einen Fehler, weil es das globale Spielerlevel gegen die Stufenanzahl eines einzelnen Hexagons verglich.
- Die voll erforschte letzte Stufe wird im Kampf ohne Fehler angewendet.
- Die Regel "welche Forschungen laufen noch" existiert genau einmal. Heute steht sie in drei AI-Spielern (`CommanderPlayer`, `RandomPlayer`, `TurnPlan`), und der alte Branch hat sie im Core noch zweimal inline ergänzt.
- Der Aufwand pro Spieler wächst nicht quadratisch mit der Zahl laufender Forschungen. Die Arena ruft `IsValid` in jedem Zug jedes Spiels.

## Zu entscheiden

- Wie `NextTurn` mit einem nicht validierten Command umgeht, der über die Tabelle hinausgeht: überspringen, oder Vorbedingung dokumentieren. Der alte Branch warf mitten im Zug.
- Ob der Typ von `UpgradeExceedsAvailableLevel.CurrentLevel` (`byte`) noch passt, wenn laufende Forschungen mitgezählt werden.

## Referenz

Tag `archive/add-web-ui` (ehemaliger Branch), Commit d1c4079, und Review zu PR #8 (Tag `archive/core-fixes`). Zur Orientierung, nicht als Vorlage.
