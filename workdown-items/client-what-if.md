---
id: client-what-if
title: Ein Client kann nicht fragen, was passieren würde
status: todo
created: 2026-11-16
---
## Problem

Ein Spieler will vor dem Absenden wissen, wohin sein Zug führt: Was habe ich in drei Zügen an Matter, Einheiten und Gebäuden, wenn ich diese Befehle gebe? Was passiert, wenn der Gegner dort angreift? Eine UI will das als Vorschau zeigen. Ein Bot, der mehr können soll als feste Regeln abzuarbeiten, muss Züge ausprobieren können, bevor er einen spielt.

Die Engine kann einen Zug auflösen, aber nur mit dem vollen Zustand und den Befehlen aller Spieler. Ein Client hat beides nicht. Er kennt seine Sicht und das, woran er sich erinnert, und er kennt nur die eigenen Befehle. Heute bleibt ihm, die Folgen seines Zugs selbst nachzurechnen.

Die Frage hat drei Stufen mit sehr verschiedener Unsicherheit:

- **Die eigene Entwicklung.** Was aus den eigenen Aufträgen wird, steht fest, solange niemand eingreift.
- **Ein einzelner Kampf.** Unsicher ist nur, was der Gegner dort hat.
- **Ein ganzer Zug.** Unsicher ist alles, was der Spieler nicht sieht, und was der Gegner tut.

## Funde

Stand 2026-11-16, vor der Umsetzung neu zu prüfen.

- `TurnResolution.Resolve` und `Game.PlayTurn` brauchen einen `GameState` und die Befehle aller Spieler.
- Einen Kampf kann ein Client bereits vorhersagen: `Combat.Fight` nimmt, was der Spieler über die Armeen weiß.
- `Harvest.Forecast` liefert das Einkommen des laufenden Zugs mit den eingegebenen Befehlen. Weiter als einen Zug und über das Einkommen hinaus reicht keine Vorhersage.
- Aus einer Sicht und Erinnerungen entsteht nirgends ein Zustand, den die Engine weiterspielen kann.
- Das Spiel ist ohne Zufall: Derselbe Zustand und dieselben Befehle ergeben denselben nächsten Zustand.

## Ideen

- Die eigene Entwicklung über mehrere Züge aus der Sicht und den geplanten Befehlen.
- Ein Weg von „Sicht, Erinnerungen und ausdrückliche Annahmen" zu einem gültigen Zustand, den die Engine auflöst. Die Annahmen kommen vom Aufrufer; die Engine rät nicht (siehe [[opponent-estimate]]).
- Die Vorhersage sagt, worauf sie beruht: was gesehen, was erinnert und was angenommen ist.

## Offene Fragen

- Welche der drei Stufen braucht die UI, welche nur ein Bot?
- Wie viel davon gehört in den Core? Die Regel gehört dorthin, die Annahme über den Gegner nicht.
- Wie schnell muss eine Vorhersage sein, wenn ein Bot pro Zug hunderte davon braucht?
- Wie verhindert man, dass eine Vorhersage für sicher gehalten wird, die auf Annahmen steht?

## Woran man es merkt

- Ein Client kann die Folgen der eigenen Befehle über mehrere Züge erfragen, ohne eine Regel selbst zu rechnen.
- Ein Bot kann einen Zug unter Annahmen durchspielen, und das Ergebnis kommt aus derselben Zugauflösung wie das echte Spiel.
