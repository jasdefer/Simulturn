---
id: ai-opponents-too-weak
title: AI-Gegner spielen zu schwach
status: in_progress
created: 2026-11-14
updated: 2026-11-27
depends_on:
- upgrade-concept
- engine-rules-for-clients
- bot-strength-measurement
- bot-game-diagnosis
- fight-rule
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

## Der erste Bot nach Regeln

Stand 2026-11-27, aus dem Gespräch zum Start der Umsetzung.

Der erste Bot mit Plan ist ein einfacher Bot nach festen Regeln. Mit ihm gibt es die erste Paarung, in der beide Seiten verlieren können. Die Frage an die Arena: Schlägt saubere Wirtschaft den Zufall? An seinen Spielen wird auch nachgeprüft, was [[bot-game-diagnosis]] offen gelassen hat: die vier Ursachen aus dem ersten Lauf des Zufallsbots und ob sich für einen neuen Bot sagen lässt, woran er verliert.

### Welche Entscheidungen ein Bot trifft

Eine gesammelte Liste ist nie nachweislich vollständig. Die Entscheidungen sind deshalb aus dem Spiel abgeleitet:

- Von unten: Ein Bot gibt nur vier Arten von Orders ab (trainieren, bauen, forschen, bewegen), jede mit den Fragen was, wie viel, wo und wann. Jede Entscheidung endet in einer davon.
- Von der Mitte: Eine Entscheidung teilt ein knappes Mittel auf Verwendungen auf, die darum konkurrieren. Zwei Entscheidungen hängen zusammen, wenn sie am selben Mittel ziehen. „Wann" ist keine eigene Entscheidung, sondern die Bedingung, unter der eine Verwendung die andere schlägt.
- Von oben, als Gegenprobe: von Sieg (der Gegner hat kein Gebäude mehr) und Niederlage (selbst keines mehr) rückwärts, und an einzelnen Situationen.

| Knappes Mittel | Konkurrierende Verwendungen |
|---|---|
| Matter | Dots, Plane auf einem neuen Feld, Axis, Trainingsgebäude, Kampfeinheiten, Forschung, zurückhalten |
| Space | Dots oder Kampfeinheiten |
| Dots | abbauen, einen Bau halten, eine Forschung halten, zu einem neuen Feld laufen, nachsehen, mitkämpfen |
| Trainingsplätze | welche Einheit in welchem Gebäude, oder leer lassen |
| Standort der Gebäude | alles auf einem Feld oder verteilt |
| Standort jeder Armee | bleiben, sammeln, teilen, wohin ziehen, was angreifen, ausweichen |
| Sicht | wohin man sieht, und was man über das Ungesehene annimmt |

Daraus werden fünf Entscheidungen:

1. Wofür geht die nächste Matter? Sie fließt durch eine Kette aus Einkommen (Dots, Planes), Durchsatz (Trainingsgebäude) und Space (Axis); die Matter gehört in das Glied, das gerade bremst. Wirtschaft, Expansion, Produktion, Space und Forschung sind Verwendungen derselben Matter und keine getrennten Entscheidungen.
2. Welche Einheiten? Die Wahl fällt in zwei Stufen, erst mit dem Trainingsgebäude, dann mit der Einheit.
3. Wo stehen die Gebäude?
4. Was tut jede Armee?
5. Womit rechnet der Bot beim Gegner?

Die Gegenprobe hat Teilentscheidungen ergänzt, die in keiner ersten Liste standen:

- Suchen: Das Startfeld des Gegners steht in den Settings, jedes später gebaute Gebäude muss gefunden werden.
- Mindestgröße je Ziel: Der Schaden an Gebäuden sammelt sich nicht über Züge. Eine Armee, die die Armor des billigsten Gebäudes nicht in einem Zug bezahlt, zerstört nie etwas.
- Dranbleiben: nach dem ersten Abriss stehen bleiben und Neubauten anderswo verfolgen.
- Schluss: Wer kein Einkommen mehr hat, setzt alles ein, sonst endet das Spiel ohne Sieger.
- Vorwarnung: Einheiten sieht man nur ein Feld weit, also einen Zug vorher.
- Wiederaufbau: Er braucht einen Dot und Matter, die nicht auf dem angegriffenen Feld liegen.
- Umzug der Dots: Ein Feld läuft leer, die Dots müssen weiter.
- Nachschub: Neue Einheiten entstehen, während die Armee unterwegs ist.

