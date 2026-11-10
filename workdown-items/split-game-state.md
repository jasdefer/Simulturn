---
id: split-game-state
title: GameState vereint Zustand, Zugauflösung und Validierung in einer Datei
status: todo
created: 2026-11-10
---
## Problem

`Source/Simulturn.Core/Model/State/GameState.cs` hat 757 Zeilen und drei Rollen:

- **Zustand**: das Record mit seinen Properties, der Konstruktor aus den Settings, `FromParts`.
- **Zugauflösung**: `NextTurn` mit rund 290 Zeilen in einer Methode, dazu `Fight`, `GetNewTrainings` und `GetNewConstructions`.
- **Validierung**: `Validate` für Commands und `IsValid` für den Zustand, zusammen rund 300 Zeilen.

Die Phasen der Auflösung existieren nur als Kommentarüberschriften in `NextTurn`: `// Income`, `// Trainigs`, `// Constructions`, `// Researches`, `// Movements`, `// Fights`, `// Destroy Buildings`. Die Phasen teilen sich Lokalvariablen der einen Methode (`playerStates`, `remainingMatter`, `potentialFightHexagons`), deshalb lassen sie sich nicht ohne Weiteres in Methoden ziehen.

Folgen:

- Wer eine Regel sucht, liest die ganze Datei. Die Kürzung von Baustellen nach Dot-Verlusten steht zum Beispiel in der Kampfphase, vier Ebenen tief hinter den Verlusten. Der Abbruch von Trainings nach Gebäudeverlust steht in der Zerstörungsphase, fünf Ebenen tief. Beide Stellen waren Fehlerquellen ([[pending-only-queues]], [[training-cancellation-after-completion]]).
- Die Kampfrechnung `Fight` ist privat und von außen nicht erreichbar; [[combat-forecast-core]] braucht sie öffentlich, ohne Kopie.
- Jede Änderung an der Auflösung ([[turn-result]], [[turn-report]], [[flat-pending-queues]]) streut Zeilen über die eine Methode statt eine Phase zu ändern. Ein Report, den jede Phase selbst schreibt, hat in dieser Form keinen natürlichen Ort.
- `Validate` und `IsValid` liefern die Unions aus `Model/State/Validation/`, liegen aber nicht dort.

## Ideen

Unverbindlich, der Implementierer entscheidet:

- Drei Typen für drei Rollen: `GameState` behält die Properties und dünne öffentliche Einstiege; die Auflösung und die beiden Validierungen bekommen eigene interne Typen. Die Validierungen gehören neben die Unions in `Validation`.
- Die Auflösung als Instanz pro Zug, die den Arbeitszustand (Builder je Spieler, Matter, Kampfhexagons, später der Report) als Felder hält. Dann kann jede Phase eine Methode mit Namen sein, ohne dass fünf Parameter durchgereicht werden. Die Stornierungen (Baustellen nach Dot-Verlust, Trainings nach Gebäudeverlust) werden eigene Methoden statt Teil der Kampf- und Zerstörungsphase.
- Die Kampfrechnung als reine Funktion zweier Armeen und ihrer Exponenten, getrennt von der Frage, wer auf dem Hexagon steht. Das ist der Teil, den [[combat-forecast-core]] veröffentlichen will.
- Keine Verhaltensänderung. Reihenfolge der Phasen, Rundungen, Stornierungsreihenfolge bleiben; die bestehende Suite ist das Netz.

## Reihenfolge

Vor [[turn-result]] und [[flat-pending-queues]]: Die Aufteilung ist verhaltensneutral und macht die beiden Umbauten zu kleineren Diffs auf kleineren Methoden. Andersherum verschiebt man Code, der gerade erst geändert wurde.

## Akzeptanzkriterien

- Die öffentliche API von `GameState` ist unverändert.
- Jede Phase der Auflösung ist eine Methode mit Namen; in der Auflösung stehen keine Kommentarüberschriften mehr.
- Die Kampfrechnung ist eine eigene Funktion, die nur Armeen und Exponenten kennt.
- `GameStateTest`, `PendingQueueTest`, `ResearchResolutionTest`, `ResearchValidationTest`, `ValidatePendingWorkTest`, `EngineInvariantsTest` und die Arena laufen ohne Änderung an den Erwartungen.
