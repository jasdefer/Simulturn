---
id: fight-rule
title: Die Kampfregel kippt an einzelnen Einheiten
status: todo
created: 2026-11-27
---
## Problem

Der Ausgang eines Kampfes soll dem folgen, was sich gegenübersteht. Heute entscheidet bei gemischten Armeen oft eine einzelne Einheit zwischen „kein Verlust" und „alle tot", und bei kleinen Armeen wirkt der Exponent kaum. Wer einen Kampf plant, ein Mensch wie ein Bot, kann sich auf das Ergebnis nicht verlassen.

Gefunden am 2026-11-27 bei der Durchsicht der Standardwerte für den ersten Bot nach Regeln ([[ai-opponents-too-weak]]).

## Absicht der Regel

Festgehalten am 2026-11-27:

- Viele Einheiten desselben Typs werden durch den Exponenten belohnt.
- Ein einzelner Typ ist schlecht gegen seinen Konter.
- Es soll also einen Anreiz geben, alles auf einen Typ zu setzen, und zugleich einen, möglichst gleichmäßig zu verteilen.

## Funde

Stand 2026-11-27. T, S, C sind Triangle, Square, Circle, gemischte Armeen stehen als T/S/C.

- **Die Regel heute.** Von jedem Typ werden die Einheiten gestrichen, die der Gegner kontert. Der Rest zählt hoch Exponent (1,2), ein Dot ein Viertel. Der Verlierer verliert alles, der Sieger den Anteil „Stärke des Verlierers durch Stärke des Siegers", je Typ aufgerundet.
- **Der Rest entscheidet über die ganze Armee.** Die Stärke wird nach dem Streichen gerechnet. Bei 4/4/4 gegen 4/4/3 bleibt 1 gegen 0, und der Sieger verliert nichts. Bei 5/5/5 gegen 5/4/6 bleibt 1 gegen 1, und beide sind tot. Gestrichene Einheiten richten keinen Schaden an und stehen beim Sieger wieder auf.
- **Das Aufrunden frisst den Exponenten.** Bei 10 gegen 5 Triangles wären es 4,35 Verluste, daraus werden 5, genau wie ohne Exponent. Bis etwa 10 Einheiten wirkt der Exponent kaum.
- **Siege ohne Überlebende.** Durch das Aufrunden kann der Sieger alles verlieren (6 T gegen eine Wache aus 2/2/2).
- **Der Konter zählt doppelt.** Eine konternde Einheit streicht eine gegnerische und kämpft selbst mit voller Stärke. 5 S sind ein Gleichstand gegen 10 T, und 10 S + 1 C schlagen 10 T + 10 S.
- **Die Belohnung für einen Typ ist bei 1,2 fast unsichtbar.** Eine reine Armee aus 12 schlägt 4/4/4 und behält 1 Einheit.

## Erwogen

Stand 2026-11-27, nichts davon ist entschieden oder umgesetzt. Alle Zahlen kommen aus einem Skript, das die Regeln nachbaut, nicht aus der Engine; das Skript ist nicht aufbewahrt. Die Kämpfe der Tabellen sind deshalb an der Engine nachzuprüfen. „A 6/10" heißt: A gewinnt, 6 von 10 überleben.

