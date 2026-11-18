---
id: bot-game-diagnosis
title: Man sieht nicht, warum ein Bot verliert
status: todo
created: 2026-11-16
updated: 2026-11-18
depends_on:
- bot-strength-measurement
---
## Problem

Eine Siegquote sagt, dass ein Bot verliert, aber nicht, woran. Im früheren Test gegen die AI war sie sehr schlecht, und was genau schlecht war, ist nicht festgehalten (siehe [[ai-opponents-too-weak]]). Wer einen Bot verbessern will, muss sehen können, was in seinen Spielen passiert: Baut er zu langsam auf, wählt er die falschen Einheiten, verliert er Kämpfe, die er hätte meiden können, greift er zu spät an?

Dafür fehlen zwei Dinge. Es gibt keine Kennzahlen, die den Stand eines Spielers in einem Zug beschreiben und sich über ein Spiel oder über tausend Spiele verfolgen lassen. Und es gibt keinen Weg, ein einzelnes Spiel aus einer Messung herauszugreifen und Zug für Zug anzusehen.

Kennzahlen sind Tatsachen, kein Urteil: Was eine Armee gekostet hat, was ein Spieler einnimmt, was er besitzt. Wer vorne liegt, ist eine andere Frage (siehe [[state-evaluation]]). Auch eine UI braucht solche Kennzahlen, für eine Anzeige des Spielstands oder eine Verlaufskurve nach dem Spiel.

## Funde

Stand 2026-11-16, vor der Umsetzung neu zu prüfen.

- Der `TurnReport` hält, was in einem Zug passiert ist: Einkommen, abgeschlossene und abgebrochene Aufträge, Bewegungen, Kämpfe, Zerstörungen, Niederlagen. Über ein Spiel hinweg wertet ihn niemand aus.
- Die Preise liefert `Costs`, das Einkommen `Harvest`. Der Wert einer Armee oder eines Spielers in Matter ist nirgends zusammengefasst.
- Ein Zustand folgt aus den Settings und den Befehlen aller Züge davor. Ein Spiel lässt sich also aus wenig wiederherstellen (siehe [[game-persistence]]).

## Ideen

- Kennzahlen je Spieler und Zug: Wert der Armee, Einkommen, Gebäude, laufende Aufträge, Upgrades, ungenutzte Matter, verbleibende Matter auf den eigenen Hexagonen.
- Kennzahlen je Spiel: Zeitpunkt der ersten Expansion, des ersten Kampfs, Bilanz der Kämpfe in Matter, Anteil ungenutzter Züge.
- Kennzahlen, die nur aus Regeln folgen, im Core, damit eine UI sie ebenso nutzen kann.
- Ein Spiel aus einer Messung wieder abspielen und als Text oder später in der UI ansehen.
- Aus einer Messung gezielt Spiele herausgreifen: die klarsten Niederlagen, die längsten Spiele, die mit ungültigen Befehlen.

## Aus dem ersten Lauf des Zufallsbots

Stand 2026-11-18, aus dem Gespräch nach [[bot-strength-measurement]]. Keine Vorgaben. Das Item ist als Nächstes dran, vor einem weiteren Bot und vor [[state-evaluation]].

Jede Ursache im ersten Lauf wurde von Hand mit einem Wegwerfskript gesucht, das alle paar Züge Matter, Space, Armeen und Gebäude druckt: die Pleite auf `Guarded` (alles ausgegeben, kein Geld für die Plane), verstreute Armeen, die nie ein Gebäude zerstören konnten, das Patt zweier Armeen auf `HomeOnly`, die Erschöpfung auf `Open`. Mit Kennzahlen pro Zug wäre jede davon eine Tabelle gewesen.

Kandidaten, getrennt nach Kurve und Zahl:

- Je Zug und Spieler: Einkommen, Matter auf der Hand, Wert von Armee und Gebäuden in Matter, Zahl der Dots, Felder mit Plane, freier Space, Zahl der Armee-Stapel und Anteil des größten, bewegte Einheiten, gesehene Hexagone.
- Je Spiel: insgesamt abgebaute Matter, in Kämpfen verlorene und vernichtete Matter, Zug der ersten Expansion, der ersten Kampfeinheit und des ersten Angriffs, Zahl der Dots, die allein unterwegs waren (Expansion, Aufklärung).
- Der Grund des Spielendes genauer als `MatchEnd`: Ein Spiel am Zuglimit, in dem die ganze Karte abgebaut ist und keiner mehr ein Gebäude zerstören kann, ist etwas anderes als eines, in dem beide noch wachsen.
- Die Arena zeigt je Paarung Mittelwerte getrennt nach Sieger und Verlierer und schreibt die Kurven einzelner Spiele auf Wunsch mit.

Wie andere Spiele es zeigen (aus dem Gedächtnis, nicht nachgeschlagen): StarCraft 2 Kurven für Einkommen, Arbeiter, Armeewert und ungenutzte Rohstoffe, dazu eine Zahl, wie gut das Einkommen ausgegeben wird; Age of Empires eine Zeitleiste und Punkte nach Militär, Wirtschaft und Technik; Dota 2 den Vorsprung an Gold und Erfahrung über die Zeit. Gemeinsam ist die Trennung von Kurven über die Zeit und wenigen Zahlen pro Spiel.

## Offene Fragen

- Welche Kennzahlen erklären eine Niederlage wirklich? Das zeigt sich erst an Spielen.
- Kennzahlen aus dem vollen Zustand oder aus der Sicht eines Spielers? Die Diagnose darf alles sehen, eine UI im laufenden Spiel nicht.
- Wie viel wird je Spiel gespeichert, wenn es viele tausend Spiele sind?
- Wie sieht man ein Spiel an, solange es keine UI gibt?

## Woran man es merkt

- Nach einer Messung lässt sich sagen, worin sich Sieger und Verlierer einer Paarung unterscheiden.
- Jedes Spiel einer Messung lässt sich einzeln wieder abspielen und ansehen.
- Die offene Frage „Was war im früheren Test schlecht?" lässt sich für einen neuen Bot beantworten.
