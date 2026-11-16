---
id: state-evaluation
title: Niemand kann sagen, wer in einem Zustand vorne liegt
status: todo
created: 2026-11-16
depends_on:
- bot-game-diagnosis
---
## Problem

Ein Bot muss zwischen Zügen wählen, und dafür muss er sagen können, welcher Ausgang besser für ihn ist. Das Spiel selbst sagt das erst am Ende: Sieg oder Niederlage. Davor braucht es ein Urteil über einen Zustand: Wie stehe ich da, verglichen mit dem Gegner? Ist eine Expansion mehr wert als die Armee, die ich stattdessen hätte? Lohnt ein Kampf, der mich weniger kostet als ihn?

Ein solches Urteil gewichtet Kennzahlen gegeneinander (siehe [[bot-game-diagnosis]]), und ob die Gewichte stimmen, ist keine Frage der Regeln. Nach Gefühl gesetzt, lässt sich nie sagen, ob das Urteil taugt. Prüfen lässt es sich nur an Spielen: Sagt es in Zug 30 den späteren Sieger häufiger richtig voraus als ein anderes?

Ein Urteil über den Zustand ist nicht dasselbe wie ein guter Zug. Aber jeder Bot, der Züge vergleicht oder durchspielt (siehe [[client-what-if]]), ist höchstens so gut wie sein Urteil.

## Funde

Stand 2026-11-16, vor der Umsetzung neu zu prüfen.

- Das Spiel kennt nur den Ausgang: `Standing.Of` sagt, wer besiegt ist und wer gewonnen hat.
- Der Wert einer Armee hängt vom Gegner ab: Der Kampf vernichtet die schwächere Armee ganz, und die Stärke wächst mit der Größe (`Combat.Fight`). Zwei kleine Armeen sind nicht so viel wert wie eine große.
- Ein fairer Bot bewertet nicht den Zustand, sondern das, was er davon weiß und annimmt (siehe [[opponent-estimate]]).

## Ideen

- Ein Urteil als Wahrscheinlichkeit zu gewinnen, damit es sich an Ergebnissen messen lässt.
- Die Gewichte aus aufgezeichneten Spielen bestimmen statt von Hand.
- Mehrere Urteile nebeneinander zulassen und an denselben Spielen vergleichen.
- Ein einfaches erstes Urteil (zum Beispiel der Wert von allem in Matter) als Maßstab, den ein besseres schlagen muss.
- Eine Anzeige in der UI nach dem Spiel: der Verlauf des Urteils über die Züge.

## Offene Fragen

- Lässt sich ein Zustand überhaupt mit einer Zahl fassen, wenn die Stärke einer Armee von der Aufstellung des Gegners abhängt?
- Wie viel zählt die Zukunft (Einkommen, Forschung) gegen die Gegenwart (Armee)? Das ändert sich vermutlich im Lauf eines Spiels.
- Aus welchen Spielen lernt man die Gewichte, solange es nur schwache Bots gibt? Ein Urteil aus schlechten Spielen beschreibt schlechtes Spiel.
- Gilt ein Urteil für alle Karten und Settings, oder muss es sich anpassen?

## Woran man es merkt

- Für ein Urteil lässt sich an aufgezeichneten Spielen angeben, wie oft es den Sieger zu einem bestimmten Zeitpunkt richtig vorhersagt.
- Ein neues Urteil lässt sich mit einem vorhandenen an denselben Spielen vergleichen.
- Ein Bot mit dem besseren Urteil gewinnt in der Messung häufiger als derselbe Bot mit dem schlechteren.
