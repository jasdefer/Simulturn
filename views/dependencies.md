# Graph: depends_on

Directed graph of items connected through `depends_on`, nested by `parent`.

```mermaid
flowchart TD
    combat-forecast-core["Kampfausgang vorab berechnen können"]
    dotnet-upgrade-and-union-types["Auf die nächste .NET-Version wechseln und neue Sprachfeatures nutzen"]
    flat-pending-queues["Warteschlangen für Trainings und Konstruktionen ohne verschachtelte Dictionaries"]
    game-persistence["Spielstände speichern und wiederherstellen"]
    map-generator-and-default-settings["Karten erzeugen und Settings nicht sechsfach pflegen"]
    movement-range["Bewegungsreichweite pro Einheitentyp"]
    pending-only-queues["Warteschlangen enthalten nur offene Aufträge"]
    player-knowledge-in-state["Wissen der Spieler liegt im Regelzustand"]
    research-cost-and-duration["Forschung kostet nichts und nutzt die falsche Stufe"]
    research-level-validation["Forschung kann über die Upgrade-Tabelle hinaus gestartet werden"]
    split-game-state["GameState vereint Zustand, Zugauflösung und Validierung in einer Datei"]
    training-cancellation-after-completion["Zerstörte Gebäude brechen bereits fertige Trainings ab"]
    turn-report["Nachvollziehbarkeit der Zugauflösung"]
    turn-result["NextTurn liefert Zustand und Ereignisse getrennt"]
    subgraph ui ["Neue UI"]
        ui-concept["UI-Konzept erarbeiten"]
    end
    validate-regression-tests["Regressionstests für Validate mit Warteschlangen auf anderen Hexagons"]
    pending-only-queues --> training-cancellation-after-completion
    research-cost-and-duration --> research-level-validation
    turn-report --> turn-result
```
