---
title: Ein Client muss Regeln der Engine nachbauen
status: done
created: 2026-11-14
updated: 2026-11-15
completed: 2026-11-15
---
## Problem

Eine UI soll zeigen, wie sich das Spiel anfühlt. Dafür muss sie dem Spieler vor dem Absenden eines Zugs sagen, was seine Orders kosten, was er einnimmt und wie das Spiel ausgegangen ist. Die Engine kannte all diese Regeln, gab sie aber nicht heraus: Sie steckten privat in der Zugauflösung oder in der Validierung. Ein Client hätte sie nachbauen müssen, und jede Kopie einer Regel läuft irgendwann auseinander. Die alte UI hat genau das mit der Bewegungsreichweite getan (siehe [[movement-range]]).

Für den Kampf war das gelöst: `Combat.Fight` ist die einzige Stelle der Regel (siehe [[combat-forecast-core]]). Für die Bewegung gilt dasselbe mit `Movement`. Dieses Item hat die Stellen gesammelt, an denen das noch fehlte, und zwei Einstellungen ohne Wirkung.

## Ergebnis

Jede Regel ist eine Funktion in `Engine/`, die nur die Settings und die nötigen Werte bekommt. Zugauflösung, Validierung, AI-Spieler und ein Client rufen dieselbe Stelle auf.

| Frage eines Clients | Stelle |
|---|---|
| Wer ist besiegt, wer hat gewonnen? | `Standing.Of`, in jeder Sicht als `PlayerGameState.Standing` |
| Was kosten meine Orders, was bleibt übrig? | `Costs` (eine Order oder alle Orders eines Zugs, Matter und Platz) |
| Was kostet die nächste Stufe einer Forschung? | `GameSettings.NextUpgradeLevel`, erledigt mit [[upgrade-concept]] |
| Was nehme ich mit diesen Orders ein? | `Harvest.Of` für ein Hexagon, `Harvest.Forecast` für eine Sicht |
| Welche Gebäude reißt diese Armee ab? | `Destruction.Of` |
| Wie führe ich ein Spiel Zug für Zug? | `Game.Start` und `Game.PlayTurn` |

### Niederlage

Am 2026-11-15 festgelegt:

- Ein Spieler ohne Gebäude und ohne laufende Konstruktion ist besiegt. Wer noch baut, ist noch im Spiel.
- Die Niederlage ist endgültig. Ein besiegter Spieler gibt keine Orders mehr (`PlayerDefeated`), also entsteht nie wieder ein Gebäude. Deshalb wird die Niederlage nicht gespeichert, sie folgt aus dem Zustand.
- Laufende Forschungen eines Besiegten enden, die Matter ist verloren.
- Die Armeen eines Besiegten bleiben stehen und kämpfen mit seinen Upgrades, wenn jemand ihr Hexagon betritt. Sie zerstören weiter Gebäude, auf denen sie stehen, und der Besiegte sieht weiter, was sie sehen.
- Jeder Spieler erfährt jede Niederlage: im Report des Zugs (`TurnReport.Defeats`) und in jeder Sicht.
- Das Spiel läuft, solange mindestens zwei Spieler nicht besiegt sind. Bleibt einer, hat er gewonnen. Bleibt keiner, haben alle verloren.

Eine Folge: Zu zweit endete das Spiel früher, sobald das letzte Gebäude fiel. Jetzt läuft es weiter, solange der Verlierer noch eine Konstruktion hat.

### Neutrale Armeen

`HexagonSettings.NeutralArmy` wurde angenommen und nie gelesen. Am 2026-11-15 festgelegt: Neutrale Armeen sind Wachen und werden bekämpft.

- Sie stehen von Spielbeginn an auf ihrem Hexagon (`GameState.NeutralArmies`), bewegen sich nie und haben keine Upgrades.
- Wer ihr Hexagon betritt, kämpft gegen sie. Der Kampf läuft über `Combat.Fight` wie jeder andere und steht genauso im Report. Die Wache hat dort die feste Kennung `Neutral.Id`; kein Spieler darf sie tragen.
- Die Startaufstellung steht in den Settings und ist damit jedem Spieler bekannt. Im Spiel sieht ein Spieler die Wachen wie gegnerische Einheiten: nur die Anzahl, nur bei voller Sicht.
- Wachen zerstören keine Gebäude und zählen nicht für den Spielausgang. Auf einem Start-Hexagon steht keine Wache.
- `MapGenerator` setzt Wachen auf Wunsch in die Mitte und auf die Expansionen (`CenterGuard`, `ExpansionGuard`). Keine Standardkarte hat welche.

### Einstellungen ohne Wirkung

- `HexagonSettings.IsBuildable` wird jetzt geprüft: Eine Konstruktion auf einem nicht bebaubaren Hexagon ergibt `HexagonNotBuildable`.
- `HexagonSettings.NeutralArmy` wirkt, siehe oben.

## Offen bei den AI-Spielern

Die AI-Spieler fragen Kosten, Einkommen und Spielausgang jetzt bei der Engine. Mit Wachen gehen sie nur korrekt um: Sie zählen sie nicht zur gegnerischen Armee und führen sie im geglaubten Zustand mit. Ob sich ein Angriff auf eine Wache lohnt, wägen sie nicht ab. Behandelt in [[ai-opponents-too-weak]].

## Nicht entschieden

- **Wer Orders sammelt.** `Game.PlayTurn` bekommt die Orders aller Spieler auf einmal. Wie ein Client sie einsammelt (Hot-Seat, Netzwerk), ergibt sich aus [[ui-concept]].
- **Speichern.** Der Zustand folgt aus den Settings und den Orders aller Züge. Was davon gespeichert wird, behandelt [[game-persistence]].
- **Wirksame Werte pro Spieler.** `Costs` und `Harvest` sind die Stellen, an denen ein Upgrade auf Kosten oder Einkommen ansetzt. Ein solches Upgrade gibt es noch nicht, siehe [[upgrade-variety]].
