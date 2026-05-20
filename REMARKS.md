# Remarks

## International Font Simulation

The original `FONTS.CV` file shipped with Civilization 1 contains only ASCII characters (space through `~`, i.e. codes 32–126) plus a small set of Western European characters stored in the control-character range (codes 0–31), such as `ü`, `é`, `â`, `ä`, `ö`, `ü`, `ß` and `ç`.

Players who own an English-only `FONTS.CV` — which lacks even those control-character glyphs — cannot display translated text containing non-ASCII letters without a modified font file.

### Solution

`InternationalSimulatedFontSet` extends `Fontset` and synthesises missing glyphs at runtime:

1. It first checks the static mapping table for characters already encoded in the control-character range.
2. If the character is not found there, it decomposes the Unicode code point into its base letter and combining diacritic mark (Unicode NFD), renders the base letter from the font, and draws the accent pixel-by-pixel on top.
3. As a last resort it falls back to the unaccented base letter.

### Mode selection (`FontSetFactory`)

The behaviour is controlled by the **Simulate International Font** setting (**Shift+F1 → Game Options → Language**):

| Setting              | Effect                                                                                                                            |
| -------------------- | --------------------------------------------------------------------------------------------------------------------------------- |
| **Yes**              | Always use the simulating font set                                                                                                |
| **No**               | Always use the plain font set                                                                                                     |
| **Auto** *(default)* | Use the simulating font set only when `FONTS.CV` starts with ASCII space (code 32), which identifies a standard English-only file |

### Relevant classes

| Class                           | File                                            |
| ------------------------------- | ----------------------------------------------- |
| `Fontset`                       | `src/Graphics/FontSet.cs`                       |
| `InternationalSimulatedFontSet` | `src/Graphics/InternationalSimulatedFontSet.cs` |
| `FontSetFactory`                | `src/Graphics/FontSetFactory.cs`                |

## Number of Civilizations

The maximum number of civilizations that can participate in a game is limited to 8. This total includes the player and the Barbarians, which means that up to 6 civilizations can be controlled by the AI in a single game.

Although there are 14 different civilizations available in the game, they are organized into pairs of "buddy civilizations." Only one civilization from each pair can appear in a game at the same time, so certain combinations are not possible. This pairing system is hardcoded throughout the game, making it difficult to modify without significant changes.

To alter or expand this behavior, it would be necessary to move away from the original game’s logic and storage format. Implementing a custom save format and new logic would allow for more flexibility in the number and combination of civilizations.

A particular challenge arises with the game's replay feature. The replay system only records the player numbers (0-7), not the civilization IDs (0-14). As a result, changing the civilization system would also require a redesign of the replay functionality to properly track and display the correct civilizations.

## Fortified Units in Cities

The original SaveGame stores up to two units per city separately in 2 bytes (`SaveData.City.FortifiedUnits`).
These units must be removed from the list of units, otherwise they will be counted twice in the city.

### Why did Sid do it this way?

I suspect this was done to increase the maximum total number of units for a civilization.
Normally, a civilization can only have 128 units, and if all cities have 2 stationed units, that would already be 256 units, so you wouldn't have any units left for attacking or exploring.
Therefore, up to 2 units per city are stored separately.
However, only the values UnitId, Fortified, and Veteran are stored.

### Or

**The structure is not used.**
At least in the original game with a low total number of units, FortifiedUnits was not used.
Maybe later in the game, when the total number of units increases, it is used, but I have not seen it yet.

Currently, we always use them!

### Further

The Fortified status is saved, but why?
If, as we assume, only the fortified units in the city are stored (`FilterUnits()`), then the Fortified status for the units in the city is irrelevant, since they cannot be moved anyway.

### "Architecture"

Currently, in CivOne, the handling of units in cities is done in the following places:

* `Game.LoadSave.cs::Save()` - This is where the SaveGame is loaded and the cities are initialized.
  * Units are saved, but without considering the FortifiedUnits.
* `SaveData.City.FortifiedUnits` - Stores up to 2 units per city in 2 bytes.
* `SaveDataAdapter.Get.cs::GetCities()` - This is where the unit bytes are taken from the SaveGame.
* `SaveDataAdapter.Set.cs::SetCities()` - This is where the unit bytes are written to the SaveGame.
* `Extensions.cs::GetCityData()` - This is where the number of units in a city is determined.
* `Extensions.cs::GetUnitData()` and `FilterUnits()` - This is where the number of units in a city is determined and filtered, by only counting the units that are not fortified in a city and do not have this city as their home.

