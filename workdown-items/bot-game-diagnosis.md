---
id: bot-game-diagnosis
title: Man sieht nicht, warum ein Bot verliert
status: in_progress
created: 2026-11-16
updated: 2026-11-19
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

Was sich messen ließe, als Stoff zum Nachdenken. Welche Kennzahlen es werden, zeigt erst die Umsetzung:

- Je Zug und Spieler: Einkommen, Matter auf der Hand, Wert von Armee und Gebäuden in Matter, Zahl der Dots, Felder mit Plane, freier Space, Zahl der Armee-Stapel und Anteil des größten, bewegte Einheiten, gesehene Hexagone.
- Je Spiel: insgesamt abgebaute Matter, in Kämpfen verlorene und vernichtete Matter, Zug der ersten Expansion, der ersten Kampfeinheit und des ersten Angriffs, Zahl der Dots, die allein unterwegs waren (Expansion, Aufklärung).
- Warum ein Spiel ohne Sieger endet: Ein Spiel am Zuglimit, in dem die ganze Karte abgebaut ist und keiner mehr ein Gebäude zerstören kann, ist etwas anderes als eines, in dem beide noch wachsen. Heute sieht man den Unterschied nicht.
- Ein Vergleich von Siegern und Verlierern einer Paarung über viele Spiele, und die Kurven eines einzelnen Spiels zum Ansehen. Wo das entsteht und in welcher Form, ist offen.

Wie andere Spiele es zeigen (aus dem Gedächtnis, nicht nachgeschlagen): StarCraft 2 Kurven für Einkommen, Arbeiter, Armeewert und ungenutzte Rohstoffe, dazu eine Zahl, wie gut das Einkommen ausgegeben wird; Age of Empires eine Zeitleiste und Punkte nach Militär, Wirtschaft und Technik; Dota 2 den Vorsprung an Gold und Erfahrung über die Zeit. Gemeinsam ist die Trennung von Kurven über die Zeit und wenigen Zahlen pro Spiel.

## Richtung

Stand 2026-11-18, aus dem Gespräch zum Start der Umsetzung.

Die Kennzahlen dienen einem Schritt in der Schleife der Bot-Entwicklung: Die Arena sagt, dass ein Bot gegen einen anderen verliert. Die Kennzahlen sagen, worin sich Sieger und Verlierer der Paarung unterscheiden. Dann greift man ein typisches Spiel heraus, sieht es an, ändert den Bot und misst neu. Eine Anzeige in der UI ist ein Nebennutzen, kein Ziel dieses Items.

- Eine Matter-Bilanz statt einer Liste von Kennzahlen: Jede Matter hat eine Herkunft und einen Verbleib. Wie viel hat ein Spieler eingenommen, wofür hat er es ausgegeben, und was ist daraus geworden (lebt noch, im Kampf gefallen, zerstört, abgebrochen, noch auf der Hand)? Die Posten ergeben zusammen das Eingenommene. So fehlt nichts, nichts ist doppelt, und eine falsche Berechnung fällt auf.
- Eine Definition, zwei Auflösungen: je Spiel die Bilanz am Ende, eine Zeile je Spieler, zum Vergleich über viele Spiele. Je Zug dieselben Posten als Verlauf, nur für ein einzelnes Spiel.
- Die Bilanz zeigt, wo eine Niederlage entsteht, das einzelne Spiel zeigt, warum. Was keinen Wert in Matter hat (Stellung, Aufklärung, verstreute Armeen), gehört nicht in die Bilanz.
- Eine Zahl außerhalb der Bilanz kommt erst dazu, wenn sich eine konkrete Frage ohne sie nicht beantworten lässt.
- Alles folgt aus dem Zustand und dem Bericht jedes Zugs. Gerechnet wird aus dem vollen Zustand, die Diagnose darf alles sehen.

Bekannte Schwäche: Die Bilanz am Ende verschluckt das Tempo. Zwei Spieler, die gleich viel ernten, der eine aber viel später, sehen gleich aus. Vorerst hingenommen.

Entschieden:

- Alle Auswertungen laufen nach dem Spiel auf dem vollen Zustand, nichts davon während des Spiels aus der Sicht eines Spielers.
- Es werden keine Spiele gespeichert. Ein Spiel folgt aus Bots, Karte, Seed und Sitz und wird zum Ansehen noch einmal gespielt.
- Die Bilanz eines Spiels ist auch für eine UI gedacht, in der man die eigenen Spiele nachträglich ansieht. Sie darf also nicht nur im Lab liegen. Was viele Spiele vergleicht, dient allein der Untersuchung.
- Es zählt, was ausgegeben wurde, nicht ein späterer Preis.
- Spiele ohne Sieger sind beim Vergleich eine eigene Gruppe neben Siegern und Verlierern. Erst Mittelwerte, später die Streuung dazu.
- Herausgegriffen werden typische Spiele einer Gruppe und Ausreißer bei einem Posten.

Funde im Code, Stand 2026-11-18:

- Der `TurnReport` hält nicht fest, was ein Spieler in einem Zug bestellt und bezahlt hat. Die Kosten aller Befehle werden als eine Summe abgezogen, im Bericht stehen nur Matter vorher und nachher. Wofür ausgegeben wurde, lässt sich daraus nicht ablesen.
- Bei keinem Abbruch kommt Matter zurück. Ein abgebrochenes Training gibt nur den Space frei.
- Eine Armee merkt sich nicht, was ihre Einheiten gekostet haben. Solange kein Upgrade einen Preis ändert, sind gezahlter und aktueller Preis gleich.

