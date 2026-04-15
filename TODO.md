# TODO

## Why?

I found many TODOs in the code, so I decided to write them down to get an overview of what still needs to be done.

## TODOs

### SaveDataAdapter.Set.cs

```cs
cities[i].ActualSize = data.ActualSize;
cities[i].VisibleSize = data.ActualSize;
cities[i].CurrentProduction = data.CurrentProduction;
cities[i].BaseTrade = data.BaseTrade;
...
SetArray(ref cities[i], nameof(SaveData.City.TradingCities), tradingCities);
```

1. `VisibleSize` is still not persisted correctly when writing SVE data.
   - `CityData` already distinguishes between `ActualSize` and `VisibleSize`, and loading uses `VisibleSizeToHumanPlayer`.
   - However, `src/Extensions.cs` currently only fills `ActualSize` when exporting `CityData` from the in-memory `City`.
   - Because of that, `src/SaveDataAdapter.Set.cs` still writes `data.ActualSize` into `VisibleSize`, so fog-of-war city size information for the human player is effectively lost during save.

2. Trade-related notes in this section are outdated.
   - `BaseTrade` is now written from `data.BaseTrade`.
   - Trading city references are copied via `tradingCities` instead of hardcoded `0xFF` placeholders.
