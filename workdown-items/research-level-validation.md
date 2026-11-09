---
id: research-level-validation
title: Forschung kann über die Upgrade-Tabelle hinaus gestartet werden
status: done
created: 2026-10-14
updated: 2026-11-09
completed: 2026-11-09
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

## Entscheidung

Die Spielregel wurde vereinfacht: Ein Hexagon bietet ein Upgrade an oder nicht. Wie viele Stufen ein Upgrade hat, legt allein die Tabelle `GameSettings.Upgrades` fest. `HexagonSettings.ResearchableUpgrades` ist deshalb ein `ImmutableHashSet<Upgrade>`; die frühere Zählung der Vorkommen pro Hexagon entfällt. Damit gibt es keine Obergrenze pro Hexagon mehr, und das Gegenbeispiel aus dem Review (Hexagon A eine Stufe, B zwei) kann nicht mehr auftreten. Keine bestehende Karte nutzte unterschiedliche Zahlen pro Hexagon.

Die Regel lautet jetzt an allen Stellen gleich: abgeschlossene Stufen (`PlayerState.UpgradeLevels`) plus laufende Forschungen (`PlayerState.PendingResearchLevels()`) plus im Zug angeforderte Forschungen dürfen `GameSettings.MaxUpgradeLevel(upgrade)` nicht überschreiten.

- `Validate` zählt die Research-Commands pro Upgrade und meldet `UpgradeExceedsAvailableLevel` (mit `CompletedLevels`, `PendingLevels`, `RequestedLevels`, `MaxLevel`) für jeden betroffenen Command, unabhängig von der Reihenfolge. Die Kosten verwendet die nächste freie Stufe nach den laufenden Forschungen. Ein Hexagon ohne das Upgrade ergibt `UpgradeNotResearchableOnHexagon`.
- `IsValid` meldet `UpgradeLevelsExceedTable`, wenn abgeschlossene plus laufende Stufen die Tabelle übersteigen, und `UpgradeNotResearchableOnHexagon` für laufende Forschungen auf einem Hexagon ohne Angebot.
- `NextTurn` setzt validierte Commands voraus (XML-Doc). Es gibt keine zusätzliche Schutzprüfung.
- `byte` bleibt der Typ der Stufenzähler, die Summe ist durch die Tabellenlänge begrenzt.
- Die drei AI-Spieler nutzen `PendingResearchLevels()` und `MaxUpgradeLevel` statt eigener Zählungen. Sie stapeln weiterhin nicht.
- Eine Settings-Validierung ist nicht nötig: Ein Upgrade ohne Tabelle hat null Stufen und wird von `Validate` abgelehnt.

Tests: `ResearchLevelTest` in `Simulturn.Core.Test`.

## Referenz

Tag `archive/add-web-ui` (ehemaliger Branch), Commit d1c4079, und Review zu PR #8 (Tag `archive/core-fixes`). Zur Orientierung, nicht als Vorlage.
