# Spieltest: Zufriedenheit nach dem Original

Arbeitsblatt für Phase E2 aus [plan-cityCitizenService.impl.md](plan-cityCitizenService.impl.md).
Zum Abhaken beim Spielen gedacht. Auf Deutsch, weil es eine Arbeitsanweisung ist.

Alle Rechenregeln stehen im Plan, hier steht nur, was zu tun und was zu erwarten ist.

## Vorbereitung

- [ ] Profildatei sichern. Sie liegt unter `%LOCALAPPDATA%\CivOne\default.profile` beziehungsweise
      `~/.local/share/CivOne/default.profile`.
- [ ] Schalter einschalten: Hauptmenü, *Game Settings*, *Patches*, Eintrag **Original city happiness** auf *Yes*.
      Er darf mitten im Spiel umgelegt werden, weil die Zufriedenheit bei jedem Zugriff neu gerechnet und
      nirgends gespeichert wird.
- [ ] Kontrolle, dass der Schalter wirkt: eine Stadt der Größe vier mit zwei Entertainern auf König-Stufe unter
      Despotismus. Alt sind es zwei zufriedene Bürger, neu ein zufriedener und ein genügsamer.
- [ ] Spielstand A: mindestens acht Städte, kurz vor einer weiteren Gründung.
- [ ] Spielstand B: mindestens zwanzig Städte.
- [ ] Beide Stände einmal mit altem und einmal mit neuem Modell öffnen, ohne einen Zug zu machen, und die
      Stadtübersicht vergleichen.

Eine Tabelle mit Stadt, Größe, Regierung, Städtezahl, zufrieden, genügsam, unzufrieden reicht als Mitschrift.

---

## 1. Die geparkte Unzufriedenheit

Der schwierigste Punkt, weil dasselbe Gebäude je nach Lage unterschiedlich viel bringt. Rote Hemden in der
Stadtansicht zeigen an, dass etwas geparkt ist.

Die Regel: unsichtbar Geparktes und sichtbar Unzufriedene werden nach jeder Änderung gegeneinander
ausgeglichen, bis die sichtbare Seite nicht mehr die kleinere ist. Die Summe bleibt dabei gleich. Deshalb
hängt die Wirkung eines Kolosseums davon ab, wie voll der Parkplatz ist.

| Lage | Erwartung | Beispiel mit einem Kolosseum |
|---|---|---|
| wenige rote Hemden | wirkt vollständig | Größe 10, 10 unzufrieden, 4 geparkt → 7 unzufrieden |
| etwa gleich viele | wirkt teilweise | 10 unzufrieden, 9 geparkt → 8 unzufrieden |
| viele rote Hemden | wirkt gar nicht sichtbar | 10 unzufrieden, 15 geparkt → bleibt 10, geparkt sinkt auf 11 |

- [ ] Große Zivilisation auf Kaiser, deutlich mehr Städte als die Basis erlaubt, dazu eine Stadt mit vielen
      roten Hemden.
- [ ] Kolosseum bauen. Notieren, wie viele der drei Punkte ankommen, und mit der Tabelle vergleichen.
- [ ] Dasselbe in einer Stadt mit wenigen roten Hemden. Dort müssen alle drei Punkte ankommen.
- [ ] Shakespeare-Theater in einem großen Reich. Es ist **nicht** absolut: es setzt die Unzufriedenen auf null,
      der Ausgleich holt danach etwa die Hälfte des Geparkten zurück.

Weicht etwas von der Tabelle ab, ist das ein Fund und kein Spielgefühl. Dann bitte notieren, in welcher Stadt
und mit welchen Zahlen, bevor am Code etwas geändert wird.

---

## 2. Die Kathedrale

Hier ändert sich am deutlichsten, was ein Spieler sieht.

- [ ] Kathedrale ohne Michelangelo: genau vier Bürger werden genügsam.
- [ ] Michelangelos Kapelle bauen. Danach sind es sechs, und zwar in **jeder** Stadt des Besitzers, auch auf
      anderen Kontinenten. Das ist die eigentliche Änderung, vorher wirkte die Kapelle nur auf dem eigenen
      Kontinent.
- [ ] Eine Stadt auf einem anderen Kontinent als die Kapelle prüfen. Auch dort sechs.
- [ ] Zum Vergleich J. S. Bachs Kathedrale: die ist weiterhin an den Kontinent gebunden und darf eine Stadt
      jenseits des Meeres **nicht** erreichen.

### Der Sonderfall: eroberte Städte

Die Kathedrale wirkt nur, wenn der Besitzer **Religion** kennt. Selbst bauen kann man sie ohnehin nur damit,
der Fall entsteht also erst durch Eroberung. Beim Tempel gilt dasselbe mit **Ceremonial Burial**, beim
Kolosseum gar nichts.

- [ ] Eine gegnerische Stadt mit Kathedrale erobern, bevor man selbst Religion hat. Die Kathedrale bewirkt
      nichts.
- [ ] Religion erforschen. Sie wirkt ab sofort, rückwirkend und ohne Zutun.
- [ ] Dasselbe mit einem Tempel und Ceremonial Burial.
- [ ] Ein erobertes Kolosseum dagegen wirkt sofort, auch ohne Construction.
- [ ] Mysticism erforschen. Alle eigenen Tempel werden gleichzeitig doppelt so wirksam.

---

## 3. Die zehn Aufrufer

Die Zufriedenheit wird an zehn Stellen gelesen, nicht nur in der Stadtansicht. Ein Modell, das in der
Stadtansicht stimmt, kann die KI trotzdem schlecht spielen lassen.

- [ ] Stadtansicht: Bürgerreihe, Aufstandsmeldung, Wir-lieben-den-König-Tag.
- [ ] Stadtübersicht und die Liste der Städte.
- [ ] Ein paar Züge mit sichtbarer KI spielen. Bauen die Computergegner weiterhin sinnvoll, oder versinken
      ihre Städte reihenweise im Aufstand?
- [ ] Regierungswechsel von Despotismus über Monarchie zur Republik. Die Strafe muss spürbar nachlassen.
- [ ] Ein Spiel speichern und wieder laden. Die Zahlen müssen gleich bleiben, weil nichts davon gespeichert wird.

---

## Wenn alles passt

Zurück in den Plan: E2 abhaken, dann E3, also `OriginalHappinessModel` in `Settings.cs` auf ein als
Voreinstellung. Danach folgt Phase F, das Zusammenfalten der beiden Klassenpaare.