Jede Entscheidung ist von einer von drei Arten: eine Rechnung (folgt aus den Settings und dem eigenen Zustand), eine Antwort auf den Gegner, oder eine Haltung (kein Richtig, nur Tempo und Risiko; ein Parameter, den die Arena misst). Die Entscheidungen 1 bis 3 sind großteils Rechnung, 4 und 5 hängen am Gegner.

### Entschieden

- **Aus den Settings gerechnet.** Die Standardwerte sind nicht durchdacht und werden sich noch stark ändern. Der Bot baut keine Zahl fest ein, sondern leitet seine Regeln aus den Settings ab. So zeigt er in der Arena auch, was ein Wert bewirkt.
- **Mit Vorlauf.** Eine Order wird für den Zug entschieden, in dem sie fertig wird, nicht für den jetzigen. Eine Axis muss stehen, bevor der Space ausgeht.
- **Nicht am Gegner orientiert.** Der Bot nutzt vom Gegner nur, wo dessen Gebäude stehen und wie viele Einheiten er auf einem Feld gesehen hat. Zusammensetzung, Upgrades, Schätzungen des Ungesehenen und Vorhersagen seiner Züge lässt er weg.
- **Noch keine gemeinsamen Helfer für ein Bild vom Gegner.** Angenommene Zustände und Schätzungen ([[opponent-estimate]], [[client-what-if]]) nützen vielen Bots und entstehen erst, wenn die Diagnose zeigt, dass der Bot daran verliert.
- **Gebäude zu Hause.**
- **Dots zählen im Kampf,** sind aber schwächer als Kampfeinheiten und haben keinen Exponenten. Bisher zählte ein Dot eine Stärke und war damit je Matter und je Space stärker als jede Kampfeinheit. Umgesetzt am 2026-11-27: Ein Dot zählt ein Viertel, der Wert steht in den Settings.

- **Mehr Matter je Feld.** Mit 1500 je Feld lohnte sich eine Expansion kaum. Umgesetzt am 2026-11-27: `Open` hat 10 000 je Feld, `Expansions` und `Guarded` 5000 am Start, 10 000 je Expansion und 15 000 in der Mitte. `HomeOnly` bleibt bei 12 000.

### Offen

- Das erste Trainingsgebäude wird gebaut, ohne etwas über den Gegner zu wissen. Fest oder gewürfelt? Eine feste Wahl lässt sich bei gleichzeitigen Zügen auskontern.
- Die Kampfregel ([[fight-rule]]). Die Rechnungen des Bots zu Armeen hängen an ihr. Stand 2026-11-28: Sie wird zuerst bearbeitet, der Bot danach, damit seine Arena-Zahlen von Anfang an für die neue Regel gelten.

### Zwischenstand: Die Standardwerte auf Plausibilität geprüft

Stand 2026-11-27. Bevor der Bot aus den Settings rechnet, wurden die Standardwerte durchgesehen. Die Dot-Stärke und die Matter je Feld sind behoben (siehe Entschieden). Offen sind:

Im Kampf: Die Regel kippt an einzelnen Einheiten, und das Aufrunden frisst den Exponenten. Die Funde und die durchgerechneten Varianten stehen in [[fight-rule]].

In der Wirtschaft:

- Eine Plane gibt 10 Space, ein Feld fasst aber 12 Abbauer.
- Der Dome kostet 100, Cube und Pyramid 150, ohne erkennbaren Grund.
- Upkeep gibt es noch nicht ([[upkeep]]).

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
