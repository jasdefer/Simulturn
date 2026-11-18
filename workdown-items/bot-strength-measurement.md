---
id: bot-strength-measurement
title: Die Stärke eines Bots lässt sich nicht verlässlich messen
status: done
created: 2026-11-16
updated: 2026-11-18
completed: 2026-11-18
---
## Problem

Bevor ein Bot besser werden kann, muss sich sagen lassen, ob er besser geworden ist. Dafür müssen Bots viele tausend Spiele gegeneinander spielen, und das Ergebnis muss stimmen, sich wiederholen lassen und über die Zeit vergleichbar bleiben. Ohne dieses Messinstrument ist jede Aussage über einen Bot geraten.

Die vorhandenen Spieler, der Runner und die Arena sind dafür keine Grundlage mehr. Sie sind vor `Combat.Fight`, `Movement`, den Upgrades und den Regeln für Clients entstanden und rechnen vieles selbst (siehe [[ai-opponents-too-weak]]). Am 2026-11-16 festgelegt: Der vorhandene Bot-Code wird entfernt und die Struktur neu aufgebaut. Das Entfernen gehört zu diesem Item.

Die neue Struktur muss mehrere Bot-Implementierungen nebeneinander zulassen, auch solche, die es noch nicht gibt: Bots nach festen Regeln, Bots, die Züge durchrechnen, und Varianten desselben Bots mit anderen Parametern.

## Festgelegt

Am 2026-11-16:

- **Faire Bots.** Ein Bot bekommt, was ein menschlicher Spieler bekommt: seine Sicht. Er sieht nie den ganzen Zustand.
- **Zuerst zwei Spieler.** Die Messung wird für Spiele zu zweit gebaut. Mehr Spieler kommen später; dort gibt es Platzierungen statt Sieg und Niederlage, und ein Dritter kann entscheiden, wer gewinnt.
- **Neuanfang.** Nichts aus dem vorhandenen Bot-Code muss erhalten bleiben.

## Funde

Stand 2026-11-16, vor der Umsetzung neu zu prüfen.

- Der Bot-Code liegt in `Source/Simulturn.AI` (Spieler, `GameRunner`, `Arena`, Auswertung der Settings), `Source/Simulturn.Runner` (Kommandozeile für die Arena) und `Test/Simulturn.AI.Test`.
- `EngineInvariantsTest` liegt in `Simulturn.AI.Test` und spielt Zufallsspiele mit `RandomPlayer`. Das ist Ebene 3 der Teststrategie (`docs/test-strategy.md`) und prüft die Engine, nicht die Bots. Mit dem Bot-Code darf dieser Schutz nicht verschwinden.
- `docs/test-strategy.md` (Ebenen 3 und 4) und `AGENTS.md` beschreiben die vorhandenen Projekte und müssen mitziehen.
- Die Spielregeln kennen kein Zuglimit. Nur der Runner bricht ab und wertet das als Unentschieden. Zwei Bots, die sich nie angreifen, spielen sonst endlos.
- Die Engine führt ein Spiel bereits Zug für Zug (`Game.Start`, `Game.PlayTurn`) und schneidet die Sicht eines Spielers (`FogOfWar.GetPlayerGameState`).

## Ideen

Keine Vorgaben, nur Stoff zum Nachdenken bei der Umsetzung.

- Ein Spiel als reine Funktion: Aus Settings, Bots und Seed folgt immer dasselbe Spiel. Dann lässt sich jedes Spiel aus wenigen Angaben wieder abspielen.
- Ein Bot als Beschreibung aus reinen Daten (Name, Parameter, Seed), damit sie im Ergebnis stehen kann und sich Parameter später automatisch variieren lassen.
- Jede Paarung auf jeder Karte mit getauschten Sitzen und denselben Seeds, damit Karte und Startplatz das Ergebnis nicht verzerren.
- Das Ergebnis als Tabelle jeder Paarung statt als eine Stärkezahl. Bei simultanen Zügen kann A gegen B gewinnen, B gegen C und C gegen A; eine einzelne Zahl versteckt das.
- Siegquoten mit einer Angabe, wie sicher sie sind. 55 % nach 40 Spielen sagt nichts.
- Ein fester Pool eingefrorener Bots als Maßstab, damit „besser" nicht nur „schlägt den Vorgänger" heißt.
- Ungültige Befehle eines Bots als Messwert statt als Abbruch.
- Ein Rechenbudget pro Zug, sobald Bots Züge durchrechnen, sonst gewinnt, wer länger rechnen darf. Ein Budget in Schritten statt in Zeit hält die Spiele wiederholbar.
- Spiele sind unabhängig voneinander und können über alle Kerne laufen.
- Ergebnisse mit dem Stand der Engine speichern, damit Läufe vor und nach einer Regeländerung nicht verwechselt werden.
- Ein kleiner, schneller Lauf als Test, der große Lauf von Hand.

