---
id: opponent-estimate
title: Ein Bot weiß nicht, womit er beim Gegner rechnen muss
status: todo
created: 2026-11-16
depends_on:
- bot-game-diagnosis
---
## Problem

Ein fairer Bot sieht vom Gegner wenig: die Zahl der Einheiten auf sichtbaren Hexagonen, Gebäude, wo er hinsieht, und die Zusammensetzung einer Armee erst im Kampf (siehe [[reconnaissance]]). Dazwischen liegen Züge, in denen der Gegner baut, trainiert und forscht, ohne dass der Bot es sieht. Ein menschlicher Spieler füllt diese Lücke mit einer Vorstellung: „Er hat seit zehn Zügen zwei Expansionen, also kommt bald eine große Armee."

Ohne eine solche Vorstellung bleiben einem Bot zwei schlechte Wege. Er rechnet nur mit dem, was er zuletzt gesehen hat, und wird überrascht. Oder er rechnet mit dem Schlimmsten und traut sich nichts. Jede Entscheidung über Angriff, Verteidigung und Aufbau hängt daran, und ein Bot, der Züge durchspielt (siehe [[client-what-if]]), braucht die Vorstellung als Eingabe.

## Funde

Stand 2026-11-16, vor der Umsetzung neu zu prüfen.

- Was ein Spieler zuletzt gesehen hat, hält ein `HexagonMemory` je Hexagon, jeder Teil mit dem Zug der Sichtung. `FogOfWar.UpdateMemories` führt die Erinnerungen fort.
- Die Settings sind jedem Spieler bekannt: Karte, Startaufstellung, Start-Upgrades, Preise, Einkommen. Was ein Gegner in einer Zahl von Zügen höchstens erwirtschaftet haben kann, folgt daraus.
- Die Upgrades eines Gegners erfährt ein Spieler nur aus einem Kampf (`FightReport.UpgradeLevels`).
- Der eigene `PlayerTurnReport` zeigt Kämpfe, Zerstörungen und Niederlagen; auch daraus lässt sich schließen.

## Ideen

- Grenzen statt eines Werts: Was hat der Gegner mindestens (gesehen), was höchstens (nach den Regeln möglich)?
- Eine Schätzung dazwischen aus dem, was beobachtet wurde: gesehene Gebäude, die Zeit seit der Sichtung, verlorene Einheiten.
- Die Grenzen folgen aus Regeln und könnten in den Core gehören; die Schätzung ist eine Meinung und gehört zum Bot.
- An aufgezeichneten Spielen prüfen, wie gut eine Schätzung war: Dort ist die Wahrheit bekannt (siehe [[bot-game-diagnosis]]).
- Eine Annahme über das Verhalten: Was der Gegner hat, ist das eine, was er damit tut, das andere.

## Offene Fragen

- Wie genau muss die Schätzung sein, damit ein Bot besser spielt als mit „zuletzt gesehen"?
- Teilen sich alle Bots dieselbe Schätzung, oder ist sie Teil dessen, was einen Bot ausmacht?
- Soll ein Bot gezielt aufklären, um die Schätzung zu verbessern?
- Wäre eine solche Schätzung auch für einen Menschen in der UI eine Hilfe, oder nimmt sie ihm das Spiel ab?

## Woran man es merkt

- Ein Bot wird nicht mehr von einer Armee überrascht, die nach den Regeln zu erwarten war.
- Ein Bot greift an, wenn der Gegner nach allem, was er wissen kann, schwach ist.
- Die Abweichung zwischen Schätzung und Wahrheit lässt sich in aufgezeichneten Spielen messen.
