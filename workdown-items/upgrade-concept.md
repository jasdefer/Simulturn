---
id: upgrade-concept
title: Upgrade-Konzept erarbeiten
status: todo
created: 2026-11-13
updated: 2026-11-14
---
## Ziel

Ein Modell für Upgrades, das viele und sehr verschiedene Upgrades trägt, und eine Regel dafür, was ein Spieler über die Upgrades eines Gegners erfährt. Upgrades sollen dem Spiel Vielfalt und Asymmetrie geben; heute ist alles symmetrisch.

## Feststehend

Regeln des Spiels, am 2026-11-14 festgelegt:

- **Dots haben keinen Kampfexponenten.** Dots arbeiten und sind schlechte Kämpfer. Einen Bonus auf den Exponenten bekommen nur die Kampfeinheiten. Ein Upgrade auf den Exponenten der Dots gibt es nicht.
- **Die drei Kampfeinheiten bekommen je ein Upgrade auf ihren Exponenten** (Triangle, Circle, Square). Das sind die ersten Upgrades, die im Spiel etwas bewirken. Sie gehören zu diesem Item und werden nicht auf ein Folge-Item verschoben: Ohne sie kauft die Forschung in einer UI nichts.

## Ideen

- Einfache Boni auf Einheiten und Gebäude.
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

- Keines der beiden Upgrades der Standardregeln bewirkt etwas. Forschung kostet Matter und Zeit ohne Wirkung.
  - Das Dot-Upgrade: `Combat.GetExponent` erhöht den Dot-Exponenten, `Army.GetStrengthOver` rechnet Dots aber linear und liest ihn nie. Die lineare Rechnung passt zur Regel oben, das Upgrade nicht.
  - `UpgradeStartMatter`: In `DefaultSettings` steht `Matter = 0`, und der Wert wird nirgends gelesen.
- `DefaultSettings.FightExponent` hat für Dots den Wert 1, der ebenfalls nie gelesen wird.
- Die Stufen sind nicht kumulativ: Es zählt nur der Bonus der höchsten Stufe. Stufe 2 des Dot-Upgrades gibt 20, nicht 40. Offen ist, ob das so gewollt ist.
- Aus den Verlusten im `FightReport` lässt sich der Kampf nicht nachrechnen, solange die Upgrades der Beteiligten fehlen.
- Welche Upgrades erforschbar sind, hängt an der Karte: `HexagonSettings.ResearchableUpgrades`. `MapGeneratorOptions` bietet als Standard nur das Dot-Upgrade an, und `MapGeneratorTest` prüft genau das.
- Drei Stellen der AI lesen `GameSettings.FightExponent` direkt statt über `Combat`: `CommanderPlayer`, `StateMachinePlayer` und `CompositionOptimizer`. Sie schätzen Kämpfe ohne Upgrades ein, auch die eigenen. Was daraus für die Gegner folgt, behandelt [[ai-opponents-too-weak]].

## Ergebnis

Ein Konzept in diesem Item, die drei Exponent-Upgrades der Kampfeinheiten im Spiel und daraus abgeleitete Items für alles Weitere.