- **Neu runden:** Der Verlust der ganzen Armee wird auf die nächste Einheit gerundet statt je Typ aufgerundet. Unabhängig von allem anderen. Behebt die meisten Siege ohne Überlebende, nicht das Kippen. Bei sehr knappen Kämpfen bleibt ein Sieger ohne Überlebende möglich.
- **Bonus:** Ein Stapel zählt mehr, je mehr seiner Einheiten ein Konter-Ziel finden. Verworfen: Eine eigene Einheit mehr kann die eigene Armee schwächen (10 T + 1 C verlieren gegen 10 T).
- **Konter-Faktor:** Eine gekonterte Einheit wird nicht gestrichen, sondern zählt noch einen Teil. Der Wert 1 ist die heutige Regel. Kein Kippen mehr, der Konter wird weicher, ein Wert mehr in den Settings.
- **Stärke vor und nach dem Streichen mischen:** Als Mischung der Stärken fast dasselbe wie der Konter-Faktor. Als Mischung nur der Verluste (Sieger wie heute) bleibt das Kippen zur Hälfte.
- **Der Konter tötet wirklich:** Erst tötet jede Einheit eine gegnerische des Typs, den sie kontert, gleichzeitig und aus den Anfangszahlen gerechnet. Dann kämpft der Rest nach der heutigen Formel, ohne weiteren Konter. Fast die heutige Regel, nur stehen die gestrichenen Einheiten des Siegers nicht wieder auf. Kein Kippen mehr, kein neuer Wert, im Kopf rechenbar, aber gemischte Kämpfe werden sehr verlustreich und der Konter zählt weiter doppelt. Mit einem Preis für die konternde Seite enden gemischte Kämpfe fast immer mit zwei toten Armeen.
- **Der Konter tötet nur einen Teil:** Wie „Der Konter tötet wirklich", aber eine Einheit tötet weniger als ein ganzes Ziel; die überlebenden Ziele kämpfen in der zweiten Phase mit. Mit dem vollen Konter verlieren 100 T gegen 100 S alles, ohne Schaden anzurichten; mit 0,75 behalten die Squares etwa 81, und 4/4/4 behält gegen 4/4/3 etwa 2 von 12 (von Hand gerechnet). In reinen Konter-Kämpfen ergibt das dasselbe wie der Konter-Faktor, in gemischten sterben die gekonterten Einheiten wirklich. Als Startwert war 0,75 im Gespräch.
- **Kampf in Runden:** Nicht durchgerechnet, deutlich mehr Regel.

Alle Spalten mit Exponent 1,2, alle außer „heute" neu gerundet:

| Kampf (A gegen B) | heute | Konter-Faktor 0,75 | Konter-Faktor 0,5 | Konter tötet, ohne Preis | Konter tötet, Preis 0,5 |
|---|---|---|---|---|---|
| 10 T gegen 5 T | A 5/10 | A 6/10 | A 6/10 | A 6/10 | A 6/10 |
| 10 T gegen 3 S | A 6/10 | A 7/10 | A 7/10 | A 4/10 | A 6/10 |
| 10 T gegen 5 S | beide tot | A 2/10 | A 4/10 | beide tot | A 3/10 |
| 10 T gegen 6 S | B 2/6 | B 1/6 | A 2/10 | B 2/6 | A 1/10 |
| 10 T gegen 8 S | B 6/8 | B 5/8 | B 2/8 | B 6/8 | B 2/8 |
| 10 T gegen 10 S | B 10/10 | B 8/10 | B 6/10 | B 10/10 | B 5/10 |
| 4/4/4 gegen 4/4/3 | A 12/12 | A 4/12 | A 2/12 | A 1/12 | beide tot |
| 4/4/4 gegen 3/3/3 | A 12/12 | A 8/12 | A 5/12 | A 3/12 | beide tot |
| 4/4/4 gegen 2/2/2 | A 12/12 | A 10/12 | A 9/12 | A 6/12 | A 3/12 |
| 5/5/5 gegen 4/4/5 | A 15/15 | A 7/15 | A 4/15 | A 2/15 | beide tot |
| 5/5/5 gegen 5/4/6 | beide tot | B 0/15 | B 0/15 | beide tot | beide tot |
| 5/5/5 gegen 5/4/7 | B 7/16 | B 4/16 | B 2/16 | B 1/16 | beide tot |
| 12 T gegen 4/4/4 | A 1/12 | A 2/12 | A 2/12 | A 1/12 | A 1/12 |
| 6/6/0 gegen 4/4/4 | B 0/12 | A 0/12 | A 1/12 | B 1/12 | B 2/12 |
| 10 T + 10 S gegen 10 S + 1 C | B 1/11 | A 2/20 | A 5/20 | B 1/11 | A 5/20 |
| 10 T + 1 C gegen 10 T | beide tot | A 0/11 | A 0/11 | beide tot | A 1/11 |
| 6 T gegen Wache 2/2/2 | A 0/6 | A 1/6 | A 1/6 | A 1/6 | A 0/6 |

