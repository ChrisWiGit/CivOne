# Civilization I – Savegame-Format (`CIVIL*.SVE`)

Analyse des Speicherformats, abgeleitet **aus dem portierten Originalcode des
OpenCiv1-Projekts** ([rajko-horvat/OpenCiv1](https://github.com/rajko-horvat/OpenCiv1),
Rewrite der DOS-Fassung 475.05) und gegengeprüft gegen die Vorab-Forschung in diesem Repo:
[docs/SaveGame/memory_map_SVE_EN.txt](SaveGame/memory_map_SVE_EN.txt).

**Zu den Quellenlinks:** Alle `src/CivGame/...`- und `src/GPU/...`-Links unten zeigen nach
OpenCiv1, **nicht** in dieses Repo, und sind auf den Stand
[`a25ae37`](https://github.com/rajko-horvat/OpenCiv1/tree/a25ae37333f98616070d844158caa812bb8f9401)
festgenagelt. Die Analyse entstand an einer lokalen Arbeitskopie dieses Stands, in der
`City.cs`, `CityWorker.cs`, `Overlay_20.cs`, `Segment_1403.cs`, `Segment_1866.cs`,
`Segment_1ade.cs`, `Segment_25fb.cs` und `Segment_2aea.cs` verändert waren — in diesen
Dateien können die Zeilennummern um einige Zeilen abweichen, und Bezeichner wie
`City.FortifiedUnits` (dort noch `City.Unknown`) sind lokale Umbenennungen. Die
Funktionsnamen (`F11_0000_083b_LoadGameData` usw.) sind stabil und der zuverlässigere Anker.

Primärquellen (OpenCiv1):

| Was | Wo |
|---|---|
| Lesereihenfolge (maßgeblich) | [GameLoadAndSave.cs:747](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/GameLoadAndSave.cs#L747) `F11_0000_083b_LoadGameData` |
| Schreibreihenfolge (spiegelbildlich) | [GameLoadAndSave.cs:1347](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/GameLoadAndSave.cs#L1347) `F11_0000_08f6_SaveGameData` |
| Stadt-Record | [City.cs](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/CivState/City.cs) `City.FromStream` |
| Einheiten-Record | [Unit.cs](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/CivState/Unit.cs) `Unit.FromStream` |
| Einheiten-Definition | [UnitDefinition.cs](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/CivState/Definitions/UnitDefinition.cs) |
| Feldgrößen / Array-Längen | [CivState.cs](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/CivState/CivState.cs), [Player.cs](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/CivState/Player.cs) |

---

## 1. Überblick

Ein Spielstand besteht **immer aus zwei Dateien** mit gleichem Basisnamen:

| Datei | Inhalt |
|---|---|
| `CIVIL<n>.SVE` | kompletter Spielzustand, **37.856 Byte, feste Länge, unkomprimiert** |
| `CIVIL<n>.MAP` | die Weltkarte als PIC-Bild (RLE + LZW komprimiert) |

`<n>` ist die Slot-Ziffer `0`–`9`; der Dateiname wird in
[GameLoadAndSave.cs:152](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/GameLoadAndSave.cs#L152) zusammengesetzt
(Basisname + Ziffer + Extension `SVE`).

Eigenschaften der `.SVE`:

* **Keine Signatur, kein Header, keine Versionsnummer, keine Prüfsumme.**
  Die Datei ist ein reiner Speicher-Dump – das Format wird ausschließlich über die
  feste Gesamtlänge von 37.856 Byte identifiziert.
* **Little Endian** (16-Bit-Werte: Low-Byte zuerst) — siehe `ReadUInt16`/`ReadInt16`.
* **Kein Padding, keine Alignment-Lücken.** Jedes Feld folgt direkt auf das vorige.
* **Struct-of-Arrays statt Array-of-Structs:** Spielerdaten liegen *nicht* als
  8 Spieler-Blöcke hintereinander, sondern feldweise (erst alle 8 Goldstände,
  dann alle 8 Forschungsstände usw.). Ausnahme sind die drei echten Record-Arrays
  (Städte, Einheiten, Einheiten-Definitionen).
* **Immer 8 Civ-Slots**, unabhängig von der tatsächlichen Spielerzahl.

---

## 2. Gesamtlayout

Alle Offsets sind absolut ab Dateianfang. Das `[a][b]`-Notat bedeutet: äußere
Schleife `a` (Civ), innere Schleife `b` — also civ0-Element0..n, dann civ1-Element0..n.

| Offset | dez. | Größe | Feld | Typ | Bedeutung |
|---|---:|---:|---|---|---|
| `0x0000` | 0 | 2 | `game_turn` | short | Zugzähler seit Spielbeginn |
| `0x0002` | 2 | 2 | `human_player` | short | Civ-Index des menschlichen Spielers (0-7) |
| `0x0004` | 4 | 2 | `player_flags` | ushort | Bitmaske: welche Civs sind menschlich gesteuert |
| `0x0006` | 6 | 2 | `random_seed` | ushort | RNG-Seed (Kartengenerierung/Zufall) |
| `0x0008` | 8 | 2 | `year` | short | Jahr, negativ = v.Chr. |
| `0x000A` | 10 | 2 | `difficulty` | short | Schwierigkeitsgrad 0-4 |
| `0x000C` | 12 | 2 | `active_civs` | ushort | Bitmaske der noch lebenden Civs |
| `0x000E` | 14 | 2 | `current_research_id` | short | aktuell erforschte Technologie des Menschen |
| `0x0010` | 16 | 112 | `leader_name[8]` | char[14] | Herrschername, 13 Zeichen + NUL |
| `0x0080` | 128 | 96 | `civ_name[8]` | char[12] | Civ-Name (Plural), 11 Zeichen + NUL |
| `0x00E0` | 224 | 88 | `nationality[8]` | char[11] | Nationalitaets-Adjektiv, 10 Zeichen + NUL |
| `0x0138` | 312 | 16 | `cash[8]` | short | Goldbestand je Civ |
| `0x0148` | 328 | 16 | `research_progress[8]` | short | akkumulierte Forschungspunkte |
| `0x0158` | 344 | 448 | `active_units[8][28]` | short | Anzahl lebender Einheiten je Typ |
| `0x0318` | 792 | 448 | `units_in_production[8][28]` | short | Anzahl in Produktion je Typ |
| `0x04D8` | 1240 | 16 | `tech_count[8]` | short | Anzahl erforschter Technologien |
| `0x04E8` | 1256 | 80 | `tech_flags[8][5]` | ushort | Bitmaske erforschter Techs (5x16 = 80 Bits, 72 genutzt) |
| `0x0538` | 1336 | 16 | `government[8]` | short | Regierungsform |
| `0x0548` | 1352 | 256 | `continent_strategy[8][16]` | short | KI-Strategie je Civ und Kontinent (siehe Abschnitt 9) |
| `0x0648` | 1608 | 128 | `diplomacy[8][8]` | ushort | Diplomatie-Bitmaske Civ i gegenüber Civ j (siehe Abschnitt 8) |
| `0x06C8` | 1736 | 16 | `city_count[8]` | short | Anzahl Staedte |
| `0x06D8` | 1752 | 16 | `unit_count[8]` | short | Anzahl Einheiten |
| `0x06E8` | 1768 | 16 | `land_count[8]` | short | Anzahl besessener Landfelder |
| `0x06F8` | 1784 | 16 | `settler_count[8]` | short | Anzahl Siedler |
| `0x0708` | 1800 | 16 | `total_city_size[8]` | short | Summe aller Stadtgroessen |
| `0x0718` | 1816 | 16 | `military_power[8]` | short | Militaerstaerke (Statistik) |
| `0x0728` | 1832 | 16 | `ranking[8]` | short | Rangwert |
| `0x0738` | 1848 | 16 | `tax_rate[8]` | short | Steuerrate |
| `0x0748` | 1864 | 16 | `score[8]` | short | Punktestand |
| `0x0758` | 1880 | 16 | `contact_countdown[8]` | short | Countdown bis erneuter Kontaktaufnahme |
| `0x0768` | 1896 | 16 | `x_start[8]` | short | X-Startposition der Civ |
| `0x0778` | 1912 | 16 | `nationality_id[8]` | short | Index in die 16 Nationalitaeten |
| `0x0788` | 1928 | 256 | `continent_attack[8][16]` | short | AI-Angriffsbewertung je Kontinent |
| `0x0888` | 2184 | 256 | `continent_defense[8][16]` | short | AI-Verteidigungsbewertung je Kontinent |
| `0x0988` | 2440 | 256 | `continent_city_count[8][16]` | short | Staedte der Civ je Kontinent |
| `0x0A88` | 2696 | 128 | `continent_size[64]` | short | Groesse jeder Landmasse (Felder) |
| `0x0B08` | 2824 | 128 | `ocean_size[64]` | short | Groesse jedes Ozeans (Felder) |
| `0x0B88` | 2952 | 32 | `build_site_count[16]` | short | verfuegbare Siedlungsplaetze je Kontinent |
| `0x0BA8` | 2984 | 1200 | `score_graph[1200]` | byte | Verlaufsdaten Punktegraph |
| `0x1058` | 4184 | 1200 | `peace_graph[1200]` | byte | Verlaufsdaten Friedensgraph |
| `0x1508` | 5384 | 3584 | `cities[128]` | City | Stadt-Records, 28 Byte (siehe unten) |
| `0x2308` | 8968 | 952 | `unit_defs[28]` | UnitDefinition | Einheiten-Definitionen, 34 Byte (siehe unten) |
| `0x26C0` | 9920 | 12288 | `units[8][128]` | Unit | Einheiten-Records, 12 Byte (siehe unten) |
| `0x56C0` | 22208 | 4000 | `map_visibility[80][50]` | sbyte | Sichtbarkeits-Bitmaske je Feld (Bit n = Civ n) |
| `0x6660` | 26208 | 128 | `strat_active[8][16]` | sbyte | Geostrategie-Slot aktiv |
| `0x66E0` | 26336 | 128 | `strat_policy[8][16]` | byte | Geostrategie-Politik |
| `0x6760` | 26464 | 128 | `strat_x[8][16]` | sbyte | Geostrategie X |
| `0x67E0` | 26592 | 128 | `strat_y[8][16]` | sbyte | Geostrategie Y |
| `0x6860` | 26720 | 144 | `tech_first_discovered[72]` | short | Civ, die die Tech zuerst entdeckt hat |
| `0x68F0` | 26864 | 128 | `units_destroyed[8][8]` | short | von Civ i zerstoerte Einheiten der Civ j |
| `0x6970` | 26992 | 3328 | `city_names[256]` | char[13] | Stadtnamens-Pool, 16 Civs x 16 Namen, je 13 Byte roh (NICHT nullterminiert-getrimmt) |
| `0x7670` | 30320 | 2 | `replay_length` | short | genutzte Bytes in replay_data |
| `0x7672` | 30322 | 4096 | `replay_data[4096]` | byte | Replay-/Chronik-Ereignisstrom |
| `0x8672` | 34418 | 44 | `wonder_city_id[22]` | short | Stadt-ID je Weltwunder; Index 0 = WonderEnum.None (ungenutzt) |
| `0x869E` | 34462 | 448 | `lost_units[8][28]` | short | verlorene Einheiten je Typ |
| `0x885E` | 34910 | 576 | `tech_acquired_from[8][72]` | sbyte | Quelle je Tech (Civ-Index / -1) |
| `0x8A9E` | 35486 | 2 | `polluted_squares` | short | Anzahl verschmutzter Felder |
| `0x8AA0` | 35488 | 2 | `pollution_effect_level` | short | Verschmutzungsgrad |
| `0x8AA2` | 35490 | 2 | `global_warming_count` | short | Zaehler globale Erwaermung |
| `0x8AA4` | 35492 | 2 | `game_settings` | ushort | Bitmaske Spieloptionen (Sound, Animationen, ...) |
| `0x8AA6` | 35494 | 260 | `land_pathfind[260]` | byte | Land-Verbindungsgraph, 20x13 Grobraster (siehe Abschnitt 7) |
| `0x8BAA` | 35754 | 2 | `max_tech_count` | short | maximale Tech-Anzahl |
| `0x8BAC` | 35756 | 2 | `player_future_tech` | short | Future-Tech-Zaehler des Menschen |
| `0x8BAE` | 35758 | 2 | `debug_switches` | ushort | Debug-Bitmaske |
| `0x8BB0` | 35760 | 16 | `science_tax_rate[8]` | short | Wissenschaftsrate |
| `0x8BC0` | 35776 | 2 | `next_anthology_turn` | short | Zug, ab dem der nächste Historikerbericht fällig ist (siehe Abschnitt 10) |
| `0x8BC2` | 35778 | 16 | `cumulative_epic_ranking[8]` | short | aufsummierte Platzierungspunkte aus dem Historikerbericht; Slot 0 = Anzahl der Berichte (siehe Abschnitt 10) |
| `0x8BD2` | 35794 | 1440 | `spaceship_data[8][180]` | sbyte | 36 Byte ungenutzt + 12x12 Zellenraster |
| `0x9172` | 37234 | 2 | `spaceship_launched_flags` | ushort | Bitmaske: welche Civ hat gestartet |
| `0x9174` | 37236 | 2 | `player_space_success_rate` | short | Erfolgswahrscheinlichkeit Mensch |
| `0x9176` | 37238 | 2 | `ai_space_success_rate` | short | Erfolgswahrscheinlichkeit AI |
| `0x9178` | 37240 | 16 | `spaceship_eta_year[8]` | short | Ankunftsjahr |
| `0x9188` | 37256 | 24 | `palace_data1[12]` | short | Palast-Bauteile (Teil 1) |
| `0x91A0` | 37280 | 24 | `palace_data2[12]` | short | Palast-Bauteile (Teil 2) |
| `0x91B8` | 37304 | 256 | `city_pos_x[256]` | sbyte | X-Position je Stadtnamen-Slot (Karte) |
| `0x92B8` | 37560 | 256 | `city_pos_y[256]` | sbyte | Y-Position je Stadtnamen-Slot (Karte) |
| `0x93B8` | 37816 | 2 | `palace_level` | short | Ausbaustufe des Palasts |
| `0x93BA` | 37818 | 2 | `peace_turn_count` | short | Zuege ohne Krieg |
| `0x93BC` | 37820 | 2 | `ai_opponents` | short | Anzahl AI-Gegner |
| `0x93BE` | 37822 | 16 | `spaceship_population[8]` | short | Bevoelkerung an Bord |
| `0x93CE` | 37838 | 16 | `spaceship_launch_year[8]` | short | Startjahr |
| `0x93DE` | 37854 | 2 | `civs_identity_flag` | ushort | Bitmaske: Civ-Identitaet bekannt |

**Gesamtlänge: 37.856 Byte (`0x93E0`)** — Ende des letzten Feldes `civs_identity_flag`
bei `0x93DE` + 2.

---

## 3. Record-Layouts

### 3.1 Stadt (`City`, 28 Byte) — 128 Slots ab `0x1508`

| rel. | Größe | Feld | Typ | Bedeutung |
|---:|---:|---|---|---|
| +0 | 4 | `ImprovementFlags` | uint | Bitmaske gebauter Stadtverbesserungen/Wunder |
| +4 | 1 | `Position.X` | byte | Kartenposition X |
| +5 | 1 | `Position.Y` | byte | Kartenposition Y |
| +6 | 1 | `StatusFlag` | byte | Statusbits (Unruhen, Belagerung, …) |
| +7 | 1 | `ActualSize` | sbyte | tatsächliche Einwohnerzahl |
| +8 | 1 | `VisibleSize` | sbyte | für Gegner sichtbare Größe |
| +9 | 1 | `CurrentProductionID` | sbyte | aktuelles Bauprojekt (negativ = Gebäude/Wunder) |
| +10 | 1 | `BaseTrade` | sbyte | Basis-Handel |
| +11 | 1 | `PlayerID` | sbyte | Besitzer-Civ |
| +12 | 2 | `FoodCount` | short | Nahrungsspeicher |
| +14 | 2 | `ShieldsCount` | short | Schildspeicher |
| +16 | 4 | `WorkerFlags` | uint | belegte Arbeitsfelder im 21-Felder-Radius |
| +20 | 2 | `SpecialWorkerFlags` | ushort | Spezialisten (Entertainer/Steuereintreiber/Wissenschaftler) |
| +22 | 1 | `NameID` | byte | Index in den Stadtnamens-Pool (siehe `city_names[256]`) |
| +23 | 3 | `TradeCityIDs[3]` | sbyte | Handelsrouten-Ziele |
| +26 | 2 | `FortifiedUnits[2]` | sbyte | bis zu 2 in der Stadt fortifizierte Einheiten: Bits 0-5 = Einheitentyp-ID, Bit 6 (`0x40`) = Veteranen-Flag, `-1` = leerer Slot; in der Referenzkarte als `fortifiedUnits1/2` bezeichnet |

> Die Referenzkarte splittet `ImprovementFlags` in 4 Einzelbytes (`buildings_flag0..3`)
> und `WorkerFlags`+`SpecialWorkerFlags` in 6 Bytes (`workers_flag0..5`) — identische
> Bytes, nur feinere Granularität.

### 3.2 Einheit (`Unit`, 12 Byte) — 8 × 128 Slots ab `0x26C0`

| rel. | Feld | Typ | Bedeutung |
|---:|---|---|---|
| +0 | `Status` | sbyte | Statusbits (befestigt, schläft, arbeitet …) |
| +1 | `Position.X` | sbyte | Kartenposition X |
| +2 | `Position.Y` | sbyte | Kartenposition Y |
| +3 | `TypeID` | sbyte | Einheitentyp 0–27 |
| +4 | `RemainingMoves` | sbyte | Restbewegung im Zug |
| +5 | `SpecialMoves` | sbyte | Sonderbewegung (Fortbewegungszähler) |
| +6 | `GoToPosition.X` | sbyte | GoTo-Ziel X |
| +7 | `GoToPosition.Y` | sbyte | GoTo-Ziel Y |
| +8 | `GoToNextDirection` | sbyte | nächste Richtung des GoTo-Pfads |
| +9 | `VisibleByPlayer` | byte | Bitmaske, welche Civ die Einheit sieht |
| +10 | `NextUnitID` | sbyte | Verkettung des Stapels auf demselben Feld |
| +11 | `HomeCityID` | sbyte | Heimatstadt (Unterhalt) |

**128 Einheiten-Slots pro Civ**, also 1024 Records = 12.288 Byte.
(Achtung: `Player.Units` ist im Repo `Unit[129]` — Slot 128 existiert nur im
Arbeitsspeicher und wird **nicht** gespeichert.)

### 3.3 Einheiten-Definition (`UnitDefinition`, 34 Byte) — 28 Slots ab `0x2308`

`char[12] Name` (nullterminiert) gefolgt von 11 × `short`:
`CancelTechnology`, `TerrainCategory`, `MoveCount`, `TurnsOutside`,
`AttackStrength`, `DefenseStrength`, `Cost`, `SightRange`,
`TransportCapacity`, `UnitCategory`, `RequiredTechnology`.

Die Einheiten-Regeltabelle wird also **im Spielstand mitgespeichert** — modifizierte
Regeln bleiben an den Savegame gebunden.

### 3.4 Raumschiff (180 Byte je Civ) ab `0x8BD2`

36 Byte ungenutzt, danach ein 12 × 12 Byte großes Zellenraster (Bauteile pro
Rasterposition). Zeilenweise: `cell<row>x<col>`.

---

## 4. Stringkonventionen

Drei verschiedene Varianten kommen vor — beim Neuschreiben eines Parsers wichtig:

1. **Nullterminiert mit fester Feldbreite** (Herrscher-/Civ-/Nationalitätsnamen):
   `n` Nutzbytes + 1 garantiertes NUL-Byte. Der Code schreibt fehlende Zeichen als
   `0x00` auf, liest aber bis zum ersten NUL.
   * Herrschername: 13 + 1 = 14 Byte
   * Civ-Name: 11 + 1 = 12 Byte
   * Nationalität (Adjektiv): 10 + 1 = 11 Byte
2. **Nullterminiert innerhalb 12 Byte** (`UnitDefinition.Name`) — `ReadString(stream, 12)`.
3. **Feste 13 Byte ohne Trim** (`city_names[256]`): der Lader kopiert alle 13 Byte
   unverändert in den String, schneidet also **nicht** am NUL ab.
   In echten DOS-Spielständen sind die Namen NUL-aufgefüllt
   (`"Rome\0\0\0\0\0\0\0\0\0"`, `"Tenochtitlan\0"`), die Default-Tabelle im Repo
   verwendet dagegen Leerzeichen + abschließendes `\0` (`"Rome        \0"`).
   Ein robuster Parser muss beides behandeln: erst am NUL trennen, dann `rstrip()`.

---

## 5. Die begleitende `.MAP`-Datei

Die Weltkarte steckt **nicht** in der `.SVE`. Gespeichert wird sie als PIC-Bild
über den internen Grafikpuffer „Screen 2":

* [GameLoadAndSave.cs:762](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/GameLoadAndSave.cs#L762) — laden
* [GameLoadAndSave.cs:1360](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/GameLoadAndSave.cs#L1360) — speichern (`savePalette = false`)

Format (siehe `GBitmap.SaveToPIC` in [GBitmap.cs:492](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/GPU/GBitmap.cs#L492)):

| Feld | Größe | Wert |
|---|---:|---|
| Block-Signatur | 2 | `'X'` `'0'` (`0x58 0x30`) |
| Blocklänge | 2 | Nutzdaten + 4, little endian |
| Breite | 2 | 320 |
| Höhe | 2 | 200 |
| Daten | n | RLE (Wortbreite 4) → LZW (9–11 Bit) |

Der Puffer ist 320 × 200 Byte groß und wird als **Ebenen-Raster von 80 × 50 Feldern**
genutzt — die Weltkarte ist 80 × 50 Felder, passend zu `map_visibility[80][50]` in der
`.SVE`. Im Code nachweisbare Ebenen-Basispunkte sind `(0,0)`, `(+80,0)`, `(+160,0)`,
`(0,+50)`, `(0,+100)` und `(0,+150)` (z. B.
[Segment_2aea.cs:1839-1879](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/Segment_2aea.cs#L1839-L1879),
[Segment_2aea.cs:751](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/Segment_2aea.cs#L751)).
Zwei Ebenen sind inzwischen eindeutig identifiziert:

| Ebene | Zugriff | Inhalt |
|---|---|---|
| `(x, y)` | `F0_2aea_134a` / `F7_0000_176d` | **Terrain**: Rohbyte, über die Tabelle bei `DS:0x2ba6` auf den Terraintyp abgebildet; Typ `10` = Ozean |
| `(x, y+50)` | `F0_2aea_1942` | **Kontinent-/Ozean-ID** des Feldes |

Die restlichen Ebenen (`+80`, `+160`, `y+100`, `y+150`) sind noch nicht benannt —
Kandidaten sind Ressourcen/Spezialfelder, Feldverbesserungen (Straße, Bewässerung,
Mine) und Stadt-/Einheitenmarkierungen.

---

## 6. Abgleich mit `memory_map_SVE_EN.txt`

Die Vorab-Forschung und der Code stimmen **vollständig** überein:

* Gesamtlänge identisch: 37.856 Byte.
* Alle stichprobenartig geprüften Blockanfänge decken sich exakt
  (`0x0158` active_units, `0x0BA8` score_graph, `0x1508` Städte, `0x2308` unit_defs,
  `0x26C0` Einheiten, `0x56C0` map_visibility, `0x6970` city_names, `0x7670` replay,
  `0x8BD2` Raumschiff, `0x91B8`/`0x92B8` Stadtpositionen, `0x93BE` spaceship_population).

Zwei Stellen weichen nur in der **Beschreibung**, nicht in den Bytes ab:

1. **Replay-Block / Wunder-Grenze.** Die Referenzkarte führt `game.replay_data` als
   einen 4.100-Byte-Block ab `0x7670`. Der Code zerlegt das feiner:
   2 Byte Längenfeld (`ReplayDataLength`) + 4.096 Byte Daten = 4.098 Byte,
   endend bei `0x8672`. Die verbleibenden 2 Byte gehören zum nächsten Block.
2. **Wunder-Indizierung (Off-by-one).** Der Code liest `WonderCityID[22]` ab `0x8672`,
   die Referenzkarte beginnt `wonder0` erst bei `0x8674`. Auflösung: `WonderEnum`
   ist **1-basiert** (`None = 0`, `Pyramids = 1` … `CureForCancer = 21`, siehe
   [WonderEnum.cs](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/CivState/Definitions/WonderEnum.cs)).
   Slot `[0]` ist der ungenutzte `None`-Eintrag und belegt genau die 2 Byte bei `0x8672`.
   **`wonderN` der Referenzkarte entspricht also `WonderEnum` N+1.**

### Namensabweichungen (gleiche Bytes, andere Bezeichner)

| Referenzkarte | Repo-Code | Inhalt |
|---|---|---|
| `civ#/leader` | `Player.Name` | Herrschername („Caesar") |
| `civ#/name` | `Player.Nation` | Civ-Name („Romans") |
| `civ#/nationality` | `Player.Nationality` | Adjektiv („Roman") |
| `civ#.total_advances` | `DiscoveredTechnologyCount` | Anzahl Technologien |
| `city#.fortifiedUnits1/2` | `City.FortifiedUnits[2]` | bestätigt (`Segment_1866`, `CityWorker`) |
| `civ#.geostrategy#` | `Player.StrategicLocations[16]` | AI-Zielpunkte |

---

## 7. Der Block `land_pathfind` (`0x8AA6`, 260 Byte)

Der Block ist **kein Pfad-Cache**, sondern ein vorberechneter
**Konnektivitätsgraph für Landeinheiten** auf einem groben Raster über der Weltkarte.
Er ersetzt die teure Feld-für-Feld-Suche durch eine schnelle Grobplanung.

### 7.1 Geometrie

`260 = 20 × 13`. Der Block ist eine 2D-Matrix über einem Grobraster, das die
80 × 50 Felder große Weltkarte in **4 × 4 Felder große Blöcke** unterteilt:

```
Index = gx * 13 + gy        gx = 0..19   (20 Spalten, 20*4 = 80)
                            gy = 0..12   (13 Zeilen, 13*4 = 52 ≳ 50)
```

Die Schleifengrenzen `0x14` (20) und `0xD` (13) stehen explizit im Code
([GameInitAndIntro.cs:2058](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/GameInitAndIntro.cs#L2058),
[GameInitAndIntro.cs:2331](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/GameInitAndIntro.cs#L2331)).
Die letzte Rasterzeile ragt rechnerisch über den Kartenrand hinaus (52 statt 50).

**Umrechnung Raster → Karte:** der Repräsentant einer Rasterzelle ist
`mapX = gx*4 + 1`, `mapY = gy*4 + 1`
([GameInitAndIntro.cs:2063-2072](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/GameInitAndIntro.cs#L2063-L2072),
Rückrichtung in [UnitGoTo.cs:687-688](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/UnitGoTo.cs#L687-L688)).
Liegt dieses Feld im Ozean (Terraintyp 10), probiert der Erzeuger nacheinander
`(x+1, y)`, `(x, y+1)` und `(x+1, y+1)`; findet sich kein Landfeld, bleibt die
Zelle komplett auf 0.

### 7.2 Bitbelegung eines Bytes

Jedes Byte ist eine **Bitmaske von 8 Himmelsrichtungen**: „von dieser Rasterzelle
ist die Nachbarzelle in Richtung *d* über Land erreichbar".

| Bit | Wert | Richtung | `MoveOffsets`-Index | Δgx, Δgy |
|---:|---:|---|---:|---|
| 0 | `0x01` | N  | 1 | 0, −1 |
| 1 | `0x02` | NE | 2 | +1, −1 |
| 2 | `0x04` | E  | 3 | +1, 0 |
| 3 | `0x08` | SE | 4 | +1, +1 |
| 4 | `0x10` | S  | 5 | 0, +1 |
| 5 | `0x20` | SW | 6 | −1, +1 |
| 6 | `0x40` | W  | 7 | −1, 0 |
| 7 | `0x80` | NW | 8 | −1, −1 |

Die Richtungsindizes entsprechen `MoveOffsets[1..8]` in
[CivGameGlobals.cs:16](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/CivGameGlobals.cs#L16).
Der Erzeuger iteriert nur über die Richtungen **1–4** (N, NE, E, SE) und setzt
pro gefundener Verbindung **zwei** Bits:

* in der Ausgangszelle Bit `d − 1`
  ([GameInitAndIntro.cs:2274](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/GameInitAndIntro.cs#L2274)),
* in der Zielzelle das Gegenrichtungs-Bit `(d + 3) & 7`
  ([GameInitAndIntro.cs:2322](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/GameInitAndIntro.cs#L2322)).

Die Matrix ist damit **symmetrisch** — ein ungerichteter Graph.
Einzige Ausnahme ist der Ostrand, siehe 7.7.

### 7.3 Wie die Kanten entstehen

Erzeuger: `F7_0000_1188()` in
[GameInitAndIntro.cs:1987](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/GameInitAndIntro.cs#L1987).
Pro Zellenpaar (Zelle, Nachbar in Richtung 1–4):

1. Beide Zellen brauchen ein Land-Ankerfeld (siehe 7.1), sonst keine Kante.
2. Beide Ankerfelder müssen **auf derselben Landmasse liegen** — verglichen wird
   die Kontinent-ID aus der `.MAP`-Ebene `(x, y+50)` (`F0_2aea_1942`).
3. Dann läuft eine echte Pfadsuche `F0_2e31_111c(x1, y1, x2, y2, 0, 20)`.
   Das Ergebnis muss `!= -1` **und `< 20`** sein
   ([GameInitAndIntro.cs:2245-2259](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/GameInitAndIntro.cs#L2245-L2259)).

Punkt 3 ist der Grund, warum der Block überhaupt gespeichert werden muss: er
kodiert nicht bloß „gleicher Kontinent", sondern „in höchstens 20 Schritten
tatsächlich begehbar" — inklusive Engstellen, die über die Kontinent-ID nicht
erkennbar sind.

### 7.4 Wie er benutzt wird

In [UnitGoTo.cs](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/UnitGoTo.cs) plant der GoTo-Befehl zuerst
auf dem Grobraster und erst danach feldgenau:

* `LandPathfinding[gx*13 + gy] != 0` = „diese Zelle gehört zum Wegenetz"
  ([UnitGoTo.cs:889](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/UnitGoTo.cs#L889),
  [UnitGoTo.cs:963](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/UnitGoTo.cs#L963)).
* Die einzelnen Bits steuern, welche Nachbarzelle als nächster Grobschritt in
  Frage kommt ([UnitGoTo.cs:666-682](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/UnitGoTo.cs#L666-L682)).
* Die gefundene Grobzelle wird mit `<< 2` bzw. `* 4 + 1` in Kartenkoordinaten
  zurückgerechnet.

### 7.5 Das nicht gespeicherte Gegenstück: der Seegraph

Es gibt eine **zweite, strukturell identische 260-Byte-Tabelle** für Seeeinheiten
bei `DS:0x7f38` (im Port noch nicht als Feld herausgezogen). Sie wird von
`F7_0000_1440()` erzeugt
([GameInitAndIntro.cs:2349](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/GameInitAndIntro.cs#L2349),
beginnend mit `memset(0x7f38, 0, 0x104)` = 260 Byte) und ist das exakte Spiegelbild:
dort gilt ein Ankerfeld nur dann, wenn es **Ozean** ist (Terraintyp 10).

Welche der beiden Tabellen gelesen wird, entscheidet
`UnitDefinition.TerrainCategory` (`0` = Land, `1` = Luft, `2` = See) —
siehe [UnitGoTo.cs:657](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/UnitGoTo.cs#L657).

**Der Seegraph steht nicht im Spielstand.** Beim Laden wird nur er neu berechnet
([GameLoadAndSave.cs:266](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/GameLoadAndSave.cs#L266) ruft
`F7_0000_1440(0)`), der Landgraph dagegen ausschließlich aus der `.SVE` übernommen.
Beim Start eines neuen Spiels laufen beide Erzeuger
([GameInitAndIntro.cs:1656-1658](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/GameInitAndIntro.cs#L1656-L1658)).

Der Seegraph bekommt zusätzlich eine **Ost-West-Umlauf-Korrektur**, die der
Landgraph nicht hat: Spalte `gx = 0` erhält `|= 0xE0` (SW, W, NW), Spalte `gx = 19`
erhält `|= 0x0E` (NE, E, SE); anschließend werden die vier Polecken-Diagonalen
wieder gelöscht
([GameInitAndIntro.cs:2766-2779](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/GameInitAndIntro.cs#L2766-L2779)).
Das ist zugleich eine unabhängige Bestätigung der Bitzuordnung aus 7.2:
am Westrand werden genau die drei nach Westen zeigenden Bits gesetzt.

### 7.6 Konsequenz für Savegame-Editoren

Wer Terrain in der `.MAP` verändert (Land aufschütten, Kanäle graben), muss
`land_pathfind` **mit neu berechnen** — sonst laufen Landeinheiten weiter nach der
alten Topologie, während der Seegraph beim Laden automatisch aktualisiert wird.
Ein sicherer Notbehelf: den Block auf 0 setzen wirkt wie „kein Wegenetz bekannt"
und degradiert nur die KI-/GoTo-Wegplanung, statt falsche Wege zu erzeugen.

---

### 7.7 Empirische Bestätigung (und ein Quirk am Ostrand)

Gegen die echten Spielstände geprüft (`~/projekte/civ_orig/CIVIL0-3.SVE`),
Block ab `0x8AA6` als 20 × 13 gelesen:

| Datei | belegte Zellen | gesetzte Richtungsbits | Symmetrie-Verletzungen |
|---|---:|---:|---:|
| `CIVIL0` | 76 / 260 | 234 | **0** |
| `CIVIL1` | 157 / 260 | 290 | 8 |
| `CIVIL2` | 157 / 260 | 290 | 8 |
| `CIVIL3` | 157 / 260 | 290 | 8 |

Die Symmetrie-Vorhersage aus 7.2 hält also — bei `CIVIL0` exakt, bei den übrigen
bis auf genau 8 Bits. Diese 8 liegen **alle in Spalte `gx = 19`** und sind
**ausschließlich ostwärts gerichtet** (E, NE, SE):

```
(19,2) E   (19,6) NE  (19,6) E   (19,7) NE
(19,7) SE  (19,9) E   (19,9) SE  (19,12) E
```

Das ist kein Lesefehler, sondern eine Eigenheit des Erzeugers: in
[GameInitAndIntro.cs:2274](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/GameInitAndIntro.cs#L2274)
wird **erst das Bit der Ausgangszelle gesetzt** und **danach** geprüft, ob die
Zielzelle überhaupt im Raster liegt
([GameInitAndIntro.cs:2290-2305](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/GameInitAndIntro.cs#L2290-L2305),
Schranken `0 ≤ gx < 20`, `0 ≤ gy < 13`). Liegt sie außerhalb, wird das
Gegenrichtungs-Bit nie gesetzt — das ausgehende Bit bleibt aber stehen.

Dass die Verbindungsprüfung für ein Ziel jenseits von `gx = 19` überhaupt
anschlägt, passt zur zylindrischen Civ-Welt: die Pfadsuche läuft über den
Ostrand hinaus um die Karte herum. Der Graph *hätte* also nach `gx = 0` wrappen
müssen — der Erzeuger verwirft diese Kante aber halb.

Dass solche losen Bits nur am **Ost**rand auftreten können, folgt direkt aus 7.2:
der Erzeuger iteriert nur die Richtungen 1–4 (N, NE, E, SE); alle westwärts
gerichteten Bits entstehen ausschließlich als Gegenrichtungs-Bit und setzen damit
eine gültige Zielzelle voraus.

**Für Parser und Editoren heißt das:** nicht auf perfekte Symmetrie verlassen,
und ein gesetztes Bit in Spalte 19 nach Osten nicht als Datenfehler behandeln.
Der Seegraph (7.5) hat dieses Problem nicht — er bekommt die Umlauf-Bits
nachträglich explizit gesetzt.

---

## 8. Der Block `diplomacy` (`0x0648`, 128 Byte)

8 × 8 `ushort`: `diplomacy[i][j]` beschreibt das Verhältnis von Civ *i* zu Civ *j*.
Index = `i * 8 + j`, Offset = `0x0648 + (i*8 + j) * 2`.
Die Diagonale `[i][i]` ist ungenutzt (in allen geprüften Spielständen 0).

10 der 16 Bits sind belegt:

| Bit | Wert | Name | Bedeutung |
|---:|---:|---|---|
| 0 | `0x0001` | Kontakt | Die beiden Civs kennen sich. **Kontakt ohne Friedensvertrag = Kriegszustand** — der kanonische Test im Code ist `(dip & 3) == 1`. |
| 1 | `0x0002` | Friedensvertrag | Waffenstillstand/Frieden. Setzen = Friedensschluss, Löschen = Kriegserklärung. |
| 2 | `0x0004` | Bündnis | Allianz; setzt Bit 1 voraus und wird mit ihm gelöscht. |
| 3 | `0x0008` | Vendetta | Blutfehde — dauerhafte KI-Feindseligkeit, überlebt Friedensschlüsse. |
| 4 | `0x0010` | Frisch | Beim Erstkontakt zusammen mit Bit 0 gesetzt; blockiert einmalig den 16-Zug-Diplomatietick und wird dort wieder gelöscht. |
| 5 | `0x0020` | Aggression | Es hat Kampfhandlungen gegeben. Wird beim Friedensschluss gelöscht. |
| 6 | `0x0040` | Botschaft | *i* unterhält eine Botschaft bei *j* (Diplomat-Aktion). Schaltet Geheimdienstanzeigen frei. |
| 7 | `0x0080` | Nukleare Drohung | *i* hat *j* gegenüber die Atomwaffenkarte gespielt. Einmal gesetzt, **nie wieder gelöscht**. Siehe 8.4. |
| 8 | `0x0100` | Bruch geplant | *i* hat den Vertragsbruch angekündigt bzw. plant ihn. |
| 9 | `0x0200` | Bruch vollzogen | *i* hat einen Friedensvertrag tatsächlich gebrochen (Wortbruch-Ruf). |

### 8.1 Symmetrie

Die Bits zerfallen in zwei Gruppen:

* **Symmetrisch** (immer in beiden Richtungen gesetzt/gelöscht): Bits 0, 1, 2, 4, 5.
  Kontakt, Verträge, Bündnis und Aggression sind beidseitige Zustände.
  Die Helfer `F0_2517_0a30_SetDiplomacyFlags(i, j, maske)` und
  `F0_2517_0aa1_ClearDiplomacyFlags(i, j, maske)`
  ([Segment_2517.cs](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/Segment_2517.cs)) schreiben grundsätzlich
  `[i][j]` **und** `[j][i]`.
* **Asymmetrisch** (Haltung einer Seite): Bits 3, 6, 7, 8, 9.
  Vendetta, Botschaft und Vertragsbruch gehören jeweils nur einer Partei.

Beide Helfer haben Sonderregeln für die Maske `2`:

| Aufruf | Wirkung | Replay-Ereignis |
|---|---|---|
| `Set(i, j, 2)` | setzt Bit 1 beidseitig **und löscht Bit 5** (Aggression) | ID 3 = „make peace with" |
| `Clear(i, j, 2)` | löscht Bit 1 beidseitig **und löscht Bit 2** (Bündnis) | ID 2 = „declare war on" |

Das Replay-Ereignis ist zugleich der Beweis für die Polarität von Bit 1:
**Bit 1 löschen erzeugt die Kriegserklärung**, Bit 1 setzen den Friedensschluss.
(Die Ereignis-IDs stehen im Replay-Strom als High-Nibble; der Abspielcode
schaltet auf `ID − 1`, deshalb landet ID 2 in `case 1` mit dem Text
„declare war on" — siehe [GameReplay.cs:235](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/GameReplay.cs#L235).)

### 8.2 Der 16-Zug-Tick: Kriege laufen aus

Alle 16 Züge (`TurnCount & 0xF == 0`) läuft in
[Segment_2517.cs:119-221](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/Segment_2517.cs#L119-L221) über jedes
Paar:

1. Ist Bit 4 gesetzt (frischer Kontakt), wird nur Bit 4 gelöscht — sonst nichts.
2. Sonst, wenn Bit 0 gesetzt und Bit 1 **nicht** gesetzt (= Kriegszustand):
   Falls der menschliche Spieler zu einer der beiden Parteien eine Botschaft (Bit 6)
   unterhält und selbst nicht beteiligt ist, erscheint
   „The war between the X and the Y has ended."
3. Danach wird **Bit 0 beidseitig gelöscht** — der Kontakt verfällt, und mit ihm der
   Kriegszustand.

Das ist der Grund, warum Kontakt und Kriegszustand im selben Bit stecken:
Civ 1 kennt keinen separaten Kriegs-Zustand, sondern nur „kennt sich, ohne Vertrag".

### 8.3 Anzeige-Semantik

Die Statusanzeige in
[Overlay_13.cs:228-265](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/Overlay_13.cs#L228-L265) leitet ihre
Labels direkt aus den Bits ab und bestätigt die Zuordnung:

| Test | Label |
|---|---|
| `(dip & 0x0B) == 0x01` | `: Contact` |
| `(dip & 0x02) != 0` und `(dip & 0x04) == 0` | `: Peace` |
| `(dip & 0x02) != 0` und `(dip & 0x04) != 0` | `: Allied` |
| `(dip & 0x08) != 0` | `: Vendetta` |
| `(dip & 0x0B) == 0` | Zeile wird gar nicht ausgegeben |

### 8.4 Belege für die Einzelbits

* **Bit 0 + Bit 4** — beim Erstkontakt setzt
  [Segment_2517.cs:791-800](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/Segment_2517.cs#L791-L800)
  beidseitig `|= 0x11`.
* **Bit 2** — wird nie eigenständig gelöscht, sondern immer zusammen mit Bit 1
  (`Clear(...,2)` maskiert zusätzlich `~0x4`): ein Bündnis ohne Vertrag gibt es nicht.
* **Bit 3** — `|= 9` (Kontakt + Vendetta) beim Entstehen einer neuen Civ
  ([Overlay_15.cs:304-313](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/Overlay_15.cs#L304-L313)),
  `|= 0x88` nach einem Flächenangriff
  ([Segment_29f3.cs:1454](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/Segment_29f3.cs#L1454)).
  > **Portierungsfehler an dieser Stelle:** In
  > [Overlay_15.cs:313](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/Overlay_15.cs#L313) steht
  > `Players[playerID].Diplomacy[j] |= 9`, obwohl die darüber berechneten Register
  > (`DI = j << 4`, `BX = playerID << 1`) die Gegenrichtung adressieren:
  > korrekt wäre `Players[j].Diplomacy[playerID] |= 9`. Das Original setzt hier
  > beide Richtungen (wie in `Segment_2517` bei `|= 0x11`), der Port schreibt
  > zweimal dieselbe Zelle.
* **Bit 5** — beidseitig gesetzt im Kampf-Handler
  ([Segment_29f3.cs:783-792](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/Segment_29f3.cs#L783-L792)),
  direkt neben `PeaceTurnCount = 0`; Barbaren (Civ 0) sind davon ausgenommen.
* **Bit 6** — gesetzt durch die Diplomaten-Aktion „Botschaft errichten"
  ([Overlay_22.cs:173](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/Overlay_22.cs#L173)). Der zweite
  Setzer in [Segment_1ade.cs:3162-3175](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/Segment_1ade.cs#L3162-L3175)
  ist der Botschafteraustausch — im Port ist direkt vermerkt, dass diese Regel im
  Original **nie greift** (die Bedingung kann nie wahr werden).
* **Bit 7** — im gesamten Code gibt es genau **sechs** Fundstellen, drei schreibende
  und drei lesende; siehe 8.4.1.
* **Bit 8 → Bit 9** — die KI setzt Bit 8 gegenüber dem Menschen, wenn sie trotz
  Vertrag mehr als eine Stadt hat und einen Bruch vorbereitet
  ([Segment_1238.cs:1465-1468](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/Segment_1238.cs#L1465-L1468)),
  bzw. nach einer angekündigten Kriegserklärung
  ([MeetWithKing.cs:1914](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/MeetWithKing.cs#L1914)).
  Im Tick wird daraus mit 1/3 Wahrscheinlichkeit der vollzogene Bruch:
  `&= ~0x102` (Bit 8 und Friedensvertrag weg), `|= 0x200`
  ([Segment_2517.cs:107-117](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/Segment_2517.cs#L107-L117)).

#### 8.4.1 Bit 7 (`0x0080`) im Detail

Alle sechs Fundstellen sind nuklear motiviert — drei schreibende, drei lesende:

| Stelle | Art | Was passiert |
|---|---|---|
| [MeetWithKing.cs:285-295](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/MeetWithKing.cs#L285-L295) | setzt | Audienz beim KI-Herrscher: besitzt die KI Nuklearwaffen (`ActiveUnits[25]`, Einheitentyp 25 = *Nuclear*), wird `0x80` gegenüber dem Menschen gesetzt. |
| [MeetWithKing.cs:430-461](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/MeetWithKing.cs#L430-L461) | **liest und setzt** | Tributforderung — die Schlüsselstelle, siehe unten. |
| [Segment_29f3.cs:1454](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/Segment_29f3.cs#L1454) | setzt | **Atomschlag.** Der Handler gibt bei geschützten Städten „SDI protects …" aus ([Zeile 1407](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/Segment_29f3.cs#L1407)); für jede Civ, die Einheiten verliert, bekommt die Zeile des Angreifers `\|= 0x88` — Vendetta **und** Bit 7. |
| [Segment_25fb.cs:1734](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/Segment_25fb.cs#L1734) | liest | Stadtverteidigung: für jedes der 8 Nachbarfelder wird der Besitzer der dort stehenden Einheit ermittelt (`F0_2aea_14e0`); gilt `(dip & 0x82) == 0x80`, zählt er als Bedrohung. |
| [Segment_25fb.cs:1859](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/Segment_25fb.cs#L1859) | liest | Dieselbe Prüfung in der übergeordneten Bedrohungsbewertung, danach Vergleich mit der Stadtgröße. |
| — | — | **Nirgends wird das Bit gelöscht.** Es gibt kein `&= ~0x80` im gesamten Code. |

**Die Tributforderung** ([MeetWithKing.cs:425-461](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/MeetWithKing.cs#L425-L461))
erklärt, wozu das Bit überhaupt existiert:

1. Die geforderte Summe wird aus Schwierigkeitsgrad und Rangwert berechnet und auf
   Vielfache von 50 gerundet.
2. Besitzt die KI keine Nuklearwaffen, passiert nichts weiter.
3. Besitzt sie welche **und ist Bit 7 noch nicht gesetzt**, wird die Forderung auf
   das gedeckelt, was der Mensch tatsächlich bezahlen kann
   (`Coins`, abgerundet auf Vielfache von 50).
4. Danach wird Bit 7 gesetzt.

Bit 7 ist also ein **Einmal-Schalter: nur die erste atomar unterfütterte Forderung
wird auf das Zahlbare heruntergerechnet.** Jede weitere Forderung derselben KI darf
die Zahlungsfähigkeit des Menschen überschreiten.

Ergänzend prüft die Routine kurz davor
([MeetWithKing.cs:266-269](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/MeetWithKing.cs#L266-L269)), ob der
**Mensch** selbst Nuklearwaffen besitzt; dann entfällt die Eskalation ganz —
gegenseitige Abschreckung.

**Damit ist die Bedeutung gesichert:** Bit 7 steht in der Zeile von *i* und heißt
„*i* hat gegenüber *j* die Atomwaffenkarte ausgespielt" — sei es als Drohung bei einer
Audienz oder Tributforderung, sei es durch einen tatsächlichen Atomschlag. Alle drei
Setzer hängen an Nuklearwaffen, beide Leser werten es als dauerhafte Bedrohung, und
weil es nie gelöscht wird, gilt es für den Rest der Partie.

### 8.5 Empirische Bestätigung

Matrix aus den echten Spielständen gelesen (`~/projekte/civ_orig/CIVIL0-3.SVE`):

* **Null Asymmetrien** bei den Bits 0, 1, 2, 4 und 5 in allen vier Dateien — exakt die
  Bits, die der Code beidseitig schreibt. Die Diagonale ist überall 0.
* `CIVIL1` enthält als einzige Datei belegte Einträge, und zwar durchweg den Wert
  **`0x0013`** (Kontakt + Friedensvertrag + Frisch), symmetrisch zwischen
  Shaka ↔ Ramesses und Ramesses ↔ Genghis Khan. Das ist genau das Muster eines
  frisch geschlossenen Friedensvertrags: `|= 0x11` beim Erstkontakt, danach `|= 0x2`.

### 8.6 Hinweise für Parser und Editoren

* Wer Krieg/Frieden setzen will, muss **beide** Richtungen schreiben — einseitige
  Änderungen an den Bits 0, 1, 2, 4, 5 erzeugen einen Zustand, den das Spiel selbst
  nie herstellt.
* „Im Krieg mit *j*" ist `(dip[i][j] & 3) == 1`, nicht ein eigenes Kriegsbit.
* Bit 4 (`0x10`) beim Setzen von Kontakt besser mitsetzen oder weglassen — es wird
  ohnehin beim nächsten 16-Zug-Tick gelöscht.
* Civ 0 ist der Barbaren-Slot; für ihn gelten Sonderregeln (keine Aggression,
  kein `PeaceTurnCount`-Reset).

---

## 9. Der Block `continent_strategy` (`0x0548`, 256 Byte)

8 × 16 `short`: `continent_strategy[civ][kontinent]` hält die **aktuelle
KI-Marschrichtung** einer Civ für einen Kontinent.
Index = `civ * 16 + kontinent`, Offset = `0x0548 + (civ*16 + kontinent) * 2`.
Die 16 Kontinent-Slots sind dieselben wie bei `continent_attack`, `continent_defense`
und `continent_city_count`.

### 9.1 Wertebereich

Die Werte sind **keine eigene Aufzählung**, sondern identisch mit
`UnitDefinition.UnitCategory` — das ist der ganze Trick des Feldes (siehe 9.3):

| Wert | Bedeutung | Anzeige |
|---:|---|---|
| 0 | **Settle** — besiedeln, es sind noch Bauplätze frei | `S` / `Settle` |
| 1 | **Attack** — angreifen | `A` / `Attack` |
| 2 | **Defend** — verteidigen | `D` / `Defend` |
| 5 | **Transport** — per Schiff auf einen anderen Kontinent ausweichen | `T` / `Transport` |

3 und 4 werden nie zugewiesen. Die Klartextnamen stammen aus den
Debug-Anzeigen: [Overlay_13.cs:141-163](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/Overlay_13.cs#L141-L163)
(`Settle`/`Attack`/`Defend`/`Transport`) und
[Overlay_10.cs:347-381](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/Overlay_10.cs#L347-L381)
(Kurzform `S`/`A`/`D`/`T`). Beide Anzeigen blenden einen Kontinent aus, wenn
`continent_attack == 0` — das ist auch der Test für „diese Civ ist auf dem Kontinent
überhaupt präsent".

### 9.2 Wie der Wert zustande kommt

Die Neubewertung läuft in `F0_25fb_0004`
([Segment_25fb.cs:640-815](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/Segment_25fb.cs#L640-L815)),
pro Civ über alle 16 Kontinent-Slots, in dieser Reihenfolge — spätere Regeln
überschreiben frühere:

1. **Grundwert `Settle` (0).** Ist der Kontinent voll — entweder
   `(eigene Angriffskraft + fremde Kraft) * 2 > Kontinentgröße` oder die eigenen
   Städte belegen bereits alle `build_site_count`-Plätze — und beherrscht die Civ
   **Kartografie** (`Mapmaking`), wird daraus `Transport` (5).
   Ohne Kartografie bleibt es bei `Settle`.
2. **`Attack` (1)**, wenn es auf dem Kontinent lohnende Ziele gibt: eine fremde Civ
   ist dort präsent, man ist mit ihr im Krieg (`(dip & 3) == 1`) oder plant den
   Vertragsbruch (`dip & 0x100`), und die eigene Angriffskraft übersteigt deren
   Verteidigung. Barbarenstädte auf dem Kontinent zählen ebenfalls als Ziel.
3. **`Defend` (2)**, wenn eine Bedrohung überwiegt — ein dort präsenter Gegner ist
   stärker als `eigene Verteidigung / 2 + eigener Angriff`. Schlägt Regel 1 und 2.
4. **Sonderfall, zuletzt geprüft und damit stärker als alles davor:** Ist die Civ auf
   dem Kontinent gar nicht vertreten
   (`continent_attack == 0 && continent_city_count == 0`) und sind dort nicht
   *ausschließlich* Vertragspartner ansässig, wird `Attack` gesetzt — ein leerer oder
   feindlich besetzter Kontinent ist ein Invasionsziel.
   Die letzte Bedingung steckt in einer 2-Bit-Zusammenfassung über alle dort
   präsenten Fremd-Civs ([Segment_25fb.cs:604-623](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/Segment_25fb.cs#L604-L623)):
   Bit 0 = „mindestens eine mit Friedensvertrag", Bit 1 = „mindestens eine ohne".
   Nur beim Wert genau 1 — alle Anwesenden sind Vertragspartner — unterbleibt die
   Invasionsplanung.

Ändert sich der Wert gegenüber dem Vorzustand und ist die Civ KI-gesteuert
(Bit in `player_flags` nicht gesetzt), ruft der Code `F0_25fb_3459(civ, kontinent)`
auf — die Neuplanung der Einheiten auf diesem Kontinent.

### 9.3 Wozu die Zahl gut ist

Der Wert ist deshalb mit `UnitDefinition.UnitCategory` identisch, weil er direkt
gegen die Kategorie der gerade gebauten Einheit verglichen wird
([CityWorker.cs:1325-1331](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/CityWorker.cs#L1325-L1331)):

```csharp
if ((strategie == 1 || strategie == 2 || strategie == 5) && ...
    UnitDefinitions[city.CurrentProductionID].UnitCategory == strategie)
{
    budget = Players[civ].Coins / 64;   // Produktion wird beschleunigt
}
```

Baut eine KI-Stadt also gerade eine Einheit, deren Kategorie zur Strategie ihres
Kontinents passt, darf sie bis zu 1/64 der Staatskasse in den Kauf stecken.
`continent_strategy` ist damit der Hebel, über den die KI ihre Bauproduktion auf
die Lage des jeweiligen Kontinents ausrichtet.

> **Verdacht auf Portierungsfehler:** In
> [CityWorker.cs:1325](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/CityWorker.cs#L1325) sind die beiden
> Indizes vertauscht — dort steht
> `Players[kontinentID & 7].Continents[civ].Strategy`, während jede andere Stelle
> `Players[civ].Continents[kontinentID].Strategy` verwendet. Das `& 7` ist laut
> Kommentar im Code nachträglich gegen einen Überlauf eingefügt worden; genau dieser
> Überlauf ist das Symptom der Vertauschung, denn Kontinent-IDs gehen bis 15, der
> Civ-Index nur bis 7. Korrekt wäre
> `Players[this.Var_6548_PlayerID].Continents[kontinentID].Strategy`.
> (Bemerkenswert: Im Original läge der überlaufende Zugriff genau im unmittelbar
> folgenden Block — `diplomacy` beginnt bei `0x0548 + 256 = 0x0648`.)

---

### 9.4 Empirische Bestätigung

Aus den echten Spielständen gelesen (alle 8 × 16 Werte je Datei):

| Datei | Zug | vorkommende Werte | davon auf Kontinenten mit `continent_attack != 0` |
|---|---:|---|---|
| `CIVIL0` | 11 | 0 (75×), 1 (53×) | nur 0 (3×) |
| `CIVIL1` | 79 | 0 (23×), 1 (105×) | nur 0 (7×) |
| `CIVIL2` | 3 | 0 (63×), 1 (65×) | nur 0 (8×) |
| `CIVIL3` | 3 | 0 (89×), 1 (39×) | nur 0 (8×) |

Das deckt sich mit der Herleitung:

* Es treten **ausschließlich Werte aus {0, 1, 2, 5}** auf — hier 0 und 1.
* Auf Kontinenten, auf denen die Civ tatsächlich präsent ist, steht durchweg
  `Settle` — frühe Partien, Bauplätze noch reichlich vorhanden (Regel 1).
* Alle `Attack`-Einträge liegen auf Kontinenten ohne eigene Präsenz — genau der
  Sonderfall aus Regel 4.
* `Defend` (2) und `Transport` (5) fehlen erwartungsgemäß: es gibt noch keine
  überlegenen Gegner und noch keine Kartografie.

---

## 10. Der Block `cumulative_epic_ranking` (`0x8BC2`, 16 Byte)

8 × `short`, ein Wert je Civ — die über die ganze Partie **aufsummierten
Platzierungspunkte aus dem Historikerbericht**.

### 10.1 Der Historikerbericht

Gemeint ist der periodische Bildschirm
„*&lt;Historiker&gt; completes his epic history: 'The &lt;Adjektiv&gt; Civilizations in the
World'*" — `F12_0000_09e2` in
[WorldMap.cs:938-1250](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/WorldMap.cs#L938-L1250).

Er wird von [Segment_1238.cs:501-510](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/Segment_1238.cs#L501-L510)
ausgelöst, sobald `TurnCount >= next_anthology_turn` (`0x8BC0`); danach wird
neu terminiert:

```
Spielstart:  next_anthology_turn = 80 + rnd(50)          // also 80..129
danach:      next_anthology_turn = TurnCount + 20 + rnd(40)
```

Der Startwert 80 steht in [CivState.cs:21](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/CivState/CivState.cs#L21),
der Zuschlag in [StartGameMenu.cs:330-332](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/StartGameMenu.cs#L330-L332).
Der erste Bericht kommt also frühestens in Zug 80.

Beim Aufruf würfelt der Bildschirm eine Zahl 0–4 aus, die **gleichzeitig das
Titel-Adjektiv und das Ranglisten-Kriterium** bestimmt:

| Wert | Sortierschlüssel | Quelle |
|---:|---|---|
| 0 | `cash` (Reichtum) | `Players[i].Coins` |
| 1 | `military_power` | `Players[i].MilitaryPower` |
| 2 | Anzahl erforschter Technologien | Schleife über alle 72 Techs |
| 3 | Zufriedenheit: Summe über alle Städte von `Stadtgröße + Zufriedene − Unzufriedene` | `F0_1d12_0045_ProcessCityState` je Stadt |
| 4 | Bevölkerung: Summe aller `city.ActualSize` | alle 128 Stadt-Slots |

### 10.2 Die Punktvergabe

Anschließend werden die aktiven Civs nach diesem Schlüssel absteigend ausgegeben,
und jede bekommt Punkte nach ihrem Platz
([WorldMap.cs:1207-1209](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/WorldMap.cs#L1207-L1209)):

```
cumulative_epic_ranking[civ] += 7 - platz      // platz 0 = bester
```

Der Erstplatzierte erhält also +7, der Achtplatzierte +0.

### 10.3 Slot 0 ist ein Zähler, kein Civ-Wert

Zum Abschluss jedes Berichts läuft
[WorldMap.cs:1232](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/WorldMap.cs#L1232):

```csharp
Players[0].CumulativeEpicRanking++;
```

Civ 0 ist der Barbaren-Slot und nimmt an keiner Rangliste teil. Der Slot wird hier
zweckentfremdet und **zählt, wie oft der Historikerbericht bisher lief**. Damit lässt
sich die Durchschnittsplatzierung einer Civ rekonstruieren:

```
Ø-Platz(civ) = 7 - cumulative_epic_ranking[civ] / cumulative_epic_ranking[0]
```

Initialisiert wird das Feld beim Spielstart mit 0
([StartGameMenu.cs:646](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/StartGameMenu.cs#L646)); es wird
danach **nie zurückgesetzt oder verringert** — der Wert wächst monoton.

> **Für Editoren:** `cumulative_epic_ranking[0]` mitzupflegen ist Pflicht, wenn man
> die anderen Werte anfasst — sonst kippt jede Auswertung, die durch den Zähler
> teilt. Sinnvolle Plausibilitätsprüfung: die Summe aller Civ-Werte sollte grob
> `Anzahl aktiver Civs × Ø-Punkte × cumulative_epic_ranking[0]` entsprechen.

---

### 10.4 Empirische Bestätigung

In allen vier Spielständen ist `cumulative_epic_ranking` **komplett 0** — und das ist
genau das erwartete Ergebnis:

| Datei | Zug | `next_anthology_turn` | Berichte (Slot 0) |
|---|---:|---:|---:|
| `CIVIL0` | 11 | 107 | 0 |
| `CIVIL1` | 79 | 104 | 0 |
| `CIVIL2` | 3 | 104 | 0 |
| `CIVIL3` | 3 | 104 | 0 |

Kein Spielstand hat den ersten Historikerbericht erreicht, folglich sind sowohl der
Zähler in Slot 0 als auch alle Punktestände 0. Die drei Termine 104/104/107 liegen
im Startintervall `80..129` und bestätigen die Formel `80 + rnd(50)`; `CIVIL1` steht
mit Zug 79 unmittelbar davor.

---

## 11. Offene Punkte

* **Ebenen-Semantik der `.MAP`** — größte verbleibende Lücke. Terrain `(x, y)` und
  Kontinent-ID `(x, y+50)` sind identifiziert (Abschnitt 5), die Ebenen `+80`, `+160`,
  `y+100` und `y+150` noch nicht.
* ~~`City.Unknown[2]`~~ — bestätigt und in `City.FortifiedUnits[2]` umbenannt:
  Cache der bis zu 2 in der Stadt fortifizierten Einheiten (Typ-ID + Veteranen-Flag),
  wird u.a. in `Segment_1866` (Auf-/Abbau des Caches, Wiederherstellung der
  echten Einheiten) und `CityWorker`/`Segment_25fb` (Verteidigungsstärke,
  Zufriedenheit durch Militärpräsenz) verwendet.
* `game_settings` (`0x8AA4`) und `debug_switches` (`0x8BAE`) — Bitbelegung
  nicht dokumentiert.
* `replay_data` (`0x7672`) — Grundstruktur beim Diplomatie-Abgleich mitgefallen,
  aber noch nicht vollständig kartiert: Jeder Eintrag beginnt mit zwei Bytes
  **big endian**, `Ereignis-ID` in den oberen 4 Bit und `Zug` in den unteren 12 Bit,
  danach folgen 1–2 Nutzbytes (siehe `F0_1866_250e_AddReplayData` in
  [Segment_1866.cs](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/Segment_1866.cs)). Bekannte IDs: 1 = Stadt
  gegründet/zerstört, 2 = Kriegserklärung, 3 = Friedensschluss. `replay_length`
  (`0x7670`) zählt die belegten Bytes; der Abspieler schaltet auf `ID − 1`.
  Die übrigen IDs (4–13) sind noch nicht zugeordnet.
* `PalaceData1` ist im Repo `short[15]`, gespeichert werden aber nur die Indizes
  2..13 (12 Werte). Die Slots 0, 1 und 14 sind reine Laufzeitdaten.
* Referenzkarte kennt `continent_size_unused` / `ocean_size_unused` — im Code werden
  je 64 Einträge gelesen, ob alle genutzt sind, ist nicht geprüft.

---

## 12. Minimal-Parser (Referenz)

```python
import struct

def load_sve(path):
    b = open(path, 'rb').read()
    assert len(b) == 37856, f"unerwartete Groesse: {len(b)}"
    u16 = lambda o: struct.unpack_from('<H', b, o)[0]
    i16 = lambda o: struct.unpack_from('<h', b, o)[0]
    # Namen sind nullterminiert ODER leerzeichen-aufgefuellt (z.B. "Attila       ")
    cstr = lambda o, n: b[o:o+n].split(b'\0')[0].decode('latin-1').rstrip()

    g = {
        'turn':        i16(0x0000),
        'human_civ':   i16(0x0002),
        'human_flags': u16(0x0004),
        'seed':        u16(0x0006),
        'year':        i16(0x0008),
        'difficulty':  i16(0x000A),
        'active_civs': u16(0x000C),
        'research_id': i16(0x000E),
    }
    g['civs'] = [{
        'leader':      cstr(0x0010 + i*14, 14),
        'nation':      cstr(0x0080 + i*12, 12),
        'nationality': cstr(0x00E0 + i*11, 11),
        'cash':        i16(0x0138 + i*2),
        'research':    i16(0x0148 + i*2),
        'cities':      i16(0x06C8 + i*2),
        'units':       i16(0x06D8 + i*2),
        'score':       i16(0x0748 + i*2),
    } for i in range(8)]

    g['cities'] = []
    for i in range(128):
        o = 0x1508 + i*28
        if b[o+7] == 0:              # ActualSize == 0 -> Slot unbenutzt
            continue
        g['cities'].append({
            'slot': i,
            'owner': b[o+11],
            'x': b[o+4], 'y': b[o+5],
            'size': b[o+7],
            'name_id': b[o+22],
            'food': i16(o+12), 'shields': i16(o+14),
            'improvements': struct.unpack_from('<I', b, o)[0],
        })

    g['city_names'] = [b[0x6970 + i*13 : 0x6970 + (i+1)*13].decode('latin-1').rstrip('\0 ')
                       for i in range(256)]
    return g
```

---

## 13. Praktische Verifikation

Gegen echte, von DOS-Civilization geschriebene Spielstände geprüft
(`~/projekte/civ_orig/CIVIL0-3.SVE`):

* Alle vier Dateien sind **exakt 37.856 Byte** groß — die feste Länge ist bestätigt.
* Der Parser aus Abschnitt 12 liefert plausible Werte, z. B. `CIVIL2.SVE`:
  Zug 3, Jahr −3940, menschliche Civ = 1 („Human" / Romans), Schwierigkeit 1,
  Städte Rome (36,19), Tenochtitlan (59,24), Athens (12,14), Delhi (49,10) —
  Civ-Index und Stadtnamen-Pool passen zusammen
  (`name_id` 0 = Rome ↔ Römer, 160 = Tenochtitlan ↔ Azteken usw.).

Dabei aufgefallene Fallstricke für eigene Parser:

1. **Slot 0 ist der Barbaren-Slot.** In `CIVIL2.SVE` steht dort
   `Attila` / `Barbarians` / `Barbarian`. Barbaren belegen einen vollwertigen
   Civ-Slot mit Einheiten, aber ohne Städte/Gold.
2. **Namen können mit Leerzeichen statt NUL aufgefüllt sein.**
   Der Barbaren-Eintrag lautet byteweise `"Attila       "` (13 Zeichen, kein NUL).
   Also immer erst am NUL abschneiden **und dann** `rstrip()`.
3. **Ein Stadt-Slot gilt genau dann als belegt, wenn `ActualSize != 0`.**
   So testet auch der Originalcode
   ([Segment_2459.cs:210](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/Segment_2459.cs#L210),
   [Segment_2459.cs:414](https://github.com/rajko-horvat/OpenCiv1/blob/a25ae37333f98616070d844158caa812bb8f9401/src/CivGame/Game/Segment_2459.cs#L414)).
   `PlayerID` ist bei unbenutzten Slots **nicht** −1, sondern enthält Müll aus
   früheren Spielen — ungenutzte Slots werden nie genullt.
4. **`city_names[256]` ist der globale Namens-Pool**, kein Pro-Stadt-Feld:
   16 Civs × 16 Namen. `City.NameID` indiziert hier hinein,
   `city_pos_x/y[256]` halten die zugehörige Kartenposition je Pool-Eintrag.