### City View

Following todo's are to be implemented in the city view:

* Auto Build not implemented
* [x] Hotkey a selects the first unit in the city.
  * Left, Right, Up, Down keys cycle through the units in the city.
  * Space/Enter selects the unit and removes sentry or fortified status.
  * Units cannot be sentried or fortified in the city view.
  * ESC closes unit selection.
* [x] Hotkey s selects city buildings
  * Up, Down keys cycle through the buildings in the city.
  * Space/Enter selects the building to be sold.
  * ESC closes building selection and city view.
* [x] Hotkey p selects city tile view
  * Up, Down, Left, Right keys cycle through the city tiles.
  * Space/Enter selects the tile to be removed or worked.
  * ESC closes tile selection.
* [x] Hotkey 1-9 cycles through the specialists in the city.
  * Hotkey changes the specialist entertainer to be changed to tax and science and back to entertainer.
  * What about > 9?
* [x] Hotkey shift+a sets production to auto build.
* [x] On CityView Info Tile (see manual page 75)
  * Bottom row contains tiles of pollution indicators
  * Traderoutes to city with trade values
    * City name: 3 (up/down arrow symbol)
* [x] Building View
  * More than 14 buildings shows More Button

## Trading Cities

See here [Caravan (Civ1)](https://civilization.fandom.com/wiki/Caravan_(Civ1))

ChatGpt revised:

### Caravan Actions (short version for code)

* **Wonder help:**

  * If caravan enters a domestic city building a wonder → add **+50 shields**. Caravan is consumed.

* **Trade route creation:**

  * **Foreign city:** always create a trade route.
  * **Domestic city ≥10 squares away:** player chooses to create route or move on.

* **Initial windfall (cash + research):**

```text
base = (distance + 10) * (trade1 + trade2) / 24
```

* Halve (`*0.5`) if:
  * cities on same continent
  * OR same civilization
* Reduce to 2/3 (`*2/3`) if:

  * player has railroads
  * OR player has flight
* If all 4 conditions true → windfall = base \* 1/9.

### Permanent trade bonus (home city trade arrows)

```text
bonus = (trade1 + trade2 + 4) / 8
```

* Halve (`*0.5`) if same civilization.
* Distance and other factors do **not** matter.

### Limits & rules

* City keeps only **3 most profitable** routes; others give only initial windfall.
* Trade is **one-way**; target city must send its own caravan for reverse route.
* Each caravan counts toward **127-unit limit**.
* Caravan disappears after building a wonder or creating a trade route.

### Trading Routes todos

* In [City View](./src/Screens/CityManagerPanels/CityInfoUnits.cs) shall show trading cities with trade values.

## Hall of Fame

### Where is it stored?

Hall of Fame data is persisted in:

* `HallOfFame.yaml` inside `Runtime.StorageDirectory`

For the SDL runtime on Windows, `Runtime.StorageDirectory` is:

* `%LOCALAPPDATA%/CivOne`

So the effective file path is usually:

On Windows this is `%LOCALAPPDATA%\CivOne\HallOfFame.yaml`.
On Linux and macOS this is `~/.local/share/CivOne/HallOfFame.yaml`.

### When is it read or written?

* **Read only (view mode):** opening Hall of Fame from credits/debug uses `ViewEntries(...)`.
* **Write (add score):** at end game flow (`conquest`, `defeat`, `alpha centauri`, `retire`) `AddScore()` composes current human entry and stores it.
* **Write (clear):** pressing **C** on the post-game Hall of Fame screen calls `Clear()`.

If the file is missing, view mode shows placeholders and does **not** create a file.

### Persistence rules

After loading existing entries and adding the newest one, entries are normalized:

* Sort by `Score` descending
* Tie-break by `CreatedAtUtc` descending (newer first)
* Keep only top `5` entries

### File format

The file uses YAML with PascalCase properties.

Top-level model:

* `Version` (currently default `1`)
* `Entries` (array of hall of fame entries)

Entry model fields:

* `LeaderName`: player-entered leader name
* `LeaderTitle`: difficulty title (`Chief`, `Lord`, `Prince`, `King`, `Emperor`, `Deity`)
* `CivilizationNamePlural`: tribe plural name (example: `Romans`)
* `YearLabel`: formatted game year label (example: `1850 AD`)
* `Population`: total population integer
* `Score`: final civilization score integer
* `RatingRankLabel`: historical personality label from rating calculation (top-leader table)
* `RatingPercent`: civilization rating percent integer
* `CreatedAtUtc`: UTC timestamp used for tie-breaking and chronology

### Example `HallOfFame.yaml`

```yaml
Version: 1
Entries:
  - LeaderName: "Marcus"
    LeaderTitle: "King"
    CivilizationNamePlural: "Romans"
    YearLabel: "1850 AD"
    Population: 1234567
    Score: 1876
    RatingRankLabel: "Augustus Caesar"
    RatingPercent: 74
    CreatedAtUtc: 2026-05-20T12:34:56.0000000+00:00
```

### Clear behavior detail

Clear does not always mean empty file:

* If a current human game context exists, clear keeps exactly **one** entry: the current human composed score.
* If no active game context exists (for example credits/debug without `Game.Instance`), clear falls back to empty entries.

## DrawText Symbols

| Symbol | Meaning      |
| ------ | ------------ |
| #      | Stick Figure |
| $      | Coin         |
| ^      | Check Mark   |
| {      | Wheat Stalk  |
| }      | Trade Arrows |
| \      | Diamond      |
| \|     | Shield       |
| ~      | Light Bulb   |
| _      | Sun          |

## DrawButton / Font IDs

`DrawButton` uses the provided `fontId` directly:

```csharp
DrawButton(string text, byte fontId, byte colour, byte colourDark, int x, int y, int width, int height)
```

There is also a shorthand overload without `fontId`, which defaults to font `1`:

```csharp
DrawButton(string text, byte colour, byte colourDark, int x, int y, int width)
// internally uses fontId = 1 and height = Resources.GetFontHeight(1) + 3
```

### Known font IDs used in code

The exact glyph shapes come from `FONTS.CV` (runtime data file, not in this repository), so visual style below is based on where each font is used in-game.

| Font ID | Typical usage in CivOne                                     | Likely visual style                |
| ------: | ----------------------------------------------------------- | ---------------------------------- |
|       0 | Standard UI text, menus, dialogs, reports                   | Default readable UI font (regular) |
|       1 | Compact UI text, many buttons/panels, small labels          | Smaller/compact UI font            |
|       2 | Newspaper headline accents (`_shout`)                       | Decorative headline style          |
|       3 | Demo / newspaper emphasis text                              | Bold or stylized display font      |
|       4 | Newspaper title (`_name`), credits text settings            | Title-like decorative font         |
|       5 | Big event/title text (city banners, game over, discovery)   | Large ornamental title font        |
|       6 | Civilopedia/body info text, intro/new game descriptive text | Thin/compact info font             |
|       8 | Unit letter overlay on sprites                              | Very compact symbol/letter font    |

### Practical button guidance

* If you want vanilla-looking UI buttons, use the shorthand overload (font `1`).
* For compact buttons, keep `height` close to `Resources.GetFontHeight(fontId) + 3`.
* If text appears vertically off-center, adjust only `height` first (the text is drawn at `y + 2`).
* If a chosen font is unavailable/out-of-range, rendering falls back to `DefaultFont`.

## Colors

| Code | Color Name   | Description             |
| ---- | ------------ | ----------------------- |
| 1    | Blue         | Standard blue           |
| 2    | Green        | Standard green          |
| 3    | Light Grey   | Grey for disabled items |
| 4    | Dark Red     | Dark red                |
| 5    | Black        | Standard black          |
| 6    | Brown        | Standard brown          |
| 7    | Light Brown  | Light brown             |
| 8    | Dark Brown   | Dark brown              |
| 9    | None         | No color                |
| 10   | Light Green  | Light green             |
| 11   | Light Blue   | Light blue              |
| 12   | Light Red    | Light red               |
| 13   | Pink         | Pink                    |
| 14   | Light Yellow | Light yellow            |
| 15   | White        | White                   |
| 16   | White        | White                   |
