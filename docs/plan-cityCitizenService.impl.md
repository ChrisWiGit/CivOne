# Plan: City Happiness — Original Model

## Instructions

* .claude\csharp.instructions.md
* .claude\CLAUDE.md

## Summary

Implementation plan for `CityCitizenService` and the types around it.

This document is **self-contained**. It was split out of
[plan-civilizationScore.impl.md](plan-civilizationScore.impl.md), which needed a corrected happiness model as the
input to its score formula — but the work below stands on its own and ships on its own, whether or not the score
port ever lands.

Facts about the original DOS game come from a separate decompilation repository. File and line references into
that repository are deliberately **not** reproduced here; they do not apply to CivOne. Every CivOne-side
statement was verified against the current working tree.

## Progress checklist

Tick as you go. Steps map to §9 *Steps*; the commit letters map to §9 *Delivery*.

### Phase A — preparation (commit a)

- [x] A1 Run `CityCitizenServiceImplTests` and `CityHappy` and record the green baseline (§9 step 0a).
- [x] A2 Make `CalculateCityStats`, `Stage1`–`Stage5`, `ApplyEmperorEffects`, `ApplyDemocracyEffects` `virtual` — behaviour-neutral, no test touched (§9 step 0b). Later extended by `ApplyBuildingEffects` and `ApplyWonderEffects` for C7.
- [x] A3 Add `GetCityIndex(ICityBasic)` to `IGameCitizenDependency`, backed by the `Game._cities` position, not persisted (§8).
- [x] A4 Add the setting `OriginalHappinessModel` (default **off**) (§7). The second setting this step
  originally added, `PendingUnhappinessMode`, was removed again when Q1 was settled (§6).
- [x] A5 Add both settings to the Patches menu in `Setup.cs`, so the play test of Phase E can toggle them in
  game instead of hand-editing `default.profile`. Switching mid-game is safe: happiness is recomputed on every
  access and never persisted.

### Phase B — the parallel subclass (commit b)

Implementation notes from the branch:

- The pending unhappiness is reset in `Stage1`, not in the two entry points. Both `GetCitizenTypes()` and
  `EnumerateCitizens()` begin there, so one reset covers the trap from §7 by construction.
- Red shirts are painted in the last normalisation only. Painting them earlier would charge the extra cost
  of a red shirt once per stage, on top of the pending unhappiness that already models the same thing. Recorded as an
  addition to deviation 1 in §10.
- `Normalise` works on counters and writes the result back into the citizen array, in the order happy,
  content, unhappy, which is the order the base class produces as well. The citizens standing for the
  parked unhappiness are the last ones, so luxuries reach the ordinary unhappy citizens first.
- The human/AI split of §5 is implemented, via `IsHumanPlayer` on the new `IGameCityQuery` interface.

- [x] B1 Create `OriginalCityCitizenService : CityCitizenService`; factory `ICityCitizenService.Create` selects it by setting, still off (§7).
- [x] B2 `base` and `E` from government, city count and city index; lower clamp **after** `E`; human/AI split optional (§9 steps 1, 5-optional).
- [x] B3 Replace `ApplyEmperorEffects` with `E` in the subclass — not both (§9 step 2).
- [x] B4 Pending unhappiness as a private field on the subclass, driving the red shirts; reset it at the top of `GetCitizenTypes()` **and** `EnumerateCitizens()` (§4, §7 traps).
- [x] B5 The refill delegate, wired in the factory (§6). Shipped as two competing readings while Q1 was open,
  reduced to `EqualisingPendingUnhappinessDelegate` once Q1 was settled.
- [x] B6 `Normalise` at all five stage boundaries: `Stage1` rewritten in full, `Stage2`–`Stage5` as `base.StageN(...)` + `Normalise(...)` (§9 step 4).


### Zwischenstand


### Phase C — corrections that stand on their own (commit c)

Implementation notes from the branch:

- **The corrections land in the subclass, not in the base class.** Step 5 and step 7 read as if they change
  `CityCitizenService` itself, but three of the six tests listed in §9 step 0 pin exactly the behaviour they
  change, and those tests must stay green until the cleanup. So `CathedralDelta` and `ApplyDemocracyEffects`
  are overridden in `OriginalCityCitizenService`. The base class is untouched, and the corrections reach the
  game together with the rest of the model when the switch flips.
- The luxury chain needed two new members on `ICityBasic`, because the interface carried no trade at all:
  `TradeTotalGross` (trade including trade routes, before any corruption) and `LuxuryCorruption`. See Q2.
- Stage 2 no longer reads `City.Luxuries`. The original sets the happy count from the luxuries outright and
  lets the normalisation push the unhappy count down to make room, so the override sets the count rather
  than upgrading citizens one at a time.
- The Cathedral entry in the building list of the city screen is still added by the base class even when
  Religion is missing. Only the effect is gated. That is display state, not part of the model.

- [x] C1 `CathedralDelta`: −4 base / −6 with Michelangelo, drop the continent check, gate the block on the advance Religion (§9 step 5).
- [x] C2 Answer Q2, then extend the luxury chain: corruption, `+5` inside the numerator, raw-trade clamp, entertainers before Marketplace and Bank (§9 step 6).
- [x] C3 Women's Suffrage term in war weariness; make the martial-law-XOR-weariness split in stage 4 explicit (§9 step 7).
- [x] C4 Answer Q3: wonder effect follows a captured city, obsolescence checked against all civilizations (§9 step 8).
- [x] C5 Luxury rate as a parameter, defaulting to the player's real rate (§9 step 9, H7).
- [x] C6 War weariness corrected after a second pass over the original (§1 *War weariness*): air units count while
  at home, ships and land units do not; the effect is uncapped and raises the unhappy counter instead of
  downgrading citizens through `DowngradeCitizens`. `ApplyDemocracyEffects` is replaced in the subclass by a
  counting `WarWeariness`, and `Stage4` hands the result to `Normalise`, mirroring stage 2.
- [x] C7 Shakespeare's Theatre and J.S. Bach's Cathedral moved from stage 3 to stage 5, where the original has
  them (§1 *Stage order*). Both sat in `ApplyBuildingEffects`, so they ran **before** martial law and war
  weariness instead of after. Consequences: the theatre now wipes out the weariness of its city as the original
  does, and both effects are rolled back by one refill instead of three. `ApplyBuildingEffects` and
  `ApplyWonderEffects` are overridden in the subclass; the base class only gained `virtual`.
  The base class's early `return` after Shakespeare — which skipped temple, colosseum and cathedral entirely —
  is **not** reproduced: in the original every building still takes effect, Shakespeare simply leaves nothing
  for them to convert two stages later.
- [x] C8 `OriginalCityEconomyService : CityEconomyServiceImpl` for the specialist order in the luxury and tax
  chain. The original adds entertainers and taxmen **before** the marketplace and the bank raise the result;
  `CityEconomyServiceImpl` adds them afterwards, so a specialist stays a flat two points. The subclass is
  selected by the **same** `OriginalHappinessModel` setting, so the city screen and the reports cannot
  disagree. `CityEconomyServiceImpl` was unsealed and the two methods made `virtual`; its own tests are
  untouched. Note this is a second hierarchy — the switch now spans two class pairs, which the cleanup in §7
  has to fold back as well.
- [x] C9 Parked unhappiness was measured against the **seats** (size minus specialists) instead of against the
  **city size**. A city whose base unhappiness fit into the city but not into its seats parked the difference,
  and that surplus then shielded the happy citizens from the very first normalisation — every crowded city
  showed one happy citizen too many. Found by loading a real save and comparing stage by stage against the
  decompilation. `Stage1` now parks only `max(BaseUnhappy - Size, 0)` and hands the **raw** count to
  `Normalise`, so the squeeze into the seats happens there.
- [x] C10 `OriginalCityEconomyService` double-counted the marketplace on entertainers: CivOne's
  `City.EntertainerLuxuries` is `Entertainers * 3`, the marketplace bonus already folded into the constant, and
  C8 raised that by another fifty percent. Fixed with a `EntertainerLuxuryPoints` seam on the base class; the
  original service returns the unraised `Entertainers * 2` and scales it itself.
- [x] C11 Temple advance gate. CivOne's temple works without Ceremonial Burial and doubles with Mysticism; the
  original gives `2` with Mysticism, `1` with Ceremonial Burial and `0` without either, adding the oracle on top
  rather than doubling. Same class of defect as the cathedral's missing Religion gate in C1, found the same way
  — by writing a combination test and checking its expectation against the decompilation.

### Phase D — tests (§12)

Two suites carry this: `OriginalCityCitizenServiceTests` for the model itself, against mocks, and
`OriginalCityHappy` as the integration sibling of `CityHappy.cs`, against a real game.

Writing D2 surfaced a defect in the refill of Phase B and it is fixed: the refill had no upper bound, so the
full drain reading emptied the pending unhappiness into a count that the clamp right behind it threw away
again. That made the full drain **weaker** than the halving, the opposite of what §6 describes. Both
delegates now take the highest unhappy count the city can show and stop there.

- [x] D1 `OriginalCityCitizenServiceTests` — base term per difficulty and government, `E = 0` agreement zone, boundary per row, staggering, clamp ordering.
- [x] D2 Pending unhappiness tests in **both** refill variants, including Shakespeare's Theatre.
- [x] D3 Stage-order test: an overshooting modifier loses its surplus at its own `N`.
- [x] D4 Modifier tests: Cathedral, Michelangelo across continents, luxury chain, Women's Suffrage, each wonder in isolation, the luxury-rate parameter.
- [x] D5 The pending-unhappiness-reset test: `GetCitizenTypes()` followed by `EnumerateCitizens()` on one instance.
- [x] D6 `GetCityIndex` stability test pinning index → penalised city.
- [x] D7 Corrected-model sibling of `CityHappy.cs`, eleven cases re-derived by hand.

