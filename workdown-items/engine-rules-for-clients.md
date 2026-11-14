---
title: Ein Client muss Regeln der Engine nachbauen
status: todo
created: 2026-11-14
---
## Problem

Eine UI soll zeigen, wie sich das Spiel anfühlt. Dafür muss sie dem Spieler vor dem Absenden eines Zugs sagen, was seine Orders kosten, was er einnimmt und wie das Spiel ausgegangen ist. Die Engine kennt all diese Regeln, gibt sie aber nicht heraus: Sie stecken privat in der Zugauflösung oder in der Validierung. Ein Client müsste sie nachbauen, und jede Kopie einer Regel läuft irgendwann auseinander. Die alte UI hat genau das mit der Bewegungsreichweite getan (siehe [[movement-range]]).

Für den Kampf ist das gelöst: `Combat.Fight` ist die einzige Stelle der Regel, die Zugauflösung und eine Vorschau rufen dieselbe Methode auf und unterscheiden sich nur in dem, was sie übergeben (siehe [[combat-forecast-core]]). Für die Bewegung gilt dasselbe mit `Movement`. Dieses Item sammelt die Stellen, an denen das noch fehlt, und einige Einstellungen ohne Wirkung, die beim Durchsehen aufgefallen sind.

Die AI-Spieler sind ebenfalls Clients. Was sie heute selbst nachrechnen, steht bei den Funden.

## Funde

Stand 2026-11-14, jede Stelle ist vor der Umsetzung neu zu prüfen.

**Regeln, die nur intern erreichbar sind**

- **Wer gewonnen hat.** `GameState.IsGameOver` sagt nur, dass das Spiel vorbei ist. Ob es einen Sieger gibt oder alle Gebäude gleichzeitig gefallen sind, entscheidet `GameRunner.ToResult` im AI-Projekt. `PlayerGameState` kennt ebenfalls nur `IsGameOver`.
- **Was die Orders eines Zugs kosten.** `CommandValidator` summiert Matter und Platz privat und meldet nur, wenn es nicht reicht. Wie viel nach den bisher eingegebenen Orders übrig ist, erfährt ein Client nicht.
- **Was die nächste Stufe einer Forschung kostet.** Der Zugriff auf die Stufentabelle mit der Stufe nach den abgeschlossenen steht viermal im Code: in `CommandValidator`, in `TurnResolution`, in `RandomPlayer` und in `TurnPlan`.
- **Was ein Hexagon einbringt.** Die Regel steht in `TurnResolution.CollectIncome`: Es braucht ein Plane, Dots, die in diesem Zug eine Konstruktion beginnen, ernten nicht, das Hexagon begrenzt die Zahl der Erntenden, und es gibt nie mehr als die restliche Matter. `StateEvaluator` und `TurnPlan` rechnen Teile davon selbst.
- **Welche Gebäude eine Armee zerstört.** `TurnResolution.GetDestroyedBuildings` ist privat. Eine Vorschau wie beim Kampf gibt es nicht.
- **Ein Spiel Zug für Zug führen.** `GameRunner.Run` spielt eine Partie zwischen AI-Spielern bis zum Ende durch. Wer einen Menschen mitspielen lässt, schreibt die Schleife aus Sicht holen, validieren und auflösen neu.

**Einstellungen ohne Wirkung**

- **`HexagonSettings.IsBuildable`** wird von `CommandValidator` nicht geprüft. Nur die AI-Spieler halten sich von selbst daran. Keine der Standardkarten setzt den Wert auf `false`, das Loch ist heute also nicht erreichbar.
- **`HexagonSettings.NeutralArmy`** wird von der Engine nirgends gelesen. Keine der Standardkarten setzt eine neutrale Armee.

## Ideen

Keine Vorgaben. Wer das Item umsetzt, sieht sich die Stellen an und entscheidet.

- Das Muster von `Combat`: Eine Regel ist eine Funktion, die nur die Settings und die nötigen Werte bekommt. Die Zugauflösung ruft sie genauso auf wie ein Client. Ob die Zugauflösung dafür umgebaut werden muss oder einzelne Regeln herausgelöst werden, ist offen.
- Nicht jede Regel braucht eine Vorschau. Die Verwaltung der Warteschlangen, das Abbrechen und das Schreiben der Reports fragt vermutlich kein Client ab.
- Die AI-Spieler führen ihr Budget schrittweise mit. Ihnen hilft der Preis einer einzelnen Order mehr als die Summe eines ganzen Zugs.
- Eine Einstellung ohne Wirkung kann wirksam werden oder verschwinden.
- Nicht alles muss vor der ersten UI fertig sein. Was eine UI zuerst braucht, ergibt sich aus [[ui-concept]].

## Woran man es merkt

- Ein Client zeigt Kosten, Einkommen und Spielausgang, ohne eine Regel selbst zu rechnen.
- Jede dieser Regeln steht einmal im Code. Zugauflösung, Validierung und Client kommen zum selben Ergebnis, weil sie dieselbe Stelle fragen.
- Keine Einstellung der `GameSettings` wird akzeptiert und dann ignoriert.
