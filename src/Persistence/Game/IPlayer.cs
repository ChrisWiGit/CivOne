using System.Collections.Generic;
using System;
using CivOne.Advances;
using CivOne.Civilizations;
using CivOne.Governments;
using CivOne.Wonders;

namespace CivOne.Persistence.Game
{
	public interface IPlayerEffects
	{
		bool HasWonderEffect<T>() where T : IWonder, new();
		bool HasAdvance<T>() where T : IAdvance;
	}

	public interface IPlayer : IPlayerEffects
	{
		ICivilization Civilization { get; }
		Guid PlayerGuid { get; }
		string TribeName { get; }
		string TribeNamePlural { get; }
		bool[,] Explored { get; }
		bool[,] Visible { get; }
		List<byte> Advances { get; }
		List<byte> Embassies { get; }
		ushort[] Diplomacy { get; }
		short Anarchy { get; }
		short Gold { get; }
		IAdvance CurrentResearch { get; }
		int CityNamesSkipped { get; }
		ushort FutureTechCount { get; }
		ushort HumanContactTurn { get; }
		short StartX { get; }
		(short X, short Y)[] MapPositions { get; }
		string[] MapPositionNames { get; }
		(short X, short Y) LastMapPosition { get; }
		ushort[] UnitsLost { get; }
		ushort[] UnitsDestroyedBy { get; }
		ushort EpicRanking { get; }
		ushort MilitaryPower { get; }
		ushort CivilizationScore { get; }
		IGovernment Government { get; }
		bool RepublicDemocratic { get; }
		bool AnarchyDespotism { get; }
		bool MonarchyCommunist { get; }

		int LuxuriesRate { get; }
		int TaxesRate { get; }
		int ScienceRate { get; }
		short Science { get; }
		PalaceData Palace { get; }
		List<ICity> Cities { get; }
	}
}