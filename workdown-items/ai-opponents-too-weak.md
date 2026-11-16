---
id: ai-opponents-too-weak
title: AI-Gegner spielen zu schwach
status: todo
created: 2026-11-14
updated: 2026-11-16
depends_on:
- upgrade-concept
- engine-rules-for-clients
- bot-strength-measurement
- bot-game-diagnosis
- client-possible-orders
- client-what-if
- opponent-estimate
- state-evaluation
---
## Problem

In einer UI soll man gegen Gegner spielen können, die nicht chancenlos sind. In einem früheren Test gegen die AI war sie sehr schlecht. Was genau schlecht war, ist nicht festgehalten.

Die Stärke der AI-Spieler wird bisher nur untereinander gemessen, in der Arena. Das sagt, welcher Bot die anderen schlägt, aber nicht, ob einer von ihnen einen Menschen fordert.

Dazu kommt, dass sich die Engine seit dem Bau der Spieler verändert hat und weiter verändert: Kämpfe laufen über `Combat.Fight`, die Bewegung über `Movement`, mit [[upgrade-concept]] bekommen die Kampfeinheiten Upgrades, und mit [[engine-rules-for-clients]] gibt die Engine weitere Regeln heraus. Die Spieler rechnen vieles davon noch selbst.

## Festgelegt

Am 2026-11-16:

- **Neuanfang.** Die vorhandenen Spieler, der Runner und die Arena werden entfernt und nicht umgebaut (siehe [[bot-strength-measurement]]).
- **Faire Bots.** Ein Bot bekommt nur die Sicht, die auch ein Mensch bekommt.
- **Zuerst zwei Spieler.**
- **Erst die Grundlagen, dann die Bots.** Dieses Item bleibt das Ziel. Davor stehen:

| Was fehlt | Item |
|---|---|
| Messen, ob ein Bot besser ist als ein anderer | [[bot-strength-measurement]] |
| Sehen, warum ein Bot verliert | [[bot-game-diagnosis]] |
| Fragen, was in einem Zug möglich ist | [[client-possible-orders]] |
| Fragen, was aus einem Zug folgt | [[client-what-if]] |
| Einschätzen, was der Gegner hat | [[opponent-estimate]] |
| Urteilen, wer vorne liegt | [[state-evaluation]] |

Nicht jeder Bot braucht alles davon. Ein einfacher Bot nach festen Regeln kann entstehen, sobald sich messen und fragen lässt, was möglich ist; er dient danach als Gegner, an dem sich jeder weitere messen muss.

## Funde

Stand 2026-11-14 an den alten Spielern. Sie werden entfernt; die Funde bleiben als Liste dessen, woran ein neuer Bot denken muss.

- Es gab sechs Spieler: `IdlePlayer`, `RandomPlayer` (mit vier Varianten), `StateMachinePlayer`, `CommanderPlayer`, `SimulatorPlayer` und `TacticianPlayer`.
- `CommanderPlayer`, `StateMachinePlayer` und `CompositionOptimizer` lasen `GameSettings.FightExponent` direkt. Sie schätzten Kämpfe ohne Upgrades ein, die eigenen wie die des Gegners.
- Nur `CommanderPlayer` und `RandomPlayer` forschten. `CommanderPlayer` nahm die erforschbaren Upgrades der Reihe nach, sobald genug Matter übrig war, ohne zu fragen, was ein Upgrade bringt.
- Die Auswertung der Settings (`GameSettingsAnalyzer`, `CompositionOptimizer`) las die Preise direkt statt über `Costs`.
- Neutrale Armeen (Wachen): Ob sich ein Angriff auf eine Wache lohnt, wog kein Spieler ab; `StateMachinePlayer` schickte Dots auch auf bewachte Expansionen.
- In einem Spiel mit mehr als zwei Spielern rechneten die Spieler nicht damit, dass ein Gegner besiegt ist und seine Armeen stehen bleiben.
- Die Spieler zogen nur auf Nachbarfelder (siehe [[movement-range]]). Eine größere Reichweite nutzten sie nicht.

## Offene Fragen

- Was heißt gut genug? Ein Maß für die Stärke gegen einen Menschen fehlt. Die Messung der Bots untereinander ist nur der Filter davor.
- Welche Arten von Bots braucht es: feste Regeln, Züge durchspielen, beides gemischt?
- Braucht ein Gegner für Menschen verschiedene Stärken oder verschiedene Spielweisen?
- Wird die AI vor oder nach der ersten spielbaren UI überarbeitet? Erst im Spiel gegen sie zeigt sich, was ihr fehlt. Andererseits macht ein Test der UI gegen chancenlose Gegner wenig Freude.

## Woran man es merkt

- Ein Mensch, der die Regeln kennt, kann gegen mindestens einen AI-Gegner verlieren.
- Die AI-Spieler fragen jede Regel bei der Engine und rechnen keine selbst.
- Die AI-Spieler berücksichtigen Upgrades, eigene und die, die sie vom Gegner kennen.
- Kein Bot hat einen einfachen Gegner, gegen den er klar verliert.
