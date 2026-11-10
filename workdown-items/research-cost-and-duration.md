---
id: research-cost-and-duration
title: Forschung kostet nichts und nutzt die falsche Stufe
status: done
created: 2026-10-14
updated: 2026-11-10
completed: 2026-11-10
depends_on: [research-level-validation]
---
## Problem

Drei Fehler rund um Forschung in `GameState`:

1. **Forschung ist kostenlos.** `Validate` rechnet die Kosten in die Matter-Prüfung ein, aber `NextTurn` zieht sie nie von `PlayerState.Matter` ab. Training und Bau werden abgezogen, Forschung nicht.
2. **Forschungen gehen verloren.** `PlayerState.Researches` ist nach Abschlussrunde und Hexagon indiziert. Zwei Forschungen auf demselben Hexagon, die in derselben Runde fertig werden, fallen auf denselben Schlüssel; die zweite überschreibt die erste. Der Spieler zahlt zweimal und bekommt eine Stufe. Der Review zu PR #8 fand den Fall für dieselbe Forschung in aufeinanderfolgenden Runden (Stufe 1 Dauer 3 in Runde t, Stufe 2 Dauer 2 in Runde t+1). Er tritt genauso mit zwei verschiedenen Upgrades auf.
3. **Forschung ohne Punkt läuft weiter.** Ein Punkt ist beim Start Voraussetzung (`UpgradeRequiresDot`), und `IsValid` meldet eine laufende Forschung ohne Punkt als ungültigen Zustand. `NextTurn` setzt das aber nicht durch: Stirbt der Punkt im Kampf oder zieht er weg, wird die Forschung trotzdem fertig.

Der ursprüngliche vierte Punkt, die falsche Stufe bei gestapelten Forschungen, entfällt mit den Spielregeln unten: Es gibt kein Stapeln mehr, deshalb ist "abgeschlossene Stufe plus eins" immer die richtige Stufe.

## Spielregeln

Entschieden am 2026-11-10. Die Regeln ersetzen das Stapeln, das PR #12 noch zugelassen hat.

1. **Pro Upgrade läuft höchstens eine Forschung.** Wer dieselbe Forschung startet, während sie läuft, wird abgelehnt. Die nächste Stufe kann erst beantragt werden, wenn die vorherige abgeschlossen ist. Es gibt keine Warteschlange.
2. **Die Regel gilt über alle Hexagons.** Die Stufe gehört dem Spieler, nicht dem Hexagon. Dasselbe Upgrade auf zwei Hexagons, auch innerhalb einer Runde, wird abgelehnt.
3. **Pro Hexagon läuft höchstens eine Forschung.** Ein Hexagon mit laufender Forschung ist belegt, auch für andere Upgrades. Wer parallel forschen will, braucht ein zweites Hexagon, das das Upgrade anbietet und auf dem ein eigener Punkt steht.
4. **Ohne Punkt bricht die Forschung ab, Matter ist verloren.** Steht am Ende der Zugauflösung kein eigener Punkt mehr auf dem Hexagon, wird die Forschung gestrichen. Es gibt keine Rückerstattung. Dabei gilt:
   - Ein Bewegungsbefehl, der den letzten Punkt von einem Hexagon mit laufender oder in dieser Runde beantragter Forschung abzieht, wird von `Validate` abgelehnt. Abbruch entsteht nur durch Verluste im Kampf.
   - Forschungen werden abgeschlossen, bevor Bewegungen und Kämpfe aufgelöst werden. Eine Forschung, die in dieser Runde fertig wird, ist sicher, auch wenn der Punkt in derselben Runde stirbt.
   - Stirbt der Punkt in der Runde, in der die Forschung gestartet wurde, ist sie bezahlt und wird abgebrochen.
   - Entscheidend ist, ob am Ende der Runde ein eigener Punkt auf dem Hexagon steht, nicht ob derselbe Punkt geblieben ist. Ein Punkt, der in derselben Runde einzieht, hält die Forschung am Laufen.
5. **Kosten werden beim Start abgezogen**, wie bei Training und Bau. Die Höhe ist der Preis der Stufe "abgeschlossene Stufe plus eins" aus `GameSettings.Upgrades`.

Unverändert: Ein Punkt darf gleichzeitig bauen und Forschung ermöglichen. Der Punkt wird durch Forschung weder verbraucht noch gebunden.

## Ziel

