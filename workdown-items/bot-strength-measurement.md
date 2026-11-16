---
id: bot-strength-measurement
title: Die Stärke eines Bots lässt sich nicht verlässlich messen
status: todo
created: 2026-11-16
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
