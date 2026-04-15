# TODO

## Why?

I found many TODOs in the code, so I decided to write them down to get an overview of what still needs to be done.

## TODOs

### SaveDataAdapter.Set.cs

```cs
cities[i].ActualSize = data.ActualSize;
cities[i].VisibleSize = data.VisibleSize;
cities[i].CurrentProduction = data.CurrentProduction;
cities[i].BaseTrade = data.BaseTrade;
...
SetArray(ref cities[i], nameof(SaveData.City.TradingCities), tradingCities);
```

1. `VisibleSize` is now written separately from `ActualSize`.
   - `CityData.VisibleSize` is populated during export in `src/Extensions.cs`.
   - `src/SaveDataAdapter.Set.cs` now writes `data.VisibleSize` into the SVE `VisibleSize` field, so the human player's fog-of-war city size is preserved in binary savegames.

### IGameData.cs

1. No additional persisted `IGameData` properties appear to be missing from the SVE payload.
   - The properties in `api/src/IGameData.cs` all map to fields in `src/IO/SaveData.cs`, sometimes via an adapter representation.
   - Examples: `ActiveCivilizations` maps to the SVE bitfield `SaveData.ActiveCivilizations`, `CivilizationIdentity` maps to `SaveData.CivilizationIdentityFlag`, and `ReplayData` is backed by `ReplayLength` plus the `ReplayData` buffer.

2. Only the technical interface members are not part of the persisted SVE data itself.
   - `Dispose()`
   - `ValidData`
   - `GetBytes()`
   - `ValidMapSize(int width, int height)`

3. The inline TODO on `CivilizationIdentity` is a cleanup/design note, not a missing SVE field.
   - `byte[] CivilizationIdentity { get; set; } // TODO fire-eggs this might as well be bool[]`
   - This is about the public adapter shape in `IGameData`, not about a missing binary save slot.

### SaveDataAdapter.cs

1. Bitfield compatibility with original CivDOS still needs verification.
   - The adapter currently packs and unpacks three SVE-backed bitfields manually:
   - `ActiveCivilizations`
   - `CivilizationIdentity`
   - `GameOptions`
   - All three still carry the same open comment: `TODO fire-eggs: is bit order compatible with CivDOS?`

### Game.LoadSave.cs

1. Unit restore still assumes an 8-player shaped SVE array.
   - The loop in `src/Game.LoadSave.cs` still has the open note `TODO fire-eggs: wrong when playing with fewer than 7?`.
   - This likely needs a dedicated check whether the binary unit arrays and active-civilization handling behave correctly for smaller player counts.

### Extensions.cs

1. Settler-specific `MovesSkip` state still appears unsaved in the SVE conversion path.
   - `src/Extensions.cs` still contains `TODO need to save (Settlers.)MovesSkip value to savefile` in the `UnitData` export path.
   - That suggests a remaining gap between in-memory unit state and the legacy binary save representation.

### CityData / BaseTrade

1. `BaseTrade` is now fully roundtripped through the legacy SVE save/load flow. [RESOLVED]
   - Background: original CIV1 DOS stores a cached `BaseTrade` per city and uses it in the
     trade-route yield formula `(BaseTrade_A + BaseTrade_B + 4) / 8`. CivOne itself recalculates
     trade dynamically and never reads this value at runtime.
   - `src/Extensions.cs` now sets `BaseTrade = city.TradeTotal` so exported SVE files are
     compatible with original CIV1 DOS.
   - `src/SaveDataAdapter.Get.cs` now reads `city.BaseTrade` back into `CityData.BaseTrade` for
     binary roundtrip fidelity (e.g. when re-exporting a previously imported SVE).
   - `src/SaveDataAdapter.Set.cs` was already writing the field correctly.
