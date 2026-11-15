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
- **Start-Upgrades sind offen, Forschung ist verdeckt.** Womit ein Spieler die Partie beginnt, wissen alle von Zug 0 an: `StartUpgrades` steht in den Settings, und die Settings sind Teil jeder Sicht. Was ein Spieler während der Partie erforscht, zeigt erst ein Kampf.
- **Jede Wirkung braucht eigenen Code.** Die Settings legen fest, welches Upgrade welche Wirkung mit welchen Werten hat; was eine Wirkung bedeutet, steht im Code. Dass der Name eines Upgrades und seine Wirkung in den Settings auseinanderlaufen können, ist vertretbar.
- **Die Grundsicht bleibt, wie sie ist.** Die Zusammensetzung aus der Ferne zu erkennen, ist eine Fähigkeit, die später dazukommen kann (siehe [[reconnaissance]]).

## Konzept

**Die Definition eines Upgrades.** `GameSettings.Upgrades` hält pro `Upgrade` eine `UpgradeDefinition`:

- `AffectedUnits`: die Einheitentypen, auf die das Upgrade wirkt („wirkt auf").
- `Levels`: die Stufen, jede mit Kosten, Dauer und Wirkung (`UpgradeLevel`).

Die Wirkung einer Stufe ist ein Fall der Union `UpgradeEffect`. Es gibt zwei Fälle: `FightExponentBonus` und `StartMatterBonus`. Ein einfaches neues Upgrade ist ein Wert im Enum und ein Eintrag in `DefaultSettings`. Eine neue Art von Wirkung ist ein Fall der Union und eine Zeile an der Stelle ihrer Regel.

**Jede Regel holt ihre Werte an einer Stelle.** Die Stelle fragt `UpgradeRules.Effects` nach den Wirkungen eines Spielers und nimmt sich ihre Art heraus.

- Der Kampf rechnet den Exponenten in `Combat.GetExponent`: der Exponent der Settings plus jeder `FightExponentBonus` auf die `AffectedUnits` seines Upgrades.
- Der Startzustand rechnet die Materie in `UpgradeRules.StartMatter`: die Startmaterie der Settings plus jeder `StartMatterBonus`. Nur der `GameState`-Konstruktor fragt dort, mit den Stufen aus `StartUpgrades`.
- Die Bewegung hat `Movement.Range`, noch ohne Upgrades. Die Sicht bekommt ihre Stelle mit dem ersten Upgrade, das sie ändert.

**Die zwei Arten als Muster.** Der Exponent-Bonus wirkt auf Einheiten, zeigt sich im Kampf und lohnt sich zu erforschen. Der Startmaterie-Bonus wirkt auf keine Einheit, zeigt sich in keinem Kampf und zählt nur als Start-Upgrade: Wer ihn während der Partie erforscht, zahlt die Stufe und bekommt nichts. Das Modell verbietet das nicht; ob ein Upgrade erforschbar ist, entscheidet die Karte.

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
| `StartMatter` | nichts | 100 Matter, 2 Züge, 250 Matter mehr zum Start | – |

Der Exponent ohne Upgrade ist 1,2. Die Schritte sind bewusst klein: Der Kampf vernichtet die schwächere Armee ganz, ein unbekanntes Upgrade soll eine knappe Rechnung verschieben und nicht entwerten. Zehn gegen zehn Einheiten desselben Typs: Mit Stufe 1 überleben zwei, mit Stufe 2 drei. Ob die Zahlen tragen, zeigt erst das Spielen.

Generierte Karten bieten als Standard die drei Exponent-Upgrades an. `StartMatter` bietet keine Standardkarte an, und in den Standardregeln startet niemand damit: `StartUpgrades` ist leer, bis es eine Wahl zu Spielbeginn gibt (siehe [[upgrade-draft]]).

## Entfernt

- Das Dot-Upgrade. Es erhöhte einen Exponenten, den der Kampf nie liest.
- Das alte `UpgradeStartMatter` mit dem Wert 0, den niemand las. An seine Stelle tritt `StartMatter` mit einer Wirkung.
- Der Wert 1 für Dots in `DefaultSettings.FightExponent`. Er steht jetzt auf 0 und wird weiter nie gelesen.

## Ideen

Wohin es gehen soll, ohne Vorgabe für die Umsetzung. Das Spiel ist sehr symmetrisch; die Variation soll aus sehr unterschiedlichen Upgrades kommen.

- Ein Spieler wählt einige Upgrades, mit denen er die Partie beginnt ([[upgrade-draft]]).
- Bedingte Wirkungen im Kampf, zum Beispiel: Drei Circles kämpfen gegen mehr als fünf Triangles. Eine solche Bedingung hängt nur an den beiden Armeen, und die kennt `Combat` schon. Der Exponent wird heute einmal pro Spieler gerechnet, eine bedingte Wirkung müsste pro Paarung gerechnet werden.
- Upgrades, die erst in Kombination mit anderen etwas bewirken.
- Eine UI, in der sich die Settings und damit die Upgrades einer Partie ändern lassen.
- Offen bei bedingten Wirkungen: ob ein Fight Report sie immer zeigt, wenn ihre Einheiten im Kampf standen, oder nur, wenn die Bedingung gegriffen hat.

Mehr dazu in [[upgrade-variety]].

## Nicht entschieden

- **Kontext über die Armeen hinaus** (Hexagon, Gelände, Entfernung). `Combat.Fight` kennt nur Armeen und Upgrade-Stufen. Wird ein Upgrade gewünscht, das den Ort braucht, ist `Combatant` die Stelle, an der der Kontext dazukommt.
- **Mehrere Wirkungen pro Stufe.** Eine Stufe hat genau eine Wirkung.
- **Erforschbar oder nur zum Start.** Das Modell unterscheidet das nicht, nur die Karte.
- **Weitere Kampfeinheiten.** `Army` hat vier feste Felder und `Army.GetStrengthOver` das feste Konter-Dreieck.

## Offen bei den AI-Spielern

Drei Stellen der AI lesen `GameSettings.FightExponent` direkt statt über `Combat`: `CommanderPlayer`, `StateMachinePlayer` und `CompositionOptimizer`. Sie schätzen Kämpfe ohne Upgrades ein, auch die eigenen, und merken sich nicht, was ein Fight Report über den Gegner zeigt. Seit die Upgrades wirken, ist das ein echter Nachteil. Behandelt in [[ai-opponents-too-weak]].

## Ergebnis

Das Konzept oben, die drei Exponent-Upgrades und das Startmaterie-Upgrade im Spiel und die Folge-Items [[upgrade-draft]], [[reconnaissance]] und [[upgrade-variety]].