## Offene Fragen

- Wie viele Spiele pro Sekunde sind nötig, und wie viele schafft die Engine heute?
- Was passiert mit einem Bot, der ungültige Befehle gibt oder sein Budget überzieht?
- Wie wird ein Spiel gewertet, das das Zuglimit erreicht: als Unentschieden oder nach einem Maß? Ein Maß verleitet Bots dazu, auf das Maß statt auf den Sieg zu spielen.
- Welche Karten gehören zum festen Satz einer Messung?
- Wo liegen die Ergebnisse, und gehören sie ins Repository?
- Welche Bots gibt es am ersten Tag? Zum Prüfen des Messinstruments genügen wenige einfache.

## Woran man es merkt

- Der alte Bot-Code ist entfernt, und die Zufallsspiele gegen die Invarianten der Engine laufen weiter.
- Ein neuer Bot lässt sich hinzufügen, ohne den Code der Messung zu ändern.
- Derselbe Lauf mit denselben Angaben liefert dasselbe Ergebnis.
- Viele tausend Spiele laufen in einer Zeit, in der man auf das Ergebnis warten mag.
- Das Ergebnis sagt für jede Paarung, wer gewinnt und wie sicher das ist, getrennt nach Karte und Sitz.
- Zwei Läufe von verschiedenen Tagen lassen sich vergleichen.

## Umsetzung

Stand 2026-11-18. Die Grundlage ist mit PR 25 auf `develop`; der erste echte Bot entsteht auf dem Branch `feature/random-bot`.

- **Abriss.** `Simulturn.AI`, `Simulturn.Runner` und `Simulturn.AI.Test` sind entfernt. Der Zufallsgenerator für gültige Befehle liegt als Testwerkzeug in `Simulturn.Test.Common` (`RandomCommands`), `EngineInvariantsTest` in `Simulturn.Core.Test`.
- **`Source/Simulturn.Bots`** (Lib): der Vertrag `IBot` (Sicht rein, Befehle raus), der `BotCatalog` (Name und Fabrik aus einem Seed) und `Match.Play`, das ein Spiel zu Ende spielt. Am ersten Tag gibt es nur den `IdleBot`.
- **`Source/Simulturn.Lab`** (Console-App): alles, was kein Test ist. Die erste Studie ist die Arena (`arena`), ihre Profile stehen in `appsettings.json`, Werte lassen sich auf der Kommandozeile ersetzen. Weitere Studien, etwa zum Balancing, bekommen einen eigenen Ordner.
- **`Test/Simulturn.Bots.Test`** und **`Test/Simulturn.Lab.Test`**: Ebene 4 der Teststrategie, mit geskripteten Bots. Das erste prüft `Match`, das zweite die Arena.

Entschieden:

- **Zuglimit:** Ein Spiel endet als Unentschieden nach 1000 Zügen in Folge, in denen nichts passiert ist (`Stall`), und spätestens nach 5000 Zügen (`TurnLimit`). Kein Maß für den Stand am Limit.
- **Ungültige Befehle:** Sie sind ein Fehler des Bots. Der Lauf bricht ab und nennt Paarung, Karte, Seed und Sitz.
- **Karten:** Die Typen `Open`, `HomeOnly`, `Expansions` und `Guarded` des Generators, je Typ die Seeds 1 bis n. Die Engine und die Karten würfeln nicht; die Streuung eines Laufs kommt aus den Karten-Seeds und aus Bots, die würfeln.
- **Ergebnis:** Eine Zeile pro Paarung, Kartentyp und Sitz mit Siegen, Niederlagen, Unentschieden und der Siegquote samt 95-%-Spanne (Wilson). Der Lauf schreibt es mit seinen Angaben und der Version der Engine (samt Commit) als JSON nach `measurements/`; der Ordner liegt nicht in Git.
- **Durchsatz:** Auf 20 Kernen rund 50.000 bis 75.000 Züge pro Sekunde mit Zufallsbefehlen, 160.000 bis 230.000 leere Züge. Ein entschiedenes Spiel kostet wenige Millisekunden.

Bewusst nicht gebaut: ein Rechenbudget pro Zug, ein Pool eingefrorener Bots, das Fortsetzen und Zusammenführen von Läufen. Sie kommen, wenn ein Bot oder ein Lauf sie braucht.

Der erste echte Bot ist der Zufallsbot (`RandomBot`, im Katalog `random`), gebaut auf dem Branch `feature/random-bot`. Er ist der erste Client von `Orders.Possible` aus [[client-possible-orders]] und die erste Probe für die Arena.

### Der Zufallsbot

