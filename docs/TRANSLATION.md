# Translation Strategy

## Overview

CivOne uses an opt-in, explicit translation approach. UI text is translated at the call site, not
transparently inside rendering primitives like `DrawText()`. This keeps rendering code pure and
avoids accidental translation of technical strings (filenames, hotkeys, save tokens, etc.).

---

## Core Infrastructure

`BaseInstance` already exposes two helpers available to all screens:

```csharp
protected string Translate(string key)
protected string TranslateFormatted(string key, params object[] args)
```

> At this time, these are simple pass-throughs to `Translation.Translate()` and `Translation.TranslateFormatted()`. In the future, they could be extended to support context-aware translation, caching, or other features without changing the call sites.

Both delegate to `ITranslationService` (created via `TranslationServiceFactory.GetCurrent()`).

---

## Rules: What to Translate

| Text type                                                      | Translate?  | How                               |
| -------------------------------------------------------------- | ----------- | --------------------------------- |
| Static UI labels (`"Population:"`, `"Treasury:"`)              | ✅ Yes      | `Translate("key")`                |
| Report / screen titles (`"SCIENCE REPORT"`)                    | ✅ Yes      | `Translate("key")`                |
| Mixed static + runtime values (`"Researching {name}"`)         | ✅ Yes      | `TranslateFormatted("key", name)` |
| Player / city / leader **names**                               | ❌ No       | Pass through unchanged            |
| Save-game filenames, drive letters                             | ❌ No       | Pass through unchanged            |
| Resource keys used as image look-ups (`Resources["SETTLERS"]`) | ❌ No       | Not user-visible                  |
| Debug screen labels                                            | ⚠️ Optional | Low priority                      |

---

## How to Translate a DrawText Call

### Static text

Before:

```csharp
this.DrawText("Population:", 0, 15, x, y);
```

After:

```csharp
this.DrawText(Translate("Population"), 0, 15, x, y);
```

### Dynamic text with placeholders

Before:

```csharp
this.DrawText($"Population: {population} Happy:{happy}% Content:{content}% Unhappy:{unhappy}%", 0, 15, x, y);
```

After:

```csharp
this.DrawText(TranslateFormatted("Population: {0} Happy:{1}% Content:{2}% Unhappy:{3}%", population, happy, content, unhappy), 0, 15, x, y);
```

Translation file entry:

```yaml
POPULATION: {0} HAPPY:{1}% CONTENT:{2}% UNHAPPY:{3}%: "Population: {0} Happy:{1}% Content:{2}% Unhappy:{3}%"
```

---

## Key Naming Convention

Use dot-separated hierarchical keys:

```text
<screen>.<section>.<element>
```

Examples:

| Key                              | English value                      |
| -------------------------------- | ---------------------------------- |
| `civilopedia.title`              | `ENCYCLOPEDIA of CIVILIZATION`     |
| `civilopedia.discovered`         | `(Discovered)`                     |
| `report.science.title`           | `SCIENCE REPORT`                   |
| `report.science.researching`     | `Researching {0}`                  |
| `report.trade.cityTrade`         | `City Trade`                       |
| `report.trade.totalIncome`       | `Total Income: {0}$`               |
| `report.military.title`          | `MILITARY STATUS`                  |
| `report.intelligence.title`      | `INTELLIGENCE REPORT`              |
| `report.intelligence.noEmbassy`  | `No embassy established.`          |
| `report.topCities.title`         | `The Top Five Cities in the World` |
| `report.worldWonders.title`      | `The Wonders of the World`         |
| `report.civilizationScore.title` | `CIVILIZATION SCORE`               |
| `conquest.destroyCivilization`   | `{0} civilization!`                |
| `conquest.worldHails`            | `The entire world hails`           |
| `conquest.conqueror`             | `{0} the CONQUEROR!`               |
| `search.cityNotFound`            | `City not found.`                  |
| `loadGame.whichDrive`            | `Which drive contains your`        |
| `loadGame.savedGameFiles`        | `saved game files?`                |
| `saveGame.whichDrive`            | `Which drive contains your`        |
| `saveGame.saveDisk`              | `Save Game disk?`                  |
| `saveGame.inProgress`            | `... save in progress.`            |
| `debug.changeHumanPlayer`        | `Change Human Player...`           |
| `saveGame.pressKey`              | `Press key to continue.`           |
| `debug.changHumanPlayer`         | `Change Human Player...`           |
| `debug.meetWithKing`             | `Meet With King`                   |
| `debug.setCitySize`              | `Set City Size...`                 |
| `debug.setGameYear`              | `Set Game Year...`                 |
| `debug.setPlayerAdvances`        | `Set Player Advances...`           |
| `debug.setPlayerGold`            | `Set Player Gold...`               |
| `debug.setPlayerScience`         | `Set Player Science...`            |
| `debug.spawnUnit`                | `Spawn Unit...`                    |

---

## Priority Order

1. **Report screens** – most visible during gameplay.
2. **Main gameplay screens** – `CityView`, `GamePlay`, `King`, `Nuke`.
3. **Dialogs** – `BaseDialog`, `Revolution`, `PopupMessage`.
4. **Load/Save screens** – `LoadGame`, `SaveGame`.
5. **Debug screens** – lowest priority.

---

## Do NOT Change

- `DrawText()` signature — keep rendering pure.
- Player/city/leader name rendering — these are data, not translations.
- Resource dictionary keys (`Resources["..."]`) — internal identifiers.
