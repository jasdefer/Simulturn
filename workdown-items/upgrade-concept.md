---
id: upgrade-concept
title: Upgrade-Konzept erarbeiten
status: todo
created: 2026-11-13
---
## Ziel

Ein Modell für Upgrades, das viele und sehr verschiedene Upgrades trägt, und eine Regel dafür, was ein Spieler über die Upgrades eines Gegners erfährt. Upgrades sollen dem Spiel Vielfalt und Asymmetrie geben; heute ist alles symmetrisch.

## Ideen

- Einfache Boni auf Einheiten (heute: Dot-Upgrade auf den Exponenten der Dots) und Gebäude.
- Änderungen an Sicht oder Bewegung.
- Stark bedingte Upgrades, zum Beispiel: eine Einheit sieht die Upgrades von Einheiten, die genau vier Felder entfernt stehen.
- Upgrades, die schon zu Beginn des Spiels gewählt werden (Draft). `GameSettings.StartUpgrades` ist bereits pro Spieler.
- Offen ist, ob weitere Kampfeinheiten dazukommen. `Army` hat vier feste Felder und `Army.GetStrengthOver` das feste Konter-Dreieck.

## Vorentscheidungen

Beim Item [[combat-forecast-core]] besprochen, hier zu bestätigen oder zu verwerfen:

- **Upgrades gehören dem Spieler**, nicht der einzelnen Einheit. Ein Upgrade wirkt auf bestimmte Einheitentypen oder Gebäude.
- **Jede Regel holt ihre Werte an einer Stelle.** Der Kampf wendet Upgrades in `Combat` an. Sicht und Bewegung bekommen je eine Stelle, die die wirksamen Werte eines Spielers liefert. Ob dahinter ein Datenbonus oder eigener Code steckt, ändert die Regel nicht.
- **Eine Angabe „wirkt auf" pro Upgrade.** Jedes Upgrade nennt ausdrücklich die Einheitentypen und Gebäude, auf die es wirkt. Bei einfachen Upgrades liest die Wirkung diese Angabe (Bonus von 20 auf die genannten Typen), ein Widerspruch ist nicht möglich. Bei Upgrades mit eigenem Code ist die Angabe von Hand gepflegt. Ein allgemeiner Test sichert sie ab: Ein Upgrade ändert nichts an Kämpfen, in denen keine seiner genannten Einheiten steht.
- **Wissen über gegnerische Upgrades kommt aus dem Kampf.** Die Sicht zeigt von einer gegnerischen Armee nur die Einheitenzahl. Ein Upgrade pro gesehenem Typ zu zeigen würde die Zusammensetzung verraten. Ein Kampf legt die Zusammensetzung ohnehin offen; das `FightReport` zeigt deshalb auch die Upgrades des Gegners, die auf die Typen im Kampf wirken. Der Client merkt sich das pro Gegner mit dem Zug, wie eine `HexagonMemory`. Ein Upgrade kann den Gegner im ersten Kampf überraschen.

## Zu klären

- Die Form einer Upgrade-Definition: Stufen (Kosten, Dauer), „wirkt auf", Wirkung. Heute ist jedes Upgrade ein Enum-Wert, ein eigener Struct und eine Zeile Code.
- Wie verschiedene Arten von Wirkung nebeneinander stehen (Exponent, Startmaterie, Sicht, Bewegung, eigener Code).
- Ob bedingte Kampf-Upgrades einen Kontext brauchen (Hexagon, Gegner). `Combat.Fight` kennt heute nur Armeen und Upgrade-Stufen.
- Ob die Offenlegung im Kampf reicht oder es weitere Wege gibt (Aufklärung als eigenes Upgrade).

## Funde im heutigen Code

- Das Dot-Upgrade wirkt im Kampf nicht: `Combat.GetExponent` erhöht den Dot-Exponenten, `Army.GetStrengthOver` rechnet Dots aber linear und liest ihn nie.
- Die Stufen sind nicht kumulativ: Es zählt nur der Bonus der höchsten Stufe. Stufe 2 des Dot-Upgrades gibt 20, nicht 40.
- Aus den Verlusten im `FightReport` lässt sich der Kampf nicht nachrechnen, solange die Upgrades der Beteiligten fehlen.

## Ergebnis

Ein Konzept in diesem Item und daraus abgeleitete Items für die Umsetzung.