Added on top of the list the plan started with:

- [x] D8 Regression test that Women's Suffrage never raises war weariness (required by §9 step 7).
- [x] D9 Cathedral with and without Religion and with and without Michelangelo, across continents (§9 step 5).
- [x] D10 The luxury chain order and the luxury rate parameter (§9 steps 6 and 9).
- [x] D11 The refill never pushes the unhappy count past what the city can show.
- [x] D12 War weariness: only air units are weary while at home (air / sea / land), and the effect raises the
  unhappy count rather than downgrading happy citizens. Both fail on the pre-C6 code.
- [x] D13 Stage placement: Shakespeare's Theatre wipes out the war weariness, and J.S. Bach's Cathedral is
  applied after it as well. Both fail on the pre-C7 code.
- [x] D14 `OriginalCityEconomyServiceTests` — entertainers and taxmen raised by marketplace and bank, both
  services agree without specialists, and the shipped order is pinned a second time so a change to the base
  shows up as a difference rather than silently.
- [x] D15 Regression for C9: the Samarkand constellation from a real save — size 18, Warlord, despotism,
  marketplace, six entertainers — yields four happy, one content, seven unhappy. The pre-C9 code gives five
  happy. Verified against the decompilation's own walk-through of the normalisation.
- [x] D16 Regression for C10: entertainer points enter the chain unraised, so a marketplace cannot be counted
  twice.
- [x] D17 Modifiers **in combination**, so the consolidation of Phase F cannot change a total unnoticed: the
  three stage-3 buildings together, the cathedral with J.S. Bach's Cathedral across their stages, Shakespeare
  leaving nothing for Bach, and the happiness wonders beside a colosseum.
- [x] D18 `EveryConstellationKeepsTheCitizenInvariants` — 4140 constellations, invariants on all five stages.
  Catches crashes and impossible counts in edge cases; it does **not** catch a value that is merely wrong,
  see F2.
- [x] D19 Regression for C11: the temple across all four advance combinations, the oracle with and without each
  of them, and the oracle without a temple. Two of the eight rows fail on the pre-C11 code.
- [x] D20 `OriginalCityHappy` builds its temples together with Ceremonial Burial, the advance a real game
  requires to build one at all (`Temple.RequiredTech`). Three of its cases had constructed a state that cannot
  occur in play — a temple owned by a civilization without the advance — and C11 exposed it. The idle-temple
  case that *can* occur, a captured city, is covered by the unit tests instead.

**Phase D is where this piece of work ends.** With D7 ticked, the branch is complete and shippable: the
corrected model exists, is covered by tests, and is switched off. Stop there.

### Phase E — flip and cleanup (deferred, **not part of this work**)

Phase E waits for a long manual play-test phase in the game and is therefore carried out later, in its own
branch and its own PR. It is listed here so the remaining debt stays visible, not as work to start now. See
§7 *Time-box*. The play test itself is written out in **§13 Manueller Testplan**, including where the switch
is turned on.

- [x] E1 Q1 settled — from the decompilation after all, not from a DOSBox run (§6). No experiment needed.
- [ ] E2 Play test the corrected model. Manual work. The working sheet to tick off while playing is
  [playtest-cityHappiness.md](playtest-cityHappiness.md); §13 holds the longer version. The blocks in §13.3:
  - [ ] E2.1 Empire penalty (§13.3 A) — it must arrive city by city, not everywhere at once.
  - [ ] E2.2 Pending unhappiness (§13.3 B) — compare a colosseum against the table in §6.
  - [ ] E2.3 Cathedral (§13.3 C) — nothing without Religion, global with Michelangelo. The two changes a
        player notices.
  - [ ] E2.4 War weariness (§13.3 D) — never worse with Women's Suffrage, never together with martial law.
  - [ ] E2.5 Luxury (§13.3 E).
  - [ ] E2.6 The ten callers of `GetCitizenTypes()` still look sensible (§13.4).
- [ ] E3 Flip `OriginalHappinessModel` to **on** — the default in `Settings.cs`, not just the menu.
- [ ] E4 Cleanup — see **Phase F**.

### Phase F — consolidation (deferred, follows E3)

Folding two class pairs back into one is the step where a silent regression is most likely: the tests that
guarded the old behaviour are being deleted at the same moment the new behaviour becomes the only one. The list
below exists so that deletion is a decision per test, not a sweep.

#### F1 — What happens to each of the 35 tests in `CityCitizenServiceImplTests`

The criterion is whether the subclass **overrode** the member, not whether the test is old.

**Keep unchanged (~20).** They cover members the subclass inherits untouched, so they keep their value after the
fold-in: `UpgradeCitizen(s)`, `UnhappyToContent`, `ContentToHappy`, `DowngradeCitizen(s)`, `CitizenByIndex`,
`AdaptCitizens`, `InitCitizens`, `InitSpecialists`, `CountCitizenTypes`, `CreateCitizenTypes`, `StageBasic`,
`WearRedShirt`, `ApplyMartialLaw`, `HasBachsCathedral`, `IsHappy`, `IsUnhappy`.

**Delete — the replacement already exists.** Check the replacement is green *before* deleting, not after:

| deleted | replaced by |
|---|---|
| `ApplyEmperorEffectsTests`, `NumberOfRedShirtsTests` | `EmpireSize*`, `WhatDoesNotFitIntoTheCityStaysPending` |
| `CathedralDeltaTest`, `ApplyBuildingEffectsTestsCathedrals` | `CathedralNeedsReligionAndIsRaisedByMichelangelo` |
| `ApplyBuildingEffectsTestsWithTemple` | `TempleAndOracle` |
| `ApplyBuildingEffectsTestsColosseum` | `ColosseumMakesThreeCitizensContent` |
| `ApplyBuildingEffectsTestsWithShakespearesTheatre` | `ShakespeareIsAbsolute…`, `ShakespeareIsNotAbsolute…`, `ShakespeareWipesOutTheWarWeariness` |
| `ApplyWonderEffectsTests`, `ApplyWonderEffectsNoWondersEffectsTests` | `HangingGardens…`, `CureForCancer…`, `TheTwoHappinessWondersAddUp` |
| `ApplyDemocracyEffectsTests` | `WomensSuffrageNeverAddsWarWeariness`, `OnlyAirUnitsAreWearyWhileAtHome`, `WarWearinessRaisesTheUnhappyCount…` |
| `CityHappy.cs` (11 cases) | `OriginalCityHappy.cs` (12 cases) |

**Adjust the expectations, do not delete (4).** These have no counterpart in the new suite, and writing one
would just be a rename. They stay, with the numbers the corrected model produces:

| test | why it stays |
|---|---|
| `GetCitizenTypesTests`, `EnumerateCitizensTests` | The only mock-based runs through all five stages with all three specialist kinds at once. `BothEntryPointsAgreeOnTheSameInstance` and `EveryStageKeepsTheCitizenCount` cover the entry points but pin no values, and `OriginalCityHappy` never uses scientists or taxmen. Recompute the four expected counts by hand from §1 before changing them, not from what the code prints. |
| `ApplyBuildingEffectsTestsNoEffects`, `ApplyWonderEffectsNoWondersEffectsTests` | The negative cases: a city without buildings and without wonders must come out unchanged. The new suite only ever tests modifiers that are present, so nothing there would notice a modifier that fires unconditionally. |

`CityCitizenServiceImplPerformanceTests` constructs the service directly and only asserts on a time budget, so
the fold-in leaves it alone. Re-run it once afterwards: the corrected `Stage1` normalises on every stage
boundary, which is more work per call than the shipped model does.

**Delete without replacement (1).** `CalculateCityStatsAllCases` — the corrected `Stage1` never calls
`CalculateCityStats`, so the method itself goes with the fold-in. Its coverage lives on in
`BaseUnhappyGrowsWithDifficulty` and the `EmpireSize*` tests. Its three `difficulty 5` rows were testing a level
the original does not have at all (§2).

#### F2 — What protects the fold-in

Deleting the old tests removes the only thing that currently notices an accidental behaviour change. Three
things take over, and all three must be green before and after every commit of this phase:

- **The combination tests.** Single modifiers are pinned individually, but the consolidation moves effects
  between classes and stages, and a combination that is only ever tested one effect at a time can change its
  total unnoticed. Covered: temple with the oracle, the three stage-3 buildings together, the cathedral with
  J.S. Bach's Cathedral *across* their stages, Shakespeare leaving nothing for Bach, the happiness wonders
  beside a colosseum, marketplace with bank.
- **The invariant sweep** `EveryConstellationKeepsTheCitizenInvariants` — 4140 constellations (3 governments ×
  6 difficulties × sizes 1–20 × every specialist count), asserting on every one of the five stages that the
  citizen count matches the city size, that no count is negative, and that happy plus unhappy never exceeds the
  seats. It needs no hand-computed expectations, which is why it can be this wide.
- **`OriginalCityHappy.cs`** as the end-to-end reference against a real game.

Be clear about what this does **not** catch: the sweep checks invariants, not values. C9 — the parked
unhappiness measured against the seats — produced entirely plausible, invariant-respecting numbers that were
merely one too high. Nothing in this suite would have found it. Only an external reference did: a real save,
compared stage by stage against the decompilation. If Phase F changes anything about the stage boundaries or the
parking, repeat that comparison rather than trusting green tests.

