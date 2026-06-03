using System.Collections.Generic;
using System;
using CivOne.Advances;
using CivOne.Governments;
using CivOne.Enums;

namespace CivOne.Persistence.Game
{
	/// <summary>
	/// This interface extends IPlayer with setters for all properties that need to be restored from a DTO.
	/// It is used as the return type of IPlayerFactory.Create() to allow the PlayerDtoMapper to set all necessary properties after creation,
	/// without requiring the actual Player class to have public setters for all properties. This
	/// allows for better encapsulation of the Player class while still enabling full restoration of player state from a DTO.
	/// The properties in this interface should match the properties in PlayerDto that are needed to restore the player's state.
	/// </summary>
	public interface IPlayerRestorable : IPlayer
	{
		new string TribeName { get; set; }
		new Guid PlayerGuid { get; set; }
		new string TribeNamePlural { get; set; }
		new bool[,] Explored { get; set; }
		new bool[,] Visible { get; set; }
		new List<byte> Advances { get; set; }
		new List<byte> Embassies { get; set; }
		new ushort[] Diplomacy { get; set; }
		new short Anarchy { get; set; }
		new short Gold { get; set; }
		new IAdvance? CurrentResearch { get; set; }
		new int CityNamesSkipped { get; set; }
		new ushort FutureTechCount { get; set; }
		new ushort HumanContactTurn { get; set; }
		new short StartX { get; set; }
		new (short X, short Y)[] MapPositions { get; set; }
		new string[] MapPositionNames { get; set; }
		(short X, short Y) LastMapPosition { get; set; }
		new int MapZoomBasisPoints { get; set; }
		new ushort[] UnitsLost { get; set; }
		new ushort[] UnitsDestroyedBy { get; set; }
		new ushort EpicRanking { get; set; }
		new ushort MilitaryPower { get; set; }
		new ushort CivilizationScore { get; set; }
		new IGovernment Government { get; set; }
		new int LuxuriesRate { get; set; }
		new int TaxesRate { get; set; }
		new int ScienceRate { get; set; }
		new short Science { get; set; }
		new PalaceData Palace { get; set; }
		SpaceShipComponentType[,] SpaceShipGrid { get; set; }
		ushort SpaceShipPopulation { get; set; }
		short SpaceShipLaunchYear { get; set; }
		new List<ICity> Cities { get; set; }
	}
}
