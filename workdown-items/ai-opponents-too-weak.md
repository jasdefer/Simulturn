---
id: ai-opponents-too-weak
title: AI-Gegner spielen zu schwach
status: todo
created: 2026-11-14
depends_on:
- upgrade-concept
- engine-rules-for-clients
---
## Problem

In einer UI soll man gegen Gegner spielen können, die nicht chancenlos sind. In einem früheren Test gegen die AI war sie sehr schlecht. Was genau schlecht war, ist nicht festgehalten.

Die Stärke der AI-Spieler wird bisher nur untereinander gemessen, in der Arena. Das sagt, welcher Bot die anderen schlägt, aber nicht, ob einer von ihnen einen Menschen fordert.

Dazu kommt, dass sich die Engine seit dem Bau der Spieler verändert hat und weiter verändert: Kämpfe laufen über `Combat.Fight`, die Bewegung über `Movement`, mit [[upgrade-concept]] bekommen die Kampfeinheiten Upgrades, und mit [[engine-rules-for-clients]] gibt die Engine weitere Regeln heraus. Die Spieler rechnen vieles davon noch selbst.

## Funde

Stand 2026-11-14, jede Stelle ist vor der Umsetzung neu zu prüfen.

- Es gibt sechs Spieler: `IdlePlayer`, `RandomPlayer` (mit vier Varianten), `StateMachinePlayer`, `CommanderPlayer`, `SimulatorPlayer` und `TacticianPlayer`.
- `CommanderPlayer`, `StateMachinePlayer` und `CompositionOptimizer` lesen `GameSettings.FightExponent` direkt. Sie schätzen Kämpfe ohne Upgrades ein, die eigenen wie die des Gegners.
- Nur `CommanderPlayer` und `RandomPlayer` forschen. `CommanderPlayer` nimmt die erforschbaren Upgrades der Reihe nach, sobald genug Matter übrig ist, ohne zu fragen, was ein Upgrade bringt.
- Die Spieler rechnen Kosten und Einkommen selbst (`TurnPlan`, `RandomPlayer`, `StateEvaluator`).
- Die Spieler ziehen nur auf Nachbarfelder (siehe [[movement-range]]). Bei einer größeren Reichweite nutzen sie sie nicht.

## Offene Fragen

- Was heißt gut genug? Ein Maß für die Stärke gegen einen Menschen fehlt.
- Was war im früheren Test schlecht: der Aufbau, die Wahl der Einheiten, das Ausweichen vor Kämpfen, der Angriff auf Gebäude?
- Reicht es, die vorhandenen Spieler anzupassen, oder braucht es einen neuen Ansatz?
- Braucht es alle sechs Spieler weiter? Einige dienen den Tests und der Arena, nicht als Gegner.
- Wird die AI vor oder nach der ersten spielbaren UI überarbeitet? Erst im Spiel gegen sie zeigt sich, was ihr fehlt. Andererseits macht ein Test der UI gegen chancenlose Gegner wenig Freude.

## Woran man es merkt

- Ein Mensch, der die Regeln kennt, kann gegen mindestens einen AI-Gegner verlieren.
- Die AI-Spieler fragen jede Regel bei der Engine und rechnen keine selbst.
- Die AI-Spieler berücksichtigen Upgrades, eigene und die, die sie vom Gegner kennen.