#### F3 — Steps

- [ ] F3.1 Fold `OriginalCityCitizenService` into `CityCitizenService`, one overridden member at a time, running
      the suite between each.
- [ ] F3.2 Fold `OriginalCityEconomyService` into `CityEconomyServiceImpl`; re-seal the class and drop the
      `virtual` markers that A2 and C7 added and nothing needs any more.
- [ ] F3.3 Delete `OriginalHappinessModel`, its Patches menu entry and the factory branch. The second setting
      and its menu entry are already gone with Q1.
- [ ] F3.4 Work the F1 table: delete the superseded tests, keep the rest, rename the `Original*` suites to the
      plain names.
- [ ] F3.5 Re-run the save-file comparison from C9 one last time against the consolidated code.

### Zwischenstand

Stand nach Phase A bis D. Alle vier sind fertig und grün. Offen ist nur noch die zurückgestellte Phase E.

**Was entstanden ist**

| Bereich | Datei |
|---|---|
| Basisklasse geöffnet | [CityCitizenService.cs](../src/Screens/Services/CityCitizenService.cs) |
| Neues Modell | [OriginalCityCitizenService.cs](../src/Screens/Services/OriginalCityCitizenService.cs) |
| Refill-Delegate | [EqualisingPendingUnhappinessDelegate.cs](../src/Screens/Services/EqualisingPendingUnhappinessDelegate.cs) |
| Auswahl | [ICityCitizenService.cs](../src/Screens/Services/ICityCitizenService.cs) |
| Einstellungen | [Settings.cs](../src/Settings.cs) |
| Stadt-Slot und Menschspieler | [IGame.cs](../src/IGame.cs), [Game.cs](../src/Game.cs) |
| Handel und Korruption für Luxus | [ICityBasic.cs](../src/ICityBasic.cs), [City.cs](../src/City.cs) |
| Tests am Modell | [OriginalCityCitizenServiceTests.cs](../xunit/src/OriginalCityCitizenServiceTests.cs) |
| Integrationstests | [OriginalCityHappy.cs](../xunit/src/OriginalCityHappy.cs) |

Die acht Member sind virtuell, dazu kamen geschützte Zugriffe auf Stadt, Spielstand und Spezialisten. Das ist
rein additiv, kein bestehender Test wurde angefasst.

**Drei Umsetzungsentscheidungen, die vom Plan abweichen**

- Der Reset der ausstehenden Unzufriedenheit sitzt in `Stage1` statt in beiden Einstiegspunkten. Beide starten dort, also deckt ein
  Reset die Falle aus Kapitel 7 strukturell ab.
- Rote Hemden werden nur in der letzten Normalisierung gemalt. Früher gemalt würden sie ihren doppelten
  Aufwand pro Stufe zusätzlich zur ausstehenden Unzufriedenheit kosten, also dieselbe Strafe zweimal.
- Der Mensch-KI-Unterschied aus Kapitel 5 ist drin, über `IsHumanPlayer` auf der neuen Abfrageschnittstelle.
  Er war als optional markiert, war aber genauso billig wie der Stadt-Index.
- Die Korrekturen aus Phase C liegen in der Unterklasse, nicht in der Basisklasse. Drei der sechs Tests aus
  Kapitel 9 Schritt 0 halten genau das Verhalten fest, das diese Korrekturen ändern. Lägen sie in der
  Basisklasse, wären diese Tests sofort rot, und genau das soll die Unterklasse verhindern.
- Die Luxuskette braucht zwei neue Mitglieder auf `ICityBasic`, weil die Schnittstelle bisher gar keinen
  Handel kannte. `TradeTotalGross` ist der Handel vor jeder Korruption, `LuxuryCorruption` die Korruption
  nach den Regeln des Originals, also ohne Palast und mit halber Wirkung durch das Gerichtsgebäude.
- Stufe 2 liest `City.Luxuries` nicht mehr. Das Original setzt die Zahl der zufriedenen Bürger direkt aus dem
  Luxus und lässt die Normalisierung die unzufriedenen verdrängen. Genau so beendet Luxus einen Aufstand.
- Deity erbt die Emperor-Zeile. Für den Empire-Term wird die Schwierigkeit auf vier begrenzt, siehe Q4.

**Was Phase C inhaltlich ändert**

- Kathedrale: minus vier als Grundwert, minus sechs mit Michelangelo, die Kontinentprüfung entfällt, und der
  ganze Block hängt an der Technologie Religion.
- Kriegsmüdigkeit: ein Punkt pro Einheit außer Haus, zwei in der Demokratie, minus einen durch das
  Frauenwahlrecht. Das Wunder kann Unzufriedenheit nie erhöhen.
- Stufe 4 wählt jetzt sichtbar entweder Kriegsrecht oder Kriegsmüdigkeit, nie beides.
- Luxuskette in der Reihenfolge des Originals, mit der Fünf im Zähler, der Deckelung auf den rohen Handel und
  den Entertainern vor Marktplatz und Bank.
- Luxussatz als Parameter an `ICityCitizenService.Create`, standardmäßig der Satz des Spielers.
- Deity nutzt für die Empire-Strafe dieselbe Basis wie Emperor. Der Teil `Size + Schwierigkeit − 6` behält
  die echte Schwierigkeit, Deity hat dort also weiterhin einen zufriedenen Bürger weniger.

**Was Phase D gefunden hat**

Beim Schreiben der Tests zur ausstehenden Unzufriedenheit kam ein Fehler aus Phase B ans Licht. Der Rückfluss
hatte keine Obergrenze. Die Full-Drain-Lesart schob deshalb alles Ausstehende sofort in die sichtbare Zahl,
und die Deckelung direkt dahinter warf es wieder weg. Damit war Full Drain schwächer als Halving, also genau
das Gegenteil dessen, was Kapitel 6 beschreibt. Beide Delegates bekamen daraufhin die höchste Zahl, die die
Stadt zeigen kann, als Grenze.

Nachtrag aus der Klärung von Q1: die Grenze war die falsche Abhilfe. Sie hat einen Fehler in der einen
Lesart repariert, der nur deshalb auftrat, weil die andere Lesart überhaupt existierte. Im Original steht die
Deckelung erst hinter dem Rückfluss, und der Rückfluss selbst kennt keine Grenze. Die Grenze ist mit Q1
wieder entfernt worden, zusammen mit Full Drain. Zur Lehre daraus siehe §6.

Zwei der elf Integrationsfälle fallen im neuen Modell anders aus als im alten. Beide Male ist der Grund
derselbe. Das Original setzt die Zahl der zufriedenen Bürger direkt aus dem Luxus und kürzt dann zufrieden
und unzufrieden gemeinsam, bis beide in die verfügbaren Plätze passen. Das alte Modell stuft stattdessen
einzelne Bürger nacheinander hoch.

**Antworten auf die offenen Punkte**

- Q2: CivOne hat eine eigene Luxuskette, die an jedem wichtigen Punkt abweicht. Entertainer zählen dort drei
  Luxus statt zwei und werden nach Marktplatz und Bank addiert statt davor. Der Luxusanteil wird aus dem
  Rest nach Steuern gebildet statt aus Handel minus Korruption. Und der Handel hat die Korruption der
  Wirtschaft schon abgezogen, die der Palast auf null setzt. Das neue Modell nutzt diese Kette deshalb gar
  nicht, sondern rechnet in Stufe 2 selbst. Doppelt angewendet wird nichts, und der Palast kann nicht
  durchschlagen.
- Q3: Passt bereits. Der Wunderbesitz hängt an den Städten des Spielers, wandert also bei einer Eroberung
  mit. Die Veralterung prüft alle Zivilisationen, nicht nur den Besitzer. Keine Codeänderung nötig.
- Q4: Entschieden. Deity wird nicht extrapoliert, sondern verhält sich beim Empire-Term wie Emperor. Sonst
  begänne die Strafe unter Despotismus schon bei fünf Städten, härter als alles, was das Original erzeugen
  kann.

**Tests**

Voller Build ohne Warnungen, der komplette Testlauf mit 1242 Tests grün.

Zwei Suiten tragen Phase D. `OriginalCityCitizenServiceTests` prüft das Modell gegen Mocks: Basisterm nach
Schwierigkeit und Regierungsform, die Zone ohne Strafe, die zuerst bestrafte Stadt pro Zeile, die Staffelung
über die Stadtplätze, die Reihenfolge von Strafe und Deckelung, beide Refill-Lesarten mit Shakespeare, die
Stufenreihenfolge, jeden Modifikator einzeln und beide Einstiegspunkte auf derselben Instanz.
`OriginalCityHappy` ist das Gegenstück zu `CityHappy.cs` mit den elf von Hand nachgerechneten Fällen gegen
ein echtes Spiel.

Die Tests in `SoundPackRenderQueueTests` sind zeitabhängig und fallen mal aus, auch ohne diese Änderungen.
Im letzten vollen Lauf waren sie grün.

Damit ist dieser Arbeitsschritt abgeschlossen. Der Schalter bleibt aus, der Zweig ändert also kein
Spielverhalten. Alle offenen Punkte aus Kapitel 11 sind beantwortet, Q1 seit der genaueren Lesung der
Dekompilierung eingeschlossen.

Als Nächstes kommt die manuelle Testphase im Spiel, danach Phase E in einem eigenen Zweig. Die Anleitung
dafür steht in Kapitel 13, samt der Stelle, an der der Schalter eingeschaltet wird.

---

## Goal

Replace CivOne's invented city-happiness base term with the original's, correct the modifiers that are wrong,
and add the two mechanisms CivOne is missing entirely (per-stage normalisation and the unhappy surplus that
stays pending).