Sechs Aufteilungen von 12 Einheiten gegeneinander, mit „Konter tötet, ohne Preis" und Exponent 1,5. Die Zahl sind die Überlebenden des Siegers: „+" heißt, die Zeile gewinnt, „−" die Spalte, 0 beide tot.

| Zeile gegen Spalte | 12/0/0 | 0/12/0 | 0/0/12 | 6/6/0 | 8/2/2 | 4/4/4 |
|---|---|---|---|---|---|---|
| 12/0/0 | 0 | −12 | +12 | −6 | +2 | +2 |
| 0/12/0 | +12 | 0 | −12 | +8 | +8 | +2 |
| 0/0/12 | −12 | +12 | 0 | 0 | −7 | +2 |
| 6/6/0 | +6 | −8 | 0 | 0 | +5 | −1 |
| 8/2/2 | −2 | −8 | +7 | −5 | 0 | +1 |
| 4/4/4 | −2 | −2 | −2 | +1 | −1 | 0 |

Gemessen an der Absicht: Ein Typ ist eine Wette (alles oder nichts gegen einen anderen reinen Typ), gleichmäßig ist die Versicherung (verliert nie hoch, gewinnt nie hoch, lässt sich nicht gezielt kontern). Die Härte des Konters steuert das Risiko der Wette, der Exponent ihre Belohnung: Eine reine Armee behält gegen eine gleichmäßige bei Exponent 1,2 eine von 12 Einheiten, bei 1,5 zwei von 12 und fünf von 24.

Keine der Varianten ist gewählt. Die Entscheidung fällt bei der Bearbeitung dieses Items, weil sie die Grundregel des Spiels ändert.

## Offene Fragen

- Die gleichmäßige Armee verliert knapp gegen jeden reinen Typ und gewinnt gegen keinen. Reicht „nicht konterbar" als Anreiz, oder ist sie damit immer die schlechtere Wahl?
- Soll der Konter doppelt zählen dürfen (11 Einheiten schlagen 20)?
- Soll der falsche Typ ein Totalverlust ohne Gegenschaden sein, oder soll er den Gegner noch etwas kosten? Ein einziger verlorener Kampf kann sonst das Spiel entscheiden.
- Wie verlustreich sollen Kämpfe sein? Mit „Konter tötet" bleibt von zwei fast gleichen gemischten Armeen fast nichts übrig; das begünstigt den, der nachbauen kann, also den Verteidiger.
- Kämpfe mit mehr als zwei Spielern laufen paarweise, und die Verluste des dominanten Spielers addieren sich. Wie verträgt sich das mit einer Konter-Phase, in der Einheiten wirklich sterben?
- Wachen aus gemischten Typen werden teurer zu räumen. Gewollt?
- Was zeigt der Kampfbericht, wenn ein Kampf zwei Phasen hat?
- Welcher Exponent? Die Arena kann das erst mit einem Bot beantworten, der Armeen baut.
- Soll dem Sieger immer mindestens eine Einheit bleiben?

## Woran man es merkt

- Eine Einheit mehr oder weniger ändert das Ergebnis eines Kampfes nur wenig.
- Eine zusätzliche eigene Einheit macht eine Armee nie schwächer.
- Der Exponent ist auch bei kleinen Armeen im Ergebnis zu sehen.
- Wer gewinnt, hat Überlebende.
- Die README beschreibt die Regel, die die Engine rechnet.
