---
id: upgrade-variety
title: Es gibt nur drei gleichartige Upgrades
status: todo
created: 2026-11-15
---
## Problem

Upgrades sollen dem Spiel Vielfalt und Asymmetrie geben. Mit [[upgrade-concept]] gibt es drei erforschbare Upgrades, und alle tun dasselbe: Sie erhöhen den Kampfexponenten einer Kampfeinheit. Wer forscht, entscheidet nur, welche Einheit stärker wird. Das Modell trägt verschiedene Arten von Wirkung, im Spiel spürbar ist eine.

## Funde

Stand 2026-11-15, vor der Umsetzung neu zu prüfen.

- Die Union `UpgradeEffect` hat zwei Fälle: `FightExponentBonus` und `StartMatterBonus`. Der zweite zählt nur als Start-Upgrade (siehe [[upgrade-draft]]).
- Für die meisten denkbaren Wirkungen fehlt noch die Stelle, die den wirksamen Wert eines Spielers liefert. Kosten, Trainingsdauer, Einkommen und Schaden an Gebäuden werden an vielen Stellen direkt aus den Settings gelesen, auch in der AI. Das überschneidet sich mit [[engine-rules-for-clients]].
- Eine Bedingung, die nur an den beiden Armeen eines Kampfes hängt, braucht keinen weiteren Kontext. Der Exponent wird aber einmal pro Spieler gerechnet, nicht pro Paarung.
- `UpgradeDefinition.AffectedUnits` nennt nur Einheitentypen. Ein Upgrade auf Gebäude hat noch keine Angabe „wirkt auf".
- `Movement.Range` ist die Stelle für die wirksame Reichweite, bekommt aber die Upgrades eines Spielers noch nicht.
- Für die Sicht gibt es keine solche Stelle (siehe [[reconnaissance]]).
- `Combat.Fight` kennt nur Armeen und Upgrade-Stufen. Ein Upgrade, dessen Wirkung vom Ort abhängt, hat dort keinen Kontext.
- Eine Stufe hat genau eine Wirkung.
- Die drei Upgrades kosten und dauern dasselbe. Ob die Zahlen tragen, hat noch niemand gespielt.

## Ideen

- Einfache Boni auf Einheiten und Gebäude: Kosten, Trainingsdauer, Einkommen, Panzerung, Schaden an Gebäuden.
- Änderungen an Bewegung oder Sicht.
- Stark bedingte Upgrades mit eigenem Code, zum Beispiel: Drei Circles kämpfen gegen mehr als fünf Triangles.
- Upgrades, die erst in Kombination mit anderen etwas bewirken.
- Offen ist, ob weitere Kampfeinheiten dazukommen. `Army` hat vier feste Felder und `Army.GetStrengthOver` das feste Konter-Dreieck.
- Nicht jedes Upgrade muss auf jeder Karte erforschbar sein: Was ein Hexagon anbietet, steht in `HexagonSettings.ResearchableUpgrades`.

## Woran man es merkt

- Zwei Spieler, die unterschiedlich forschen, spielen spürbar unterschiedlich und nicht nur mit anderen Zahlen im selben Kampf.