## Decisions taken

| # | Decision |
|---|---|
| H1 | **Port the original's base term.** CivOne's `6 − Difficulty` content limit plus `ApplyEmperorEffects` is replaced by the original's government- and empire-size-dependent formula. |
| H2 | **One model, not a scoring-only variant.** Every consumer of `GetCitizenTypes()` sees the corrected model. |
| H3 | **Red shirts stay** as CivOne's representation of the original's pending unhappiness — driven by the pending unhappiness, not by the frozen 37-city literal. |
| H4 | **The pending unhappiness refill ships in both readings, switchable** — halving and full drain — decided later by play testing. See §6. |
| H5 | **This lands as a parallel subclass** behind a factory switch, with its own test class; the existing model and its tests stay untouched until the switch flips. The switch stays **off** in this branch — flipping it and folding the subclass back in is deferred to a follow-up branch after the manual play test. See §7. |
| H6 | **Difficulty 5 (Deity) is CivOne's own**, and it shares the emperor row rather than extrapolating the formula. Decided, see §2. |
| H7 | **The luxury rate becomes a parameter** rather than being read from the player. Needed by the score's 40 % reference; harmless everywhere else. |

---

## 1. The original's model

For the human player:

```
base    = 14 − 2 × Difficulty
base    = ((GovernmentType / 2) + 2) × (base / 2)
E       = ((cityId % base) + CityCount − base) / base
unhappy = E + Size + Difficulty − 6                     // NOT clamped here — see §3

corruption = (trade × X × 3) / (Government × 20 + 80)
corruption = 0            if Government == Democracy
corruption = corruption/2 if Courthouse                  // Courthouse only — the Palace does not reduce it

lux     = clamp( ((10 − Science − Tax) × (trade − corruption) + 5) / 10, 0, trade )
lux    += Entertainers × 2
lux    += lux / 2   if Marketplace
lux    += lux / 2   if Bank
happy   = lux / 2
```

For AI players `unhappy = Size − 3` — and that is the only difference, see §5.

Two details of the luxury chain that are easy to get wrong: the `+5` sits **inside the numerator**
(`(A × B + 5) / 10`, i.e. rounding applied to the product, not afterwards), and `Entertainers × 2` is added
**before** Marketplace and Bank, so entertainer luxury is multiplied by them too. The clamp's upper bound is the
**raw** trade, while the product uses trade *minus* corruption — the two values are deliberately different.

### Modifiers — exact

| Modifier | Effect | Condition |
|---|---|---|
| Colosseum | `unhappy −= 3` | the building alone, no advance |
| Cathedral | `−6` with Michelangelo, else `−4` | outer gate: the advance **Religion** |
| Temple | `−2` with Mysticism, else `−1` with Ceremonial Burial, else `0` | `else if`, **not** additive; without either advance the building is inert |
| Oracle | a further `−2` with Mysticism, else `−1` | inside the Temple block: needs a temple, no advance of its own, and does **not** inherit the Ceremonial Burial gate |
| Hanging Gardens | `happy++` | owner has the wonder |
| Cure For Cancer | `happy++` | owner has the wonder |
| Shakespeare's Theatre | `unhappy = 0` | the wonder's city **is** this city — no owner check |
| J.S. Bach's Cathedral | `unhappy −= 2` | owner has the wonder **and** same continent as the wonder's city |

Corrections this forces on CivOne:

- **Michelangelo's Chapel is global for its owner, not continental.** The original's wonder-ownership helper
  checks ownership and obsolescence only — no geography. CivOne's `CathedralDelta`
  ([:449-467](../src/Screens/Services/CityCitizenService.cs#L449-L467)) restricts it to the city's continent;
  that check is removed. Only J.S. Bach is continent-bound, and CivOne already has that right.
- **Cathedral: the inversion claimed here was never real.** An earlier revision of this plan said CivOne had the
  `−4` base and the `−6` with Michelangelo the wrong way round. It does not; `CathedralDelta` was always right
  about the two numbers. The genuine defects are the two below. Noted so the claim does not come back.
- **Cathedral over-restricts Michelangelo.** The chapel is global for its owner, while CivOne restricts it to
  the city's continent. That check is removed; only J.S. Bach is continent-bound.
- **Cathedral is missing its advance gate.** The whole block sits behind a check on **Religion**, which CivOne
  does not have. The gate was briefly removed from this plan on the argument that Religion is the cathedral's
  own build prerequisite and the check could therefore never fire. That argument is wrong, and the case it
  misses is the one that matters: **a captured city brings the building without the advance**. Such a
  cathedral is idle until its new owner researches Religion, and then it works retroactively. The same holds
  for the temple, one step further down — see the table in §1.
- **Temple and Oracle needed correcting after all.** An earlier revision of this plan claimed CivOne's doubling
  (`<<= 1`) produced the same numbers as the original's addition. It does not, because CivOne has **no advance
  gate**: its temple is worth one even to an owner who knows neither Ceremonial Burial nor Mysticism, where the
  original's is worth nothing. The doubling then compounds the error. Rewritten as the original has it —
  additive, gated, with the oracle following the temple's Mysticism step but not its Ceremonial Burial gate.
  Practically this shows up on **captured cities**, whose temples stay idle until the new owner knows the
  advance, and Mysticism raises every temple the owner has at once.

  The three happiness buildings are gated differently, and the difference is easy to get wrong:

  | building | gate | what a captured copy does for an owner without the advance |
  |---|---|---|
  | Colosseum | none | works at once |
  | Temple | two steps: Mysticism → 2, else Ceremonial Burial → 1, else 0 | nothing, until Ceremonial Burial is researched |
  | Cathedral | one step: Religion | nothing, until Religion is researched |

  Mysticism requires Ceremonial Burial and advances are never lost, so "Mysticism without Ceremonial Burial"
  is the one combination that cannot occur in a game. All three temple branches are otherwise reachable.

- **Wonder ownership follows the city.** Capturing a city transfers its wonder's effect to the captor. And
  obsolescence is checked against **all** civilizations 1–7: a wonder goes obsolete as soon as *any* player has
  the obsoleting advance, not only its owner. Verify CivOne's `HasWonderEffect` / `WonderObsolete` against both
  (Q3).
- **Shakespeare has no owner check** in the original — the wonder works for whoever holds the city. That falls
  out of "ownership follows the city" and matches CivOne's `_cityBuildings.HasWonder<>()`.

### War weariness

```
w        = max(1 + (Government == Democracy ? 1 : 0) − (WomensSuffrage ? 1 : 0), 0)
unhappy += w × UnitsCountedAsAway          // uncapped — the normalisation behind it is the only limit

UnitsCountedAsAway = units with Attack > 0, supported by this city,
                     that are air units OR not standing on the city tile
```

Women's Suffrage removes the base point entirely and Democracy adds a second — so Democracy *with* Women's
Suffrage is as weary as Republic *without* it. The floor at `0` can never be reached with these terms, so
`(WomensSuffrage ? 0 : 1) + (Democracy ? 1 : 0)` is an equivalent formulation. Some remake code in the wild
inverts this and makes Women's Suffrage add unhappiness; that is a hard bug and must not be ported.

Three details that are easy to get wrong, all confirmed against the original:

- **Air units count even while they stand at home.** The rule is "is an air unit", not "is a ship" — the
  original's terrain class is 0 land, 1 air, 2 sea, and exactly Fighter, Bomber and Nuclear carry the 1. A
  battleship in its home port causes **no** weariness; a fighter on the same tile does. CivOne's `UnitClass.Air`
  covers exactly those three (`BaseUnitAir`), so the predicate maps one to one.
- **There is no upper bound.** Martial law is capped at three units *and* limited to the unhappy citizens that
  already exist; war weariness is neither. It raises a counter and lets the normalisation clamp the result —
  the same counter-versus-array distinction as stage 2 (§7). Going through `DowngradeCitizens` would silently
  cap the effect at the citizens that happen to be downgradable and would spend points turning happy citizens
  content instead of making citizens unhappy.
- **Attack value zero never counts**, which excludes settlers, diplomats, caravans and the transport.

The same predicate drives the original's city-screen display, so the icons and the count always agree.

### Martial law detail

Beyond the unit list, the original also counts two unidentified city slots towards martial law, and it applies
the conversion only while `unhappy != 0`. The second half matters (CivOne's `UnhappyToContent` already no-ops on
zero); the two extra slots are not ported — recorded as deviation 4.

### Stage order — exact

Five normalisation calls (`N`), with exactly this between them:

```
1.  base term  (human: E formula / AI: Size − 3),  overflow → pending
    ── N ──
2.  happy = lux / 2
    ── N ──
3.  Colosseum, Cathedral (Religion gate), Temple (+ Oracle)
    ── N ──
4.  martial law  (Government < Republic)  XOR  war weariness  (Government >= Republic)
    ── N ──
5.  Hanging Gardens++, Cure For Cancer++, Shakespeare = 0, J.S. Bach −2
    ── N ──
```

Stage 4: martial law and war weariness are an `if/else` on government in the original, never both. CivOne calls
`ApplyMartialLaw` and `ApplyDemocracyEffects` in sequence
([Stage4:196-205](../src/Screens/Services/CityCitizenService.cs#L196-L205)); their internal guards happen to make
them mutually exclusive today, but make the exclusivity explicit.

Stage 5: **Shakespeare runs before the last `N`**, so the pending unhappiness refill can partially undo its `unhappy = 0`.
In a large empire Shakespeare is therefore not absolute: it sets `unhappy` to zero, and the balancer then gives
back half of what is parked.

---

## 2. How CivOne differs today (H1, H6)

CivOne today ([:237-240](../src/Screens/Services/CityCitizenService.cs#L237-L240)):

```
contentLimit = 1 + 5 − Difficulty                       // = 6 − Difficulty
unhappy      = max(Size − contentLimit, 0)              // = max(Size + Difficulty − 6, 0)
```

The original's per-city term contains that expression verbatim. The whole difference is one additive
empire-size penalty:

```
unhappy_original = unhappy_civone + E
E = trunc( ((cityId % base) + CityCount − base) / base )
base = ((GovernmentType / 2) + 2) × (7 − Difficulty)
```

Because `cityId % base ∈ [0, base−1]`, the numerator stays below `base` as long as `CityCount ≤ base`, so
**`E = 0` and the two formulations are bit-identical** up to that empire size. Beyond it, one city per `base`
crosses the threshold at a time, staggered by `cityId`.

| Difficulty | `base` Despotism / Monarchy / Republic | first city penalised | every city penalised |
|---|---|---|---|
| 0 Chieftain | 14 / 21 / 28 | 15 / 22 / 29 | 28 / 42 / 56 |
| 1 Warlord | 12 / 18 / 24 | 13 / 19 / 25 | 24 / 36 / 48 |
| 2 Prince | 10 / 15 / 20 | 11 / 16 / 21 | 20 / 30 / 40 |
| 3 King | 8 / 12 / 16 | 9 / 13 / 17 | 16 / 24 / 32 |
| 4 Emperor | 6 / 9 / 12 | 7 / 10 / 13 | 12 / 18 / 24 |
| 5 Deity *(CivOne only)* | 6 / 9 / 12 | 7 / 10 / 13 | 12 / 18 / 24 |

(`GovernmentType / 2 + 2`: Anarchy/Despotism → 2, Monarchy/Communism → 3, Republic/Democracy → 4.)

**The original has five levels, 0–4** — Chieftain … Emperor, clamped to `[0, 4]` in its menu. Rows 0–4 are
ported fact.

**Difficulty 5 (Deity) is CivOne's own addition** (H6), switchable via `Settings.DeityEnabled`
([Game.cs:189-190](../src/Game.cs#L189-L190)); `MaxDifficulty` is 5 only when it is on. **Deity repeats the
emperor row**: the difficulty is clamped to 4 before `base` is computed, so deity starts the empire-size
penalty at the same city count as emperor.

The alternative was to continue `(7 − D)` down to `2`, giving `base = 4 / 6 / 8` and a penalty from 5 cities
under Despotism. That is steeper than anything the original can produce, and it would make deity unplayable
for reasons the original never intended. The clamp lives in `OriginalCityCitizenService.MaxPenaltyDifficulty`.

Note this affects **only** the empire-size term. The `Size + Difficulty − 6` part keeps the real difficulty,
so deity still grants one content citizen less than emperor, exactly as CivOne has always done.

Everything here must therefore work for `Difficulty ∈ [0, MaxDifficulty]`, read from settings — never
hard-coded to 4 or 5. `CalculateCityStats` already gets this right with its "do not use `_game.MaxDifficulty`
here" comment ([:234-237](../src/Screens/Services/CityCitizenService.cs#L234-L237)); keep that property.

### CivOne's stand-in, and why replacing it generalises rather than discards

`ApplyEmperorEffects` ([:265-291](../src/Screens/Services/CityCitizenService.cs#L265-L291)) fires only at
difficulty ≥ 4 and from 13 cities, then downgrades one citizen (13–24), the whole city (≥ 25), and adds red
shirts from 37. It diverges structurally:

1. **Government type has no effect at all in CivOne.** In the original it scales `base` — Despotism → Monarchy
   is ×1.5, → Republic ×1.33. Largest single deviation, present on every difficulty.
2. **Onset.** The original penalises from `base + 1` cities on *every* difficulty; CivOne from a fixed 13 and
   only at difficulty ≥ 4.
3. **Growth.** `E` grows linearly and unbounded; CivOne's effect is capped.

But the constants are not arbitrary: `MinEmperorCityCount = 13`, `MinEmperorCityCountStage2 = 25`,
`MinRedShirtCityCount = 37` and the `/12` step in `NumberOfRedShirts`
([:297-307](../src/Screens/Services/CityCitizenService.cs#L297-L307)) are exactly the steps of
`trunc(((cityId % 12) + CityCount − 12) / 12)` — **the `E` formula with `base` frozen at 12**, i.e. Emperor +
Republic, exactly what the in-code comment claims. CivOne hard-coded one row of the table and lost the
government and difficulty dependency. Replacing `ApplyEmperorEffects` with `E` therefore generalises the
existing behaviour.

### What this changes in play

Republic and Democracy become noticeably stronger for large empires, Despotism weaker, and the AI loses a
handicap it was never meant to carry (§5). Existing saves will play differently. That is intended — and it is
why this ships and is reviewed on its own.

---

## 3. Normalisation after every stage — the missing piece

The original does not compute `happy` and `unhappy` in one pass. It calls a normalisation routine **five times**
— after the base term, after luxuries, after buildings, after martial law, after wonders. In full:

```
N(specialists):
    while (pending >= 0 && unhappy < pending):     // pending unhappiness flows back
        pending--;  unhappy++
    happy   = clamp(happy,   0, Size)
    unhappy = clamp(unhappy, 0, Size)
    while (happy + unhappy > clamp(Size − specialists, 0, 99)):
        if pending > 0: pending--
        else:             happy = clamp(happy − 1, 0, Size)
        unhappy = clamp(unhappy − 1, 0, Size)
```

Without it the base term is simply wrong at the small end: `Size + Difficulty − 6` is `−5` for a size-1 city on
difficulty 0.

Two consequences the implementation must honour:

- **Order matters.** `E` is added *before* the lower clamp: `max(Size + Difficulty − 6 + E, 0)`, never
  `max(Size + Difficulty − 6, 0) + E`. Since `E ≥ 0` the two differ exactly where the base term is negative —
  i.e. for every small city in a large empire, which is the common case the empire-size penalty exists for.
- **Clamping is per stage, not once at the end.** A modifier that would drive `unhappy` below zero loses the
  surplus at that point; it does not carry over to offset a later increase.

CivOne's `CitizenTypes` bookkeeping already re-counts after every stage
([Stage1–Stage5](../src/Screens/Services/CityCitizenService.cs#L148-L216)) and its `DebugService.Assert(ct.Sum()
== _city.Size)` invariants enforce the ceiling structurally. What is missing is the flow-back of the pending unhappiness and the
lower clamp ordering around `E`.

---

## 4. The unhappy pending unhappiness (H3, H4)

When `unhappy` exceeds the city size, the original does not discard the surplus. It parks the excess in a side
variable, sets `unhappy = Size`, and the normalisation routine **refills from that store** whenever a
modifier later reduces `unhappy`. So a heavily over-unhappy city needs more Temple/Colosseum/luxury capacity
than its visible unhappy count suggests before anything improves.

There is no distinct very-unhappy *citizen type* in the original — its screens know three categories plus
specialists. But the semantics CivOne's red shirts express, "costs more to pacify", is a genuine mechanism,
implemented as a pending count rather than as a type. **CivOne's red shirts are a legitimate representation of a
real mechanism**, not an invention, and stay (H3).

CivOne's red-shirt machinery is `WearRedShirt` plus the special cases in `UpgradeCitizens`
([:530-540](../src/Screens/Services/CityCitizenService.cs#L530-L540)) and `UnhappyToContent`
([:610-621](../src/Screens/Services/CityCitizenService.cs#L610-L621)). Rule: the *count* comes from the original
(`E` plus the pending unhappiness), the *representation* stays CivOne's — red shirts are now driven by `unhappy` exceeding
`Size` instead of by the frozen 37-city literal.

---

## 5. AI players — one term, not one model

The `PlayerID == HumanPlayerID` branch sets the base term and **nothing else**. Everything after it — Colosseum,
Cathedral, Temple, Oracle, martial law, war weariness, all four wonders, all five normalisation calls — is
shared code and runs identically for AI cities with their own government.

So the difference is exactly one term:

```
human: unhappy = max(Size + Difficulty − 6 + E, 0)
AI:    unhappy = Size − 3
```

It is one `if (isHuman)` at a single site, and leaving it out is not neutral: CivOne currently applies the human
base — difficulty scaling *and* the empire-size penalty — to AI cities, a handicap the original's AI never
carries.

This is the **only** part of this plan with no effect on the civilization score, which sums the human player's
cities alone. Treat it as optional and drop it from the PR without hesitation if it complicates review.

---

## 6. Two readings of the pending unhappiness refill, switchable (H4)

The refill loop's termination condition `unhappy < pending` makes the flow-back stop halfway — it ends as soon
as `unhappy` has caught up with what is still pending, so roughly half of a modifier's improvement is
immediately undone. The alternative reading is a **full drain**, where improvements do nothing at all until
nothing is pending any more. A swapped compare operand in the decompilation would produce exactly that difference, and
the decompilation repository holds no raw disassembly to settle it.

### Settled — the loop is neither of the two (Q1)

A closer reading of the decompilation settled this without a DOSBox run. The first loop is not a drain of the
parked unhappiness at all. It is a **balancer**: it moves one unit at a time from the parked side into the
visible side until the visible side is no longer the smaller of the two. The sum of both is untouched.

```csharp
while (pending > 0 && unhappy < pending) { pending--; unhappy++; }
```

What an improvement achieves therefore depends on how much is parked, and the three outcomes are all reachable
in a normal game. Written with `s` for visible after the improvement and `p` for parked:

| situation | outcome | example, a colosseum worth three |
|---|---|---|
| `p <= s` | works in full, nothing flows back | size 10, visible 10, parked 4 → visible 7, parked 4 |
| `p` slightly above `s` | works in part | visible 10, parked 9 → visible 8, parked 8 |
| `p` far above `s` | absorbed, only the surplus shrinks | visible 10, parked 15 → visible 10, parked 11 |

As a formula: the new visible count is `max(s, ceil((s + p) / 2))`, the parked count is what is left of the
sum. The odd numbers come from every pass closing the gap by two, because it takes from one side and gives to
the other.

Two things matter for reproducing this faithfully:

- **The clamp to the city size belongs after the loop, not inside it.** In the third row above the loop runs to
  eleven visible against eleven parked, and the clamp then cuts the visible side back to ten. A loop that
  stopped at the city size on its own would leave twelve parked instead of eleven, so one unit of unhappiness
  that the original has already spent would still be waiting.
- **This is what the earlier `maxUnhappy` bound got wrong.** That bound was added in Phase D to rescue the full
  drain reading, and it silently changed the halving reading too. Full drain never existed, so the bound never
  had a reason to.

Only one behaviour remains, `EqualisingPendingUnhappinessDelegate`. The setting and its menu entry are gone.
The delegate stays a delegate, because the fold-in of Phase F is easier with the behaviour separate, and
because it is pinned on its own in `EqualisingPendingUnhappinessDelegateTests` — the three rows of the table
above are hard to reach through a whole city calculation.

---

## 7. How this lands: a parallel subclass (H5)

The corrected model does **not** replace `CityCitizenService` in place. It arrives as
`OriginalCityCitizenService : CityCitizenService`, selected in the factory, so the current model and its tests
keep running unchanged until the switch is flipped.

Why the extra class is worth it:

- **The existing tests stop being a problem.** They keep guarding the old class, which stays valid and green.
  Nothing is expected to break, so *any* red test is a real regression — a much sharper signal.
- **The risky moment becomes one line.** Game behaviour changes when the factory switches, not when the code
  lands. Trivially revertible, and play-testable before anyone commits to it.
- **It composes with H4**, one level up: a switch at the class instead of at the delegate.

The pattern already exists here — [CityCitizenServiceImplShim.cs](../xunit/src/Mocks/CityCitizenServiceImplShim.cs)
subclasses `CityCitizenService` and overrides its two virtual members — so this is the established way to extend
this class, not a new idea.

### What it costs

**A preparatory commit making members `virtual`.** Only `CathedralDelta`
([:449](../src/Screens/Services/CityCitizenService.cs#L449)) and `HasBachsCathedral`
([:469](../src/Screens/Services/CityCitizenService.cs#L469)) are virtual today. The subclass also needs
`CalculateCityStats`, `Stage1`–`Stage5`, `ApplyEmperorEffects` and `ApplyDemocracyEffects`. Adding `virtual` is
behaviour-neutral: separate commit, no test touched, easy to review.

### Why all five stages, and what the override actually looks like

The root of it is a model mismatch, not the inheritance. The original works on **two counters** — `happy` and
`unhappy` as plain ints, plus the pending unhappiness. CivOne works on a **citizen array**: `ct.Citizens[]` is the truth
and the counts are *derived* from it by `CountCitizenTypes` at the end of every stage
([:158](../src/Screens/Services/CityCitizenService.cs#L158), [:179](../src/Screens/Services/CityCitizenService.cs#L179), …).

So `Normalise()` cannot be a transcription. In an array model it has to count, correct, and then **write the
difference back into the array** by up- or downgrading citizens.

**Half of it is free.** The normalisation's second loop — the ceiling `happy + unhappy ≤ Size − specialists` —
is already structurally guaranteed here: the array has exactly `Size` entries and
`DebugService.Assert(ct.Sum() == _city.Size)` follows every stage. You cannot have more citizens than seats.
What remains is only the flow-back of the pending unhappiness (the first loop) and the lower clamp around `E` — considerably
less than §3's pseudocode suggests.

**`Stage1` must be rewritten in full**, without calling `base`:

```csharp
protected int Stage1(ref CitizenTypes ct)
{
    (int initialUnhappyCount, int initialContent) = CalculateCityStats(ct);
    ct = StageBasic(ct, initialContent, initialUnhappyCount);
    ApplyEmperorEffects(ct);          // must go, but sits in the middle of the body
    ...
    return initialContent;            // consumed later by Stage4
}
```

Two reasons: `ApplyEmperorEffects` is wired into the body, and `CalculateCityStats` returns a `(int, int)` tuple
with no room for the pending unhappiness. Per `CLAUDE.md`'s side-effect rule, note that the return value
`initialContent` flows through `Stage4` into `ApplyDemocracyEffects`
([:199](../src/Screens/Services/CityCitizenService.cs#L199)) — it must survive the rewrite even though the
computation above it changes.

**`Stage2`–`Stage5` are cheap.** The base already recounts at the end, so `Normalise` just layers on top:

```csharp
protected override CitizenTypes Stage3(CitizenTypes ct)
{
    ct = base.Stage3(ct);   // building effects + CountCitizenTypes, unchanged
    return Normalise(ct);   // flow-back of the pending unhappiness + lower clamp
}
```

Four times the same line. That is where the inheritance pays for itself.

### Two traps

**`CitizenTypes` is a struct** — visible from `public readonly bool Valid()`. That is why `Stage2`–`Stage5`
return `ct` instead of mutating it, and it means the pending unhappiness cannot live inside the struct without changing a
public type that ten consumers use. It belongs in a private field on the subclass.

**Park against the city size, not against the seats.** The surplus the original parks is what exceeds the
**city**; the seats (size minus specialists) are the concern of the normalisation that follows, not of the
parking test. Getting this wrong does not look like an error — the counts stay plausible — but a non-zero
surplus makes the normalisation spend the surplus instead of a happy citizen, so every crowded city reports one
happy citizen too many. It cost a save-file comparison to find. See C9.

**That field needs an explicit reset.** The service is short-lived — `ICityCitizenService.Create` builds one per
city evaluation — but `GetCitizenTypes()` and `EnumerateCitizens()` can both run on the same instance. Without a
reset at the top of both, the second call inherits the pending unhappiness of the first one. This is the kind of defect tests
miss, because a test usually exercises only one of the two entry points; add one that calls both in sequence.

### The switch

`ICityCitizenService.Create` ([:64](../src/Screens/Services/ICityCitizenService.cs#L64)) hard-codes
`new CityCitizenService(...)`. That is the single selection point, and per `CLAUDE.md` the choice belongs in the
factory rather than inside either class. Add one setting (`OriginalHappinessModel`), defaulting to **off** while
the subclass is being built and flipped to **on** once the play test passes.

### Time-box — this is a transitional state, and where the current work stops

Two parallel happiness models violate H2 ("one model"). That is acceptable while the new one is being validated
and **not** acceptable afterwards. The full sequence is:

1. `OriginalCityCitizenService` lands with the setting off. Old tests green, new tests green.
2. Play test decides H4 (pending unhappiness refill) and confirms the model.
3. Setting flips to on; the default path is the corrected model.
4. **Cleanup commit:** fold the subclass into `CityCitizenService`, delete the setting, delete the superseded
   tests listed in §9 step 0, and re-derive `CityHappy.cs` against the corrected model. One model again.

**The current work delivers step 1 and stops there.** Steps 2–4 depend on a long manual play-test phase in the
game, which cannot be compressed into this branch; they are deferred to a later, separate branch and PR.

What that means in practice:

- The branch ships with `OriginalHappinessModel` **off**. No behaviour changes, so nothing is at risk while the
  play test runs.
- The two models coexist for as long as the play test takes. That is a known, accepted debt, not an oversight —
  H2 is satisfied in step 4, not in this branch.
- The deferred work must stay visible: keep Phase E in this document, and do not close the plan or delete the
  setting until step 4 has run. If steps 2–4 are dropped rather than deferred, the repository owns two
  happiness models indefinitely — the outcome H2 exists to prevent.

---

## 8. Sourcing `cityId`

The staggering term `cityId % base` needs the original's **numeric city slot**, 0–255. CivOne has no such field:
`ICityBasic.Id` ([:19](../src/ICityBasic.cs#L19)) is a `Guid`. The equivalent is the city's position in
`Game._cities`, which is already what the SVE writer treats as the slot
([Game.LoadSave.cs:94](../src/Game.LoadSave.cs#L94)).

**Add one additional city index per city evaluation, sourced from the array, and do not persist it.** CivOne has
a map editor, so cities can be created and removed at arbitrary positions and any persisted index would go stale.
A derived runtime index is always consistent with the list it came from, and matches the original, where the slot
was likewise just an array position.

- Add a city-index lookup to `IGameCitizenDependency` (e.g. `int GetCityIndex(ICityBasic city)`), backed by
  `Game._cities.IndexOf(...)`. This is the additional per-city index needed by the model; the service reads it
  once per `GetCitizenTypes()` call.
- `Game` already builds per-call city lookups ([Game.cs:241](../src/Game.cs#L241)); if the linear scan shows up
  in profiling, cache a `Guid → index` map invalidated on city add/remove, not on every access.
- Consequence to accept: adding or deleting a city in the editor renumbers the ones after it, so *which* cities
  carry the empire-size penalty first can shift. That is inherent to the original's model — the penalty is
  distributed by slot, not by any property of the city — and is only visible while `E` differs across cities,
  i.e. between `base + 1` and `2 × base` cities.

---

## 9. What this touches, and the steps

### Blast radius

`GetCitizenTypes()` has ten call sites. Only one of them is the civilization score:

| Consumer | Effect of the change |
|---|---|
| [City.cs](../src/City.cs) `:449`, `:532`, `:1229`, `:1559`, `:1602` | civil disorder, growth, celebration — the gameplay core |
| [CivilizationScore.cs:138](../src/Screens/Reports/CivilizationScore.cs#L138) | the F9 report |
| [TradeReport.cs:62](../src/Screens/Reports/TradeReport.cs#L62) | trade/tax figures |
| [TopCities.cs:161](../src/Screens/Reports/TopCities.cs#L161) | city ranking |
| [Diplomat.cs:39](../src/Units/Diplomat.cs#L39) | incite-revolt cost |
| [Icons.cs:339](../src/Graphics/Icons.cs#L339) | citizen icons on the city screen |

### Delivery

Its own branch, its own PR, its own merge, titled for what it is — "city happiness aligned with the original" —
not as preparation for anything. Because of H5 the PR changes no behaviour on its own.

Natural commit boundaries for **this** branch: (a) the `virtual` commit, (b) the subclass with steps 1–4,
(c) the corrections in steps 5–9, which land as overrides in the subclass rather than in the base class, so
the six tests of §9 step 0 keep passing. The factory switch and the cleanup are the deferred Phase E and
belong to the follow-up branch, after the manual play test (§7 *Time-box*).

### Steps

0. **Baseline, and the `virtual` commit.** The existing suites —
   [CityCitizenServiceImplTests.cs](../xunit/src/CityCitizenServiceImplTests.cs) (35 tests, 1162 lines) and
   [CityHappy.cs](../xunit/src/CityHappy.cs) (11 end-to-end cases) — are **not** modified here and must stay
   green throughout. Any red test in them is a regression, full stop.

   Two preparatory moves, each its own commit:

   a. Run both suites green and keep the result as the reference.
   b. Add `virtual` to `CalculateCityStats`, `Stage1`–`Stage5`, `ApplyEmperorEffects` and
      `ApplyDemocracyEffects`. Behaviour-neutral; no test touched.

   These six tests encode behaviour the subclass deliberately changes. They keep guarding the **old** class and
   are removed only in the cleanup step (§7), not now:

   | Test | Behaviour it pins, which the subclass changes |
   |---|---|
   | `CalculateCityStatsAllCases` (`:1073`) | the base term without `E` and without the human/AI split |
   | `ApplyEmperorEffectsTests` (`:1014`) | the method the subclass replaces with `E` |
   | `NumberOfRedShirtsTests` (`:996`) | the `36 / 12` literals |
   | `CathedralDeltaTest` (`:616`) | the continent restriction and the missing Religion gate |
   | `ApplyBuildingEffectsTestsCathedrals` (`:715`) | same, at the caller |
   | `ApplyDemocracyEffectsTests` (`:925`) | no Women's Suffrage term |

1. Extend `CalculateCityStats` with `Government`, `CityCount` and the additional runtime city index to compute
  `base` and `E`.
   Government and city count are already reachable (`_city.PlayerIntf.Government`,
   `_game.GetPlayer(...).Cities.Length` — `ApplyEmperorEffects` uses the latter today); the index comes from the
   lookup in §8. The existing `contentLimit` expression is already correct and stays. Apply the lower clamp
   **after** adding `E` (§3). Gate the whole term on `isHuman`; AI cities get `Size − 3` — *optional, §5*.
2. Replace `ApplyEmperorEffects` with `E`. Do not keep both.
3. Add the pending unhappiness (§4): surplus beyond `Size` is parked and refilled as modifiers reduce `unhappy`.
   Drive the red-shirt representation from it instead of from `MinRedShirtCityCount`. The refill goes in as a
   delegate (§6) that balances the visible against the parked side.
4. Add `Normalise` at the five stage boundaries — `Stage1` rewritten in full, `Stage2`–`Stage5` as
   `base.StageN(...)` plus `Normalise(...)`. Only the flow-back of the pending unhappiness and the lower clamp need implementing;
   the ceiling is already structural. See §7 for the details and the two traps.
5. Fix `CathedralDelta` ([:449-467](../src/Screens/Services/CityCitizenService.cs#L449-L467)) on two counts:
   **drop the continent check** — Michelangelo is global for its owner — and gate the whole Cathedral block on
   the advance Religion. The `−4` base and the `−6` with Michelangelo are already correct and stay.
6. Extend the luxury computation to the full original form (§1): corruption (Democracy → 0, Courthouse halves,
  Palace does not), `+5` inside the numerator, clamp upper bound on **raw** trade, `Entertainers × 2`, then
   `+50 %` each for Marketplace and Bank — in that order. Check against CivOne's existing `City.Luxuries` /
  `EntertainerLuxuries` before adding anything (Q2). Explicit guard: no Palace-based corruption reduction may
  flow into the happiness/luxury branch.
7. Add the Women's Suffrage term to war weariness in `ApplyDemocracyEffects`, and make stage 4's
  martial-law-XOR-weariness split explicit. Implement the exact formula from §1 and add a regression test that
  Women's Suffrage never increases war weariness.
8. Verify CivOne's wonder ownership and obsolescence against the original's rules (Q3): effect follows the city
   on capture, and a wonder is obsolete as soon as *any* civilization has the obsoleting advance.
9. Parameterise the luxury rate (H7): the rate flows in from the caller, defaulting to the player's real rate.
   The civilization score passes a fixed 40 % reference instead; every other caller behaves exactly as before.
10. Tests — see §11.

---

## 10. Known deviations from the original

1. **Red shirts as a citizen type.** The original has the *mechanism* (pending unhappiness) but not the *type* —
   its screens know three citizen categories plus specialists. CivOne keeps its fourth sprite as the
   representation. The frozen `13 / 25 / 37 / step 12` thresholds are *not* kept.
2. **`DebugFlags & 0x2` is not reproduced.** When that bit is cleared the original zeroes `happy` and `unhappy`
  at the end of the computation. Its default has the bit set, so the normal path is the one implemented; there
  is simply no equivalent switch here. Treat this as debug-only legacy behavior, never as part of the citizen
  model.
3. **~~Temple/Oracle keep CivOne's doubling form.~~** Withdrawn — the two do *not* produce identical numbers,
   because CivOne had no advance gate at all. See §1; the block was rewritten in the additive form.
4. **The two unidentified martial-law slots are not ported.** The documented three-unit martial law is
   implemented.
5. **Difficulty 5 (Deity) does not exist in the original** and repeats the emperor row for the empire-size
   term (H6). Its own difficulty still counts in the `Size + Difficulty − 6` part.
6. **~~Build prerequisites are not re-checked as effect gates.~~** Withdrawn. The gates are ported, both of
   them. Recorded here because the mistake is easy to repeat: an advance check that guards a building whose
   own prerequisite is that advance looks redundant, and is not. The case it decides is the **captured city**,
   which owns the building without the advance, and which starts working once the advance is researched.

---

## 11. Open points

| # | Point | Blocks |
|---|---|---|
| Q1 | ~~Pending unhappiness refill: halving or full drain?~~ **Settled, and neither.** The loop balances the visible against the parked side and preserves their sum, so how much of an improvement survives depends on how much is parked. See §6. The switch, the second delegate and the `maxUnhappy` bound are removed. | closed |
| Q2 | **Answered.** CivOne has its own chain in `CityEconomyServiceImpl`, and it differs on every point that matters: entertainers count **three** luxuries each and are added **after** Marketplace and Bank rather than before, the luxury share is taken from the post-tax remainder instead of the original's `(rate × (trade − corruption) + 5) / 10`, and `City.RawTradeTotal` has the **economy's** corruption already subtracted — which the Palace zeroes. The corrected model therefore does not reuse `City.Luxuries` at all: stage 2 is overridden and computes the chain from the two new members `TradeTotalGross` and `LuxuryCorruption`. Nothing is applied twice, and the Palace cannot leak in, because `LuxuryCorruption` never consults it. | done |
| Q3 | **Answered, no change needed.** `Player.HasWonder<T>()` is defined as *any of the player's cities has it*, so the effect follows a captured city and disappears with a destroyed one, exactly as in the original. `Game.WonderObsolete(wonder)` tests `_players.Any(x => x.HasAdvance(...))`, so a wonder goes obsolete as soon as **any** civilization has the obsoleting advance, not only its owner. Both match. | done |
| Q4 | **Answered.** Deity does not extrapolate. The difficulty is clamped to 4 for the empire-size term, so deity behaves like emperor there, and no empire is punished harder than the original could punish it. | done |

---

## 12. Test plan

- `CityCitizenServiceImplTests` / `CityHappy` — **unchanged and green throughout** (H5). Superseded tests are
  deleted only in the cleanup step (§7).
- `OriginalCityCitizenServiceTests` (new) — the base term per difficulty (0–4, plus Deity sharing the emperor row) **and
  government**; human vs. AI base; the `E = 0` agreement zone from §2's table; the first-penalised-city boundary
  per row; `cityId % base` staggering; **the clamp ordering** (a size-1 city on difficulty 0 in a large empire —
  the case that separates the two orderings); the pending unhappiness, in **both** refill variants (H4); the stage order
  (a modifier that overshoots loses the surplus at its own `N`, not later); Cathedral with/without Michelangelo
  and without Religion; Michelangelo across continents; the luxury chain including Marketplace/Bank and
  entertainer multiplication; war weariness with and without Women's Suffrage; each remaining modifier in
  isolation; the luxury-rate parameter; and **`GetCitizenTypes()` followed by `EnumerateCitizens()` on the same
  instance** — the pending-unhappiness-reset trap from §7.
- A corrected-model sibling of `CityHappy.cs` (new) — the eleven entertainer/temple integration cases
  **re-derived by hand** against the new formula, not adjusted until green. These are what catch a mistake in
  the luxury chain.
- Combination tests and the invariant sweep — see the Phase F checklist; they are what protects the fold-in.
- `GetCityIndex` — stable for an unchanged list; the staggering test pins the mapping index → penalised city
  explicitly, so a future change to the index source fails loudly.
- Manual: city screens, disorder and celebration behave sensibly; the six consumers in §9 still render.


---

## 13. Manueller Testplan (deutsch)

Dieses Kapitel ist die Anleitung für die Spielphase vor Phase E. Es ist bewusst auf Deutsch, weil es eine
Arbeitsanweisung ist und kein Teil der Codedokumentation.

Ziel der Phase sind drei Dinge. Erstens bestätigen, dass sich das korrigierte Modell im echten Spiel
vernünftig anfühlt. Zweitens prüfen, ob die zehn Stellen, die `GetCitizenTypes()` benutzen, weiterhin sinnvoll aussehen.

### 13.1 Den Schalter einschalten

Am bequemsten geht das im Spiel: Hauptmenü, dann *Game Settings*, dort *Patches*. Die beiden Einträge heißen
*Original city happiness* und *Pending unhappiness* (A5). Beide dürfen mitten im Spiel umgelegt werden, weil
die Zufriedenheit bei jedem Zugriff neu gerechnet und nirgends gespeichert wird.

Alternativ direkt in der Profildatei, was für einen reproduzierbaren Startzustand der Testreihe der sicherere
Weg ist.

Die Datei heißt `default.profile` und liegt im Speicherordner von CivOne:

| System | Pfad |
|---|---|
| Windows | `%LOCALAPPDATA%\CivOne\default.profile` |
| Linux und macOS | `~/.local/share/CivOne/default.profile` |

Die Datei ist XML. Innerhalb von `<CivOneProfile>` werden zwei Einträge gesetzt oder ergänzt:

```xml
<CivOneProfile>
  <OriginalHappinessModel>1</OriginalHappinessModel>
</CivOneProfile>
```

- `OriginalHappinessModel`: `1` schaltet das korrigierte Modell ein, `0` oder fehlend lässt das alte Modell
  laufen. Voreinstellung ist aus.

Wichtig: Das Spiel liest die Einstellungen beim Start. Nach jeder Änderung an der Datei muss CivOne neu
gestartet werden. Am besten die Datei vorher sichern, damit der Ausgangszustand jederzeit zurückkommt.

Kontrolle, dass der Schalter wirkt: eine Stadt der Größe vier mit zwei Entertainern auf König-Stufe unter
Despotismus. Das alte Modell zeigt zwei zufriedene Bürger, das neue einen zufriedenen und einen
genügsamen. Siehe `OriginalCityHappy.ACityOfFourWithTwoEntertainers`.

### 13.2 Vorbereitung

- Ein Spielstand kurz vor einer Ausdehnung, mindestens acht Städte, damit die Empire-Strafe überhaupt
  greifen kann.
- Ein zweiter Spielstand mit einer sehr großen Zivilisation, mindestens zwanzig Städte.
- Beide Spielstände jeweils einmal mit altem und einmal mit neuem Modell öffnen, ohne einen Zug zu machen,
  und die Stadtübersicht vergleichen.
- Notieren, was auffällt. Eine einfache Tabelle mit Stadt, Größe, Regierung, Städtezahl, zufrieden,
  genügsam, unzufrieden reicht.

### 13.3 Was zu prüfen ist

**A. Die Empire-Strafe**

1. Mit wenigen Städten darf sich nichts ändern. Unterhalb der Basis ist das neue Modell identisch zum alten.
   Basis ist `(Regierung / 2 + 2) × (7 − Schwierigkeit)`, unter Despotismus auf Kaiser also sechs.
2. Eine Stadt über der Basis gründen. Genau eine Stadt soll unzufriedener werden, nicht alle.
3. Weiter ausdehnen. Pro zusätzlicher Stadt kommt eine weitere betroffene Stadt dazu, bis bei der doppelten
   Basis alle betroffen sind.
4. Regierungswechsel von Despotismus zu Monarchie und weiter zur Republik. Die Basis wächst dabei um die
   Hälfte und dann um ein Drittel, die Strafe muss spürbar nachlassen.

**B. Die ausstehende Unzufriedenheit**

Q1 ist entschieden, hier wird also nur noch bestätigt, was §6 beschreibt. Der Punkt bleibt trotzdem der
schwierigste, weil die Wirkung eines Gebäudes davon abhängt, wie viel geparkt ist.

1. Eine große Zivilisation auf Kaiser, deutlich über der Basis, dazu eine Stadt mit viel ausstehender
   Unzufriedenheit. Erkennbar an roten Hemden in der Stadtansicht.
2. In dieser Stadt ein Kolosseum bauen und beobachten, wie viel von den drei Punkten ankommt. Bei wenigen
   roten Hemden alles, bei vielen nichts, dazwischen ein Teil. Zum Vergleich die Tabelle in §6.
3. Dasselbe mit dem Shakespeare-Theater. Es ist in einem großen Reich nicht absolut, weil der Rückfluss nach
   der Wunderstufe noch einmal greift.
4. Wenn sich etwas anders verhält als die Tabelle sagt, ist das ein Fund und kein Spielgefühl. Dann dieselbe
   Stellung in DOSBox nachstellen, bevor am Code etwas geändert wird.

**C. Die Kathedrale**

1. Ohne die Technologie Religion darf eine Kathedrale nichts bewirken. Das ist neu und fällt auf.
2. Mit Religion macht sie vier Bürger genügsam.
3. Mit Michelangelo sechs, und zwar in jeder Stadt des Besitzers, auch auf anderen Kontinenten. Das ist die
   zweite auffällige Änderung.

**D. Kriegsmüdigkeit**

1. Als Republik eine Einheit außerhalb der Stadt stationieren. Ein Bürger wird unzufrieden.
2. Als Demokratie dasselbe. Zwei Bürger werden unzufrieden.
3. Frauenwahlrecht bauen. In der Republik verschwindet die Wirkung ganz, in der Demokratie halbiert sie sich.
   Sie darf sich unter keinen Umständen verschlimmern.
4. Unter Despotismus und Monarchie prüfen, dass stattdessen das Kriegsrecht greift und nie beides zugleich.

**E. Luxus**

1. Steuer- und Forschungsregler so stellen, dass Luxus entsteht, und die Zahl der zufriedenen Bürger
   beobachten.
2. Marktplatz und Bank bauen. Beide erhöhen den Luxus um die Hälfte, und sie wirken auch auf den Luxus der
   Entertainer, weil diese vorher addiert werden.
3. Entertainer zählen jetzt zwei Luxus statt drei. In kleinen Städten fällt das auf.
4. Ein Gerichtsgebäude halbiert die Korruption im Luxuspfad, ein Palast nicht. Die Hauptstadt darf hier
   keinen Sondervorteil haben.

**F. Deity**

Deity nutzt für die Empire-Strafe dieselbe Basis wie Kaiser. Prüfen, dass sich eine große Zivilisation auf
Deity nicht härter anfühlt als auf Kaiser, abgesehen von dem einen zufriedenen Bürger weniger, den Deity
schon immer gekostet hat.

**G. Die zehn Verbraucher**

Für jede dieser Stellen einmal hinsehen, ob die Anzeige stimmig bleibt:

- Stadtansicht mit den fünf Stufen der Zufriedenheit.
- Aufstand und Feier, also ob eine Stadt in den richtigen Momenten kippt.
- Stadtwachstum.
- Zivilisationsbericht F9.
- Handelsbericht.
- Beste Städte.
- Kosten für den Aufstand durch einen Diplomaten.
- Die Bürgersymbole in der Stadtansicht.

**H. Speichern und Laden**

1. Spielstand speichern und wieder laden. Die Zufriedenheit muss danach identisch sein.
2. Der Stadtplatz wird nicht gespeichert, sondern aus der Städteliste abgeleitet. Nach dem Laden muss
   dieselbe Stadt bestraft sein wie vorher.

**I. Karteneditor**

Eine Stadt in der Mitte der Liste löschen. Die Städte danach rücken auf, also kann sich verschieben, welche
Stadt die Empire-Strafe zuerst trägt. Das ist beabsichtigt, siehe Kapitel 8. Prüfen, dass nichts abstürzt und
die Gesamtzahl der unzufriedenen Bürger plausibel bleibt.

### 13.4 Abbruchkriterien

Die Phase gilt als gescheitert und der Schalter bleibt aus, wenn eines davon eintritt:

- Eine Stadt zeigt mehr oder weniger Bürger als ihre Größe.
- Eine Stadt bleibt dauerhaft im Aufstand, obwohl genug Luxus und Gebäude vorhanden sind.
- Die Empire-Strafe trifft alle Städte gleichzeitig statt gestaffelt.
- Deity fühlt sich härter an als Kaiser.

### 13.5 Was danach passiert

Mit einem guten Ergebnis geht es weiter mit Phase E, in einem eigenen Zweig: Schalter auf ein, danach die Unterklasse in `CityCitizenService` zurückfalten und die sechs
überholten Tests aus Kapitel 9 Schritt 0 löschen.
