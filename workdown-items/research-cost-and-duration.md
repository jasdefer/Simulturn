---
id: research-cost-and-duration
title: Forschung kostet nichts und nutzt die falsche Stufe
status: todo
created: 2026-10-14
depends_on: [research-level-validation]
---
## Problem

Zwei Fehler in `GameState.NextTurn`:

1. **Forschung ist kostenlos.** `Validate` rechnet die Kosten in die Matter-Prüfung ein, aber `NextTurn` zieht sie nie von `PlayerState.Matter` ab. Training und Bau werden abgezogen, Forschung nicht.
2. **Falsche Stufe.** Wird eine Forschung gestartet, während dieselbe Forschung noch läuft, übernimmt `NextTurn` Dauer und Kosten der zuletzt abgeschlossenen Stufe statt der nächsten freien.

## Ziel

Jede gestartete Forschung kostet die Matter ihrer tatsächlichen Stufe und dauert so lange, wie die Tabelle für diese Stufe angibt. Dabei gilt dieselbe Stufenzählung wie in [[research-level-validation]].

## Akzeptanzkriterien

- Matter nach einer Runde mit Forschung = vorher + Einkommen − Kosten der richtigen Stufe.
- Eine gestapelte Forschung landet genau einmal auf der nächsten Stufe, nach der Dauer dieser Stufe.
- Zwei Forschungen auf demselben Hexagon gehen nicht verloren, wenn sie in derselben Runde fertig werden. Der Review zu PR #8 fand genau das: `PlayerState.Researches` ist nach Abschlussrunde und Hexagon indiziert, und wenn eine spätere Stufe kürzer dauert als die frühere (Stufe 1 Dauer 3 in Runde t, Stufe 2 Dauer 2 in Runde t+1), fielen beide auf denselben Schlüssel. Der Spieler zahlte zweimal und bekam eine Stufe.

## Zu entscheiden

Wie der letzte Punkt gelöst wird: Datenmodell der Warteschlange erweitern, den Fall ablehnen, oder als Spielregel festlegen, dass pro Hexagon nur eine Forschung gleichzeitig läuft. Das hat Auswirkungen auf die AI-Spieler, die heute pro Hexagon planen.

## Referenz

Tag `archive/add-web-ui` (ehemaliger Branch), Commit d1c4079. Zur Orientierung, nicht als Vorlage.
