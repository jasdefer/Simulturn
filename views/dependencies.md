# Graph: depends_on

Directed graph of items connected through `depends_on`, nested by `parent`.

```mermaid
flowchart TD
    combat-forecast-core["Kampfausgang vorab berechnen können"]
    game-persistence["Spielstände speichern und wiederherstellen"]
    map-generator-and-default-settings["Karten erzeugen und Settings nicht sechsfach pflegen"]
    movement-range["Bewegungsreichweite pro Einheitentyp"]
    research-cost-and-duration["Forschung kostet nichts und nutzt die falsche Stufe"]
    research-level-validation["Forschung kann über die Upgrade-Tabelle hinaus gestartet werden"]
    turn-report["Nachvollziehbarkeit der Zugauflösung"]
    subgraph ui ["Neue UI"]
        ui-concept["UI-Konzept erarbeiten"]
    end
    validate-regression-tests["Regressionstests für Validate mit Warteschlangen auf anderen Hexagons"]
    research-cost-and-duration --> research-level-validation
```
