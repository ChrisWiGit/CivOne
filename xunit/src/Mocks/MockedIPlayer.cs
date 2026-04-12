using System.Linq;
using System.Collections.Generic;
using CivOne.Wonders;
using System;
using CivOne.Governments;
using CivOne.Advances;
using CivOne.Persistence.Model;
using CivOne.Civilizations;
using CivOne.Enums;

namespace CivOne.UnitTests
{
	public class MockedIPlayer : IPlayerRestorable
	{
		public MockedIPlayer()
		{
			Civilization = new MockedICivilization(1);
			PlayerGuid = Guid.NewGuid();
			TribeName = "Mock Tribe";
			TribeNamePlural = "Mock Tribes";
			Explored = new bool[10, 10];
			Visible = new bool[10, 10];
			Advances = [];
			Embassies = [];
			Diplomacy = new ushort[8];
			Anarchy = 0;
			Gold = 100;
			CurrentResearch = null;
			CityNamesSkipped = 0;
			FutureTechCount = 0;
			HumanContactTurn = 0;
			StartX = 0;
			UnitsLost = new ushort[28];
			UnitsDestroyedBy = new ushort[8];
			EpicRanking = 0;
			MilitaryPower = 0;
			CivilizationScore = 0;
			Government = null;
			RepublicDemocratic = false;
			AnarchyDespotism = false;
			MonarchyCommunist = false;
			LuxuriesRate = 0;
			TaxesRate = 0;
			ScienceRate = 0;
			Science = 0;
			Palace = null;
			Cities = [];
		}
		public ICivilization Civilization { get; set; }

		public Guid PlayerGuid { get; set; }

		public string TribeName { get; set; }

		public string TribeNamePlural { get; set; }

		public bool[,] Explored { get; set; }

		public bool[,] Visible { get; set; }

		public List<byte> Advances { get; set; }

		public List<byte> Embassies { get; set; }

		public ushort[] Diplomacy { get; set; }

		public short Anarchy { get; set; }

		public short Gold { get; set; }

		public IAdvance CurrentResearch { get; set; }

		public int CityNamesSkipped { get; set; }

		public ushort FutureTechCount { get; set; }

		public ushort HumanContactTurn { get; set; }

		public short StartX { get; set; }

		public ushort[] UnitsLost { get; set; }

		public ushort[] UnitsDestroyedBy { get; set; }

		public ushort EpicRanking { get; set; }

		public ushort MilitaryPower { get; set; }

		public ushort CivilizationScore { get; set; }

		public IGovernment Government { get; set; }

		public bool RepublicDemocratic { get; set; }

		public bool AnarchyDespotism { get; set; }

		public bool MonarchyCommunist { get; set; }

		public int LuxuriesRate { get; set; }

		public int TaxesRate { get; set; }

		public int ScienceRate { get; set; }

		public short Science { get; set; }

		public PalaceData Palace { get; set; }

		public List<ICity> Cities { get; set; }
		public SpaceShipComponentType[,] SpaceShipGrid { get; set; }
		public ushort SpaceShipPopulation { get; set; }
		public short SpaceShipLaunchYear { get; set; }

		public bool HasAdvance<T>() where T : IAdvance
		{
			return false;
		}

		public bool HasWonderEffect<T>() where T : IWonder, new()
		{
			return false;
		}
	}
}
