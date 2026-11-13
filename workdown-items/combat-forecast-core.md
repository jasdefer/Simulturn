---
id: combat-forecast-core
title: Kampfausgang vorab berechnen können
status: done
created: 2026-10-14
updated: 2026-11-13
completed: 2026-11-13
---
## Problem

Die alte UI brauchte eine Kampfprognose für den Spieler und hat die Mathematik kopiert (`Simulturn.Web/Services/CombatForecast.cs`). Zwei Implementierungen derselben Regel driften auseinander.

Seit [[split-game-state]] ist die Regel für zwei Armeen als `Combat.Fight` erreichbar. Zwei Teile der Regel lagen aber weiter privat in `TurnResolution`: das Zusammenrechnen bei mehr als zwei Armeen und die Wirkung der Upgrades auf den Exponenten. Ein Client hätte beides nachbauen müssen.

## Ziel

Ein Client kann für die Armeen eines Hexagons den Ausgang eines Kampfes vorab bestimmen, und zwar garantiert mit derselben Rechnung, die `TurnResolution.Resolve` anwendet.

## Entscheidungen

- **Ein Eingang.** `Combat.Fight(settings, combatants)` nimmt pro Spieler-ID einen `Combatant` (Armee und Upgrade-Stufen) und liefert ein `FightReport`. Die Zugauflösung übergibt die echten Stufen, eine Prognose die Stufen, die ihr Spieler kennt. Ein unbekanntes Upgrade fehlt in der Eingabe und der Kampf rechnet mit dem Basiswert. Die Rechnung für zwei Armeen ist privat, niemand rechnet an Upgrades und Deckel vorbei.
- **Upgrades wirken in der Kampfregel.** Was ein Upgrade im Kampf bewirkt, steht in `Combat` (`Combat.GetExponent`), nicht mehr in `PlayerState`.
- **Mehr als zwei Armeen.** Jede Armee kämpft gegen jede andere mit voller Stärke. Die Verluste einer Armee addieren sich über ihre Kämpfe und enden pro Einheitentyp beim Bestand. Es überlebt höchstens die stärkste Armee, und nur, wenn die anderen sie zusammen nicht aufreiben. Wer als Schwächerer dazustößt, hilft den Stärksten zu töten und stirbt selbst. Vorher konnte der Bestand negativ werden.
- **Fog of War liegt beim Aufrufer.** `Combat` kennt weder Spielzustand noch Sicht. Wer nur ein `PlayerGameState` hat, kann nichts Verbotenes übergeben. Die gegnerische Zusammensetzung ist eine Annahme des Clients; eine Hilfsfunktion von der Sicht zur Prognose gibt es bewusst nicht.

## Akzeptanzkriterien

- Prognose und tatsächliche Auflösung liefern für dieselben Armeen und Upgrade-Stufen identische Verluste aller Seiten. `StateInvariants.CheckTransition` rechnet jeden Kampf eines Zuges nach.
- Die Kampfregel existiert genau einmal im Code.
- Fog of War bleibt gewahrt: Die Prognose eines Spielers beruht nur auf dem, was dessen Spieleransicht kennt.
- Kein Kampf nimmt einer Armee mehr, als sie hat.

## Nicht Teil dieses Items

- Die AI-Spieler vergleichen weiter `Army.GetStrengthOver` mit dem Basis-Exponenten. Die Umstellung ändert ihr Spielverhalten und ist eigene Arbeit.
- Was ein Spieler über die Upgrades eines Gegners erfährt und wie Upgrades modelliert sind: [[upgrade-concept]].

## Referenz

`CombatForecast.cs` im Tag `archive/add-web-ui` (ehemaliger Branch) zur Orientierung, nicht als Vorlage.
