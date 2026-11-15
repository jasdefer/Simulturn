---
id: upgrade-concept
title: Upgrade-Konzept erarbeiten
status: done
created: 2026-11-13
updated: 2026-11-15
completed: 2026-11-15
---
## Ziel

Ein Modell für Upgrades, das viele und sehr verschiedene Upgrades trägt, und eine Regel dafür, was ein Spieler über die Upgrades eines Gegners erfährt. Upgrades sollen dem Spiel Vielfalt und Asymmetrie geben; bisher war alles symmetrisch.

## Regeln des Spiels

Am 2026-11-14 und 2026-11-15 festgelegt:

- **Dots haben keinen Kampfexponenten.** Dots arbeiten und sind schlechte Kämpfer, sie zählen im Kampf einzeln. Ein Upgrade auf den Exponenten der Dots gibt es nicht.
- **Die drei Kampfeinheiten bekommen je ein Upgrade auf ihren Exponenten** (Triangle, Circle, Square).
- **Upgrades gehören dem Spieler**, nicht der einzelnen Einheit. Ein Upgrade wirkt auf alle Einheiten der Typen, die es nennt.
- **Die Werte einer Stufe sind absolut.** Stufe 2 nennt die ganze Wirkung, die Stufen addieren sich nicht.
- **Upgrades eines Gegners sieht, wer die Zusammensetzung einer Armee erkennt.** Gezeigt werden die Upgrades, die auf die Typen dieser Armee wirken. Heute erkennt nur ein Kampf die Zusammensetzung: Die Sicht zeigt von einer gegnerischen Armee nur die Zahl der Einheiten und damit keine Upgrades. Wer wissen will, was der Gegner hat, schickt eine Einheit in dessen Armee. Ein Upgrade kann den Gegner im ersten Kampf überraschen.
- **Die Grundsicht bleibt, wie sie ist.** Die Zusammensetzung aus der Ferne zu erkennen, ist eine Fähigkeit, die später dazukommen kann (siehe [[reconnaissance]]).

## Konzept

**Die Definition eines Upgrades.** `GameSettings.Upgrades` hält pro `Upgrade` eine `UpgradeDefinition`:

- `AffectedUnits`: die Einheitentypen, auf die das Upgrade wirkt („wirkt auf").
- `Levels`: die Stufen, jede mit Kosten, Dauer und Wirkung (`UpgradeLevel`).

Die Wirkung einer Stufe ist ein Fall der Union `UpgradeEffect`. Heute gibt es einen Fall, `FightExponentBonus`. Ein einfaches neues Upgrade ist ein Wert im Enum und ein Eintrag in `DefaultSettings`. Eine neue Art von Wirkung ist ein Fall der Union und eine Zeile an der Stelle ihrer Regel.

**Jede Regel holt ihre Werte an einer Stelle.** Der Kampf rechnet den Exponenten in `Combat.GetExponent`: der Exponent der Settings plus der Bonus jedes Upgrades auf seine `AffectedUnits`. Die Bewegung hat `Movement.Range`. Die Sicht bekommt ihre Stelle mit dem ersten Upgrade, das sie ändert.

**„Wirkt auf" ist die einzige Angabe.** Eine einfache Wirkung nennt selbst keine Einheiten, sie liest `AffectedUnits`; ein Widerspruch ist nicht möglich. Bei einer Wirkung mit eigenem Code wäre die Angabe von Hand gepflegt. Ein allgemeiner Test sichert sie für jedes Upgrade der Regeln ab: Ein Upgrade ändert nichts an einem Kampf, in dem keine seiner Einheiten steht (`Fight_UpgradeWithoutItsUnitsInTheArmy_ChangesNothing`).

**Was eine Armee über die Upgrades verrät.** `UpgradeRules.ActingOn` liefert die Upgrades eines Spielers, die auf eine Armee wirken. `Combat.Fight` schreibt sie für jeden Beteiligten ins `FightReport`. Ein Kampf lässt sich damit allein aus seinem Report nachrechnen; die Invarianten prüfen das nach jedem Zug. Wer später eine Zusammensetzung zeigt, ruft dieselbe Funktion auf.

**Was sich ein Client merkt.** Die Engine merkt sich nicht, was ein Spieler über einen Gegner weiß. Ein Client hält pro Gegner die zuletzt gesehenen Stufen mit dem Zug, wie eine `HexagonMemory`, und übergibt sie einer Vorschau als `Combatant.UpgradeLevels`. Das Wissen kann veraltet sein.

**Die nächste Stufe.** `GameSettings.NextUpgradeLevel` ist die einzige Stelle, die die Stufe nach den abgeschlossenen liest. Validierung, Zugauflösung und die AI-Spieler fragen dort.

## Die Upgrades der Standardregeln

| Upgrade | wirkt auf | Stufe 1 | Stufe 2 |
|---|---|---|---|
| `TriangleExponent` | Triangle | 150 Matter, 3 Züge, Exponent 1,3 | 175 Matter, 4 Züge, Exponent 1,4 |
| `CircleExponent` | Circle | wie oben | wie oben |
| `SquareExponent` | Square | wie oben | wie oben |

Der Exponent ohne Upgrade ist 1,2. Die Schritte sind bewusst klein: Der Kampf vernichtet die schwächere Armee ganz, ein unbekanntes Upgrade soll eine knappe Rechnung verschieben und nicht entwerten. Zehn gegen zehn Einheiten desselben Typs: Mit Stufe 1 überleben zwei, mit Stufe 2 drei. Ob die Zahlen tragen, zeigt erst das Spielen.

Generierte Karten bieten als Standard alle drei Upgrades an.

## Entfernt

- Das Dot-Upgrade. Es erhöhte einen Exponenten, den der Kampf nie liest.
- `UpgradeStartMatter`. Der Wert wurde nirgends gelesen. Startmaterie ergibt nur als Wahl zu Spielbeginn Sinn, siehe [[upgrade-draft]].
- Der Wert 1 für Dots in `DefaultSettings.FightExponent`. Er steht jetzt auf 0 und wird weiter nie gelesen.

## Nicht entschieden

- **Kontext für bedingte Kampf-Upgrades** (Hexagon, Gegner). `Combat.Fight` kennt weiter nur Armeen und Upgrade-Stufen. Wird ein solches Upgrade gewünscht, ist `Combatant` die Stelle, an der der Kontext dazukommt. Siehe [[upgrade-variety]].
- **Weitere Kampfeinheiten.** `Army` hat vier feste Felder und `Army.GetStrengthOver` das feste Konter-Dreieck.

## Offen bei den AI-Spielern

Drei Stellen der AI lesen `GameSettings.FightExponent` direkt statt über `Combat`: `CommanderPlayer`, `StateMachinePlayer` und `CompositionOptimizer`. Sie schätzen Kämpfe ohne Upgrades ein, auch die eigenen, und merken sich nicht, was ein Fight Report über den Gegner zeigt. Seit die Upgrades wirken, ist das ein echter Nachteil. Behandelt in [[ai-opponents-too-weak]].

## Ergebnis

Das Konzept oben, die drei Exponent-Upgrades im Spiel und die Folge-Items [[upgrade-draft]], [[reconnaissance]] und [[upgrade-variety]].