Stand 2026-11-18. Ein Bot, der gleichverteilt aus allem wählt, was möglich ist, sagt nichts aus: Er zerstreut seine Armeen und verliert gegen jeden. Der Zufallsbot lässt offensichtlich Unsinniges aus und würfelt unter dem Rest. Einen zweiten, ganz dummen Zufallsbot gibt es nicht; den Beleg, dass die Arena Unterschiede misst, liefert der nächste Bot.

- **Ausgeben.** Er gibt jeden Zug alles aus. Zurück behält er nur den Preis einer Plane, solange es ein Feld mit Matter ohne eigene Plane gibt; ohne diese Reserve hat er nichts mehr, wenn sein Feld leer ist. Dots sind von der Reserve ausgenommen.
- **Dots.** Ein Feld bekommt so viele Dots, wie dort abbauen können, aber nur solange die Matter des Felds jeden Dot viermal bezahlt (`DotPayback`), dazu die Dots, die Bau und Forschung binden. Ein Dot mehr wird trainiert, solange es ein freies Feld mit Matter oder Upgrades gibt; er läuft dorthin und baut eine Plane oder forscht. Es ist immer nur ein Dot unterwegs.
- **Gebäude.** Eine Axis, sobald der freie Space nicht mehr für das reicht, was die Gebäude auf einmal trainieren. Ein weiteres Trainingsgebäude nur, wenn das Einkommen die vorhandenen übersteigt oder die Matter sie auf einmal füllt und das neue samt erster Einheit bezahlt. Die Trainingsgebäude stehen auf einem Feld, damit die Einheiten eine Armee sind.
- **Würfel.** Welche Forschung und welche Kampfeinheit er kauft, welches Trainingsgebäude er baut, wie lange eine Armee Wache hält und wohin sie dann zieht.
- **Armee.** Die Kampfeinheiten eines Felds ziehen gemeinsam. Bei ihren Trainingsgebäuden sammelt sich die Armee und zieht nur los, wenn sie ein fremdes Feld nehmen kann: Sie hat mehr Einheiten, als sie dort zuletzt gesehen hat, und zerstört mindestens ein bekanntes Gebäude (`Destruction.Of`). Unterwegs bewacht sie auch eigene Felder und freie Felder mit Matter oder Upgrades. Wer kein Einkommen mehr hat und nichts mehr trainiert, greift an, was auch immer dort steht.
- Upkeep ([[upkeep]]) beachtet er nie.

Was der erste Lauf gezeigt hat:

- **Seeds der Bots.** Die Arena gab den beiden Bots eines Spiels die Seeds n und n + 1.000.000. `System.Random` liefert für solche Seeds erste Würfe, die auseinander folgen: Der zweite Bot baute als erstes fast immer das Gebäude nach dem des ersten und gewann im Spiegelspiel auf `HomeOnly` 263 von 400 Spielen, der erste 70. Die Seeds kommen jetzt aus `ArenaStudy.BotSeed`, das Spiegelspiel ist ausgeglichen.
- **Gegen `idle`** (100 Karten-Seeds je Typ, beide Sitze): 799 Siege in 800 Spielen, im Schnitt nach 50 bis 120 Zügen.
- **Spiegelspiel:** entschieden auf `Expansions` (8 % Unentschieden), `HomeOnly` (20 %) und `Guarded` (29 %). Auf `Open` enden rund 90 % unentschieden: Die Felder dort sind klein, beide bauen die ganze Karte ab, und am Ende hat keiner genug Armee für ein Gebäude. Das ist eine Aussage über die Karte und den Bot, kein Fehler der Messung.
- **Die Arena misst.** `DotPayback` 2 gegen 4: die 4 gewinnt auf `Expansions` 46 zu 10 und auf `Open` 16 zu 0. Noch höhere Werte gewinnen auf `Expansions` weiter, weil der Bot dann kaum Dots trainiert und früh angreift; der Standard bleibt bei 4. Immer bis zur Abbaugrenze des Felds zu trainieren (`DotPayback` 0) verliert gegen die 4 auf `Open` und `Expansions` 0 zu 114 und 2 zu 114 (60 Karten-Seeds, beide Sitze): Auf einem kleinen Feld kosten zwölf Dots mehr, als sie noch abbauen.
- **Durchsatz.** 2400 Spiele in 32 Sekunden auf 20 Kernen, obwohl der Bot `Orders.Possible` pro Zug viele Male fragt.

Offen für spätere Items:

- Die Unentschieden auf `Open`: Erst ein Bot mit Plan zeigt, ob sie am Zufallsbot oder an der Karte liegen.
- Die Armee zählt Einheiten und kennt keine Zusammensetzung. Eine Kampfvorhersage mit `Combat.Fight` gehört zum nächsten Bot ([[opponent-estimate]]).