Jede gestartete Forschung kostet die Matter ihrer Stufe, dauert so lange wie die Tabelle für diese Stufe angibt, kommt genau einmal an oder wird nach Regel 4 abgebrochen. `Validate` lehnt alles ab, was die Regeln 1 bis 4 verletzt, `IsValid` erkennt Zustände, die sie verletzen.

## Akzeptanzkriterien

- Matter nach einer Runde mit Forschung = vorher + Einkommen − Kosten der Stufe.
- Dieselbe Forschung während einer laufenden wird abgelehnt, auch auf einem anderen Hexagon, auch zweimal in derselben Runde auf zwei Hexagons.
- Eine zweite Forschung auf einem Hexagon mit laufender Forschung wird abgelehnt.
- Ein Bewegungsbefehl, der den letzten Punkt von einem Forschungs-Hexagon abzieht, wird abgelehnt.
- Stirbt der letzte Punkt im Kampf, ist die Forschung in der nächsten Runde verschwunden, die Stufe unverändert, die Matter nicht erstattet.
- Wird die Forschung in derselben Runde fertig, in der der Punkt stirbt, ist die Stufe erhöht.
- Zieht ein anderer Punkt in der Runde ein, in der der letzte abzieht, läuft die Forschung weiter.
- Das Datenmodell kann zwei Forschungen nicht auf denselben Schlüssel legen.
- `RandomGamesWithResearch_ConserveMatter` in `EngineInvariantsTest` läuft ohne Skip. Abgebrochene Forschungen zählen als verbrauchte Matter.
- Die AI-Spieler liefern unter den neuen Regeln keine ungültigen Befehle. Regel 1, 2 und das Ankern des Punkts sind schon umgesetzt, Regel 3 fehlt in `CommanderPlayer` und `RandomPlayer`.

## Umsetzung

Entschieden am 2026-11-10: `PlayerState.Researches` ist nach Hexagon geschlüsselt, `ImmutableDictionary<Hexagon, Research>` mit `Research(Upgrade, CompletionTurn)`. Regel 3 steckt damit im Typ, zwei Forschungen können nicht mehr auf einem Schlüssel landen. Die Alternativen (nach Upgrade schlüsseln, oder die Warteschlange `Runde → Hexagon → Upgrade` behalten und nur validieren) wurden verworfen: Die neuen Operationen, Abbruch bei Punktverlust und Bewegungsprüfung, sind Nachschläge pro Hexagon, und die Form entspricht Armeen und Gebäuden.

- `NextTurn` zieht die Kosten beim Einreihen ab, schließt fällige Forschungen vor Bewegungen und Kämpfen ab und streicht nach den Kämpfen jede Forschung, auf deren Hexagon kein eigener Punkt mehr steht.
- `Validate` meldet `UpgradeAlreadyInResearch` (Regel 1), `UpgradeRequestedOnMultipleHexagons` für jeden betroffenen Command (Regel 2), `HexagonAlreadyResearching` (Regel 3) und `MovementLeavesResearchWithoutDot` (Regel 4). Ankommende Punkte derselben Runde zählen mit, ein Tausch des Punkts ist erlaubt. `UpgradeExceedsAvailableLevel` meldet nur noch das Tabellenende.
- `IsValid` meldet zusätzlich `UpgradeResearchedOnMultipleHexagons`.
- `PlayerState.PendingUpgrades()` ersetzt `PendingResearchLevels()`; die drei AI-Spieler nutzen es, `TurnPlan` ankert einen Punkt auf jedem Hexagon mit laufender Forschung, `CommanderPlayer` und `RandomPlayer` überspringen belegte Hexagons.
- `TestSettings` definiert eine Stufe `UpgradeStartMatter`, damit Regel 3 mit zwei verschiedenen Upgrades testbar ist.

Tests: `ResearchValidationTest` (Validate und IsValid) und `ResearchResolutionTest` (NextTurn) in `Simulturn.Core.Test`; `RandomGamesWithResearch_ConserveMatter` läuft ohne Skip.

## Referenz

Tag `archive/add-web-ui` (ehemaliger Branch), Commit d1c4079. Zur Orientierung, nicht als Vorlage.

Das README beschreibt unter "Upgrade Units" ein älteres Konzept (Gebäude forschen, Abbruch mit Teilerstattung), das die Engine nie umgesetzt hat. Das wird hier nicht angeglichen.
