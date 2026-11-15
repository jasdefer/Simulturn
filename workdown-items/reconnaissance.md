---
id: reconnaissance
title: Die Zusammensetzung einer gegnerischen Armee erfährt man nur im Kampf
status: todo
created: 2026-11-15
---
## Problem

Die Sicht zeigt von einer gegnerischen Armee nur die Zahl der Einheiten. Welche Typen darin stehen und welche Upgrades auf sie wirken, erfährt ein Spieler nur aus einem Kampf (siehe [[upgrade-concept]]). Der einzige Weg zu diesem Wissen ist, eine Einheit zu opfern. Das ist für den Anfang gewollt. Ob es auf Dauer reicht, ist offen: Der Kampf vernichtet die schwächere Armee ganz, und wer falsch schätzt, verliert alles.

## Funde

Stand 2026-11-15, vor der Umsetzung neu zu prüfen.

- `HexagonObservation.OpponentUnitCounts` hält pro Gegner eine Zahl. Auch auf einem voll sichtbaren Hexagon gibt es nicht mehr.
- Die Sichtweiten `VisibilityRange` und `PartialVisibilityRange` gelten für alle Einheitentypen und alle Spieler gleich. Eine Stelle, die die wirksame Sicht eines Spielers liefert, gibt es noch nicht; `FogOfWar.GetVisibility` liest die Settings direkt.
- `UpgradeRules.ActingOn` liefert bereits die Upgrades, die auf eine Armee wirken. Die Regel steht fest: Wer die Zusammensetzung erkennt, sieht auch diese Upgrades.

## Ideen

- Einheiten mit mehr Sicht.
- Ein Upgrade, mit dem Einheiten die Zusammensetzung benachbarter Armeen erkennen.
- Stark bedingte Aufklärung, zum Beispiel: Eine Einheit sieht die Upgrades von Einheiten, die genau vier Felder entfernt stehen.
- Die Grundsicht bleibt bewusst knapp. Wäre die Zusammensetzung schon ohne Fähigkeit sichtbar, gäbe es nichts mehr zu vergeben.

## Woran man es merkt

- Ein Spieler kann sich entscheiden, in Wissen über den Gegner zu investieren, ohne dafür kämpfen zu müssen.
