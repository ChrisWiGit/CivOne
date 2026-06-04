using System;
using System.Collections.Generic;
using System.Drawing;

namespace CivOne.Agents
{
	/// <summary>
	/// Exposes readonly civilization state returned by <see cref="ITurnContext"/>.
	/// </summary>
	public interface ICivilizationView
	{
		/// <summary>
		/// Gets the civilization identifier.
		/// </summary>
		Guid Id { get; }

		/// <summary>
		/// Gets the civilization leader name.
		/// </summary>
		string LeaderName { get; }

		/// <summary>
		/// Gets the civilization singular name.
		/// </summary>
		string CivilizationName { get; }

		/// <summary>
		/// Gets the civilization plural name.
		/// </summary>
		string CivilizationNamePlural { get; }

		/// <summary>
		/// Gets the current gold amount.
		/// </summary>
		short Gold { get; }

		/// <summary>
		/// Gets the current luxury rate.
		/// </summary>
		int LuxuriesRate { get; }

		/// <summary>
		/// Gets the current tax rate.
		/// </summary>
		int TaxesRate { get; }

		/// <summary>
		/// Gets the current science rate.
		/// </summary>
		int ScienceRate { get; }
	}

	/// <summary>
	/// Exposes readonly unit state returned by <see cref="ITurnContext.OwnUnits"/>.
	/// </summary>
	public interface IUnitView
	{
		/// <summary>
		/// Gets the unit identifier.
		/// </summary>
		Guid Id { get; }

		/// <summary>
		/// Gets the owner identifier.
		/// </summary>
		Guid OwnerId { get; }

		/// <summary>
		/// Gets the internal unit name.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Gets the current X coordinate.
		/// </summary>
		int X { get; }

		/// <summary>
		/// Gets the current Y coordinate.
		/// </summary>
		int Y { get; }

		/// <summary>
		/// Gets the current goto destination.
		/// </summary>
		Point Goto { get; }

		/// <summary>
		/// Gets the remaining full moves.
		/// </summary>
		byte MovesLeft { get; }

		/// <summary>
		/// Gets the remaining partial moves.
		/// </summary>
		byte PartMoves { get; }

		/// <summary>
		/// Gets a value indicating whether the unit has action flags set.
		/// </summary>
		bool HasAction { get; }

		/// <summary>
		/// Gets a value indicating whether the unit can still move.
		/// </summary>
		bool HasMovesLeft { get; }

		/// <summary>
		/// Gets a value indicating whether the unit is sentry.
		/// </summary>
		bool Sentry { get; }

		/// <summary>
		/// Gets a value indicating whether the unit is fortify-active.
		/// </summary>
		bool FortifyActive { get; }

		/// <summary>
		/// Gets a value indicating whether the unit is fortified.
		/// </summary>
		bool Fortify { get; }

		/// <summary>
		/// Gets a value indicating whether the unit is veteran.
		/// </summary>
		bool Veteran { get; }
	}

	/// <summary>
	/// Exposes readonly city state returned by <see cref="ITurnContext.OwnCities"/>.
	/// </summary>
	public interface ICityView
	{
		/// <summary>
		/// Gets the city identifier.
		/// </summary>
		Guid Id { get; }

		/// <summary>
		/// Gets the owner identifier.
		/// </summary>
		Guid OwnerId { get; }

		/// <summary>
		/// Gets the city name.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Gets the city X coordinate.
		/// </summary>
		int X { get; }

		/// <summary>
		/// Gets the city Y coordinate.
		/// </summary>
		int Y { get; }

		/// <summary>
		/// Gets the city size.
		/// </summary>
		byte Size { get; }

		/// <summary>
		/// Gets the stored food amount.
		/// </summary>
		int Food { get; }

		/// <summary>
		/// Gets the stored shield amount.
		/// </summary>
		int Shields { get; }

		/// <summary>
		/// Gets the current production internal name if any.
		/// </summary>
		string? CurrentProductionName { get; }

		/// <summary>
		/// Gets the currently available production names.
		/// </summary>
		IReadOnlyList<string> AvailableProductionNames { get; }

		/// <summary>
		/// Gets the owned building names.
		/// </summary>
		IReadOnlyList<string> BuildingNames { get; }

		/// <summary>
		/// Gets the owned wonder names.
		/// </summary>
		IReadOnlyList<string> WonderNames { get; }

		/// <summary>
		/// Gets the last known visible city sizes per player.
		/// </summary>
		IReadOnlyList<uint> VisibleSizes { get; }
	}

	/// <summary>
	/// Exposes readonly tile state through <see cref="IMapView.GetTile(int, int)"/>.
	/// </summary>
	public interface ITileView
	{
		/// <summary>
		/// Gets the tile X coordinate.
		/// </summary>
		int X { get; }

		/// <summary>
		/// Gets the tile Y coordinate.
		/// </summary>
		int Y { get; }

		/// <summary>
		/// Gets a value indicating whether the tile is currently visible.
		/// </summary>
		bool IsVisible { get; }

		/// <summary>
		/// Gets a value indicating whether the tile has been explored.
		/// </summary>
		bool IsExplored { get; }

		/// <summary>
		/// Gets the internal terrain name.
		/// </summary>
		string TerrainName { get; }

		/// <summary>
		/// Gets a value indicating whether the tile has a road.
		/// </summary>
		bool Road { get; }

		/// <summary>
		/// Gets a value indicating whether the tile has a railroad.
		/// </summary>
		bool Railroad { get; }

		/// <summary>
		/// Gets a value indicating whether the tile has irrigation.
		/// </summary>
		bool Irrigation { get; }

		/// <summary>
		/// Gets a value indicating whether the tile has a mine.
		/// </summary>
		bool Mine { get; }

		/// <summary>
		/// Gets a value indicating whether the tile has pollution.
		/// </summary>
		bool Pollution { get; }

		/// <summary>
		/// Gets the continent identifier.
		/// </summary>
		int ContinentId { get; }

		/// <summary>
		/// Gets the visible city identifier if any.
		/// </summary>
		Guid? CityId { get; }

		/// <summary>
		/// Gets the visible unit identifiers on the tile.
		/// </summary>
		IReadOnlyList<Guid> UnitIds { get; }
	}

	/// <summary>
	/// Exposes readonly map access via <see cref="ITurnContext.Map"/>.
	/// </summary>
	public interface IMapView
	{
		/// <summary>
		/// Gets the map width.
		/// </summary>
		int Width { get; }

		/// <summary>
		/// Gets the map height.
		/// </summary>
		int Height { get; }

		/// <summary>
		/// Gets one tile view for the provided coordinates.
		/// </summary>
		/// <param name="x">The X coordinate.</param>
		/// <param name="y">The Y coordinate.</param>
		/// <returns>The tile view or <c>null</c> if the tile is unknown.</returns>
		ITileView? GetTile(int x, int y);
	}
}