---
id: upgrade-variety
title: Es gibt nur drei gleichartige Upgrades
status: todo
created: 2026-11-15
---
## Problem

Upgrades sollen dem Spiel Vielfalt und Asymmetrie geben. Mit [[upgrade-concept]] gibt es drei Upgrades, und alle tun dasselbe: Sie erhöhen den Kampfexponenten einer Kampfeinheit. Wer forscht, entscheidet nur, welche Einheit stärker wird. Das Modell trägt verschiedene Arten von Wirkung, genutzt wird eine.

## Funde

Stand 2026-11-15, vor der Umsetzung neu zu prüfen.

- Die Union `UpgradeEffect` hat einen Fall, `FightExponentBonus`.
- `UpgradeDefinition.AffectedUnits` nennt nur Einheitentypen. Ein Upgrade auf Gebäude hat noch keine Angabe „wirkt auf".
- `Movement.Range` ist die Stelle für die wirksame Reichweite, bekommt aber die Upgrades eines Spielers noch nicht.
- Für die Sicht gibt es keine solche Stelle (siehe [[reconnaissance]]).
- `Combat.Fight` kennt nur Armeen und Upgrade-Stufen. Ein Upgrade, dessen Wirkung vom Ort oder vom Gegner abhängt, hat dort keinen Kontext.
- Die drei Upgrades kosten und dauern dasselbe. Ob die Zahlen tragen, hat noch niemand gespielt.

## Ideen

- Einfache Boni auf Einheiten und Gebäude: Kosten, Trainingsdauer, Einkommen, Panzerung, Schaden an Gebäuden.
- Änderungen an Bewegung oder Sicht.
- Stark bedingte Upgrades mit eigenem Code.
- Offen ist, ob weitere Kampfeinheiten dazukommen. `Army` hat vier feste Felder und `Army.GetStrengthOver` das feste Konter-Dreieck.
- Nicht jedes Upgrade muss auf jeder Karte erforschbar sein: Was ein Hexagon anbietet, steht in `HexagonSettings.ResearchableUpgrades`.

## Woran man es merkt

- Zwei Spieler, die unterschiedlich forschen, spielen spürbar unterschiedlich und nicht nur mit anderen Zahlen im selben Kampf.