## Stand

Stand 2026-11-18, nach dem ersten Durchstich.

- Der Bericht eines Zugs hält jetzt die bestellten Aufträge mit der gezahlten Matter fest.
- Es gibt die Bilanz eines Spielers je Zug, aufsummiert seit Spielbeginn. Herkunft: Start-Matter, Startbestand, geerntet. Ausgegeben für: Dots, Kampfeinheiten, Gebäude, Forschung. Noch da: auf der Hand, Dots, Kampfeinheiten, Gebäude, Forschung, in Auftrag. Weg: gefallen, zerstört, abgebrochen. Daneben, was er anderen angetan hat: vernichtet (Einheiten anderer Spieler und neutrale getrennt), abgerissen. Sie geht in allen Zufallsspielen auf.
- Ein einzelnes Spiel einer Messung lässt sich über seinen Schlüssel (Bots, Karte, Seed, Sitz) noch einmal spielen. Heraus kommen die Bilanz des Spiels als Tabelle und die Bilanz jedes Zugs als CSV.

Noch nicht da: die Bilanz je Spiel in der Arena, der Vergleich von Siegern, Verlierern und Spielen ohne Sieger, das Herausgreifen typischer Spiele und Ausreißer. Die vier Ursachen aus dem ersten Lauf sind noch nicht mit der Bilanz nachgeprüft.

Stand 2026-11-19: Der Bericht hält die Preise eines Zugs jetzt einmal je Spieler fest (`TrainingPrices`, `ConstructionPrices`), auch ohne Bestellung. Die Aufträge sind Armeen und Gebäude je Hexagon, ohne eigenen Preis.

## Zur Diskussion: ein Ort für die Preise

Stand 2026-11-19, aus dem Gespräch. Nicht entschieden.

Regeln sollen an einem Ort stehen, den Zugauflösung, Validierung und Clients gemeinsam fragen, damit nichts auseinanderläuft. Für Preise ist das `Costs`. Sobald ein Upgrade einen Preis ändert, reicht der heutige Stand nicht:

- `Costs.Research` und `Costs.Of` kennen die Upgrades des Spielers, `Costs.Training` und `Costs.Construction` nur die Settings. Ein Preis-Upgrade hat dort keinen Platz.
- Die Preisliste hat keine eigene Funktion. `TurnResolution.PayOrders` und `StateInvariants` bauen sie beide von Hand.
- Die Bilanz bewertet an etwa acht Stellen Bestand, Gefallenes und Abgebrochenes zum heutigen Preis. Ändern sich Preise im Spiel, geht sie nicht mehr auf. Das widerspricht auch „Es zählt, was ausgegeben wurde, nicht ein späterer Preis".

Vorschlag für den sauberen Zustand, ohne Rücksicht auf den Aufwand:

- Die Preisliste ist die Grundlage: `Costs.TrainingPrices(settings, upgradeLevels)` und `Costs.ConstructionPrices(settings, upgradeLevels)` lesen als Einzige die Preise der Settings und wenden ein Preis-Upgrade an. Alles andere in `Costs` rechnet damit.
- Jeder Aufrufer sagt, wessen Preis er meint, und übergibt die Upgrades eines Spielers.
- Space ist kein Preis. Er wird beim Tod einer Einheit und beim Abbruch zum Wert der Settings zurückgegeben und darf von keinem Upgrade abhängen, sonst läuft `UsedSpace` auseinander. `OrderCost` bündelt heute beides.
- Die Bilanz liest das Ausgegebene aus den Preisen des Berichts und bewertet Bestand zum Grundpreis. Die Differenz ist ein eigener Posten, „durch Upgrades gespart". So geht sie auf und zeigt, ob sich eine Preis-Forschung gelohnt hat.

Offen:

- Jetzt alles in einem Stück (eine Effektart für Preise in `UpgradeEffect`, die geänderten Signaturen, Tests mit Settings aus `DefaultSettings`, noch kein Upgrade der Standardregeln nutzt sie), oder jetzt nur die Preisliste ohne Upgrade-Parameter und der Rest mit dem ersten Preis-Upgrade?
- Bedenken gegen den Parameter allein: Ohne Effekt verhält sich ein Aufrufer mit den falschen Upgrades gleich, kein Test fängt das.
- Soll der Grundsatz „eine Regel, ein Ort" in `AGENTS.md` einen eigenen Abschnitt weit oben bekommen? Es gibt keine `CLAUDE.md`; falls eine dazukommt, sollte sie `AGENTS.md` nur einbinden.

## Offene Fragen

- Reicht die Tabelle je Zug als CSV, um ein Spiel anzusehen, solange es keine UI gibt?
- Braucht es die Bilanz zu festen Zügen, um das Tempo zu sehen?

## Woran man es merkt

- Nach einer Messung lässt sich sagen, worin sich Sieger und Verlierer einer Paarung unterscheiden.
- Jedes Spiel einer Messung lässt sich einzeln wieder abspielen und ansehen.
- Die offene Frage „Was war im früheren Test schlecht?" lässt sich für einen neuen Bot beantworten.
