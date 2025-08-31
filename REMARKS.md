# Remarks

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

## Game Loading Refactoring

The game loading process has been refactored to improve maintainability and flexibility. The key changes include:

Now the services are used to load the map and game data.
A service provides a specific functionality and can be replaced with another implementation if needed.

The GameLoaderService is responsible for loading the game data from a save file.
It returns an IGame interface, which is implemented by the Game class.
By using IFileGameLoader, IStreamGameLoader, IGameData, IStreamToSaveDataService and IMapLoader interfaces, the GameLoaderService can work with different file formats and data sources.

## General remarks about services and providers

### Services

Services provide specific functionalities and can be replaced with different implementations if needed.
Every service must have an interface that defines its functionality.
It is named IMyServiceNameService.
Its implementation is named MyServiceNameServiceImpl.

All Services for CivOne reside in the Services namespace: CivOne.Services and CivOne.Services.Impl.
The files resides in the Services folder.
The files can be further organized in subfolders if needed.

### Providers

Providers are used to provide services.
They are used to get a service implementation.
They are usually static classes with static methods.

They are named MyServiceProvider.
The files resides in the Services folder.

A single provider class can provide multiple services if they are related.
This supports the principle of interface segregation (SOLID).

### Usage

Services are received via the provider within a class that needs the service.
The provider can use settings or other criteria to decide which implementation to provide.

The service is usually stored in a protected property for use within the class.

``csharp
protected IMyServiceNameService MyService => MyServiceProvider.GetMyService();
``
