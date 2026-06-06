using CivOne.Buildings;
using CivOne.Wonders;
using System.Drawing;
using CivOne.Tiles;
using CivOne.Units;
using System;
using System.Linq;
using System.Collections.Generic;
using CivOne.Enums;
using CivOne.Persistence.Model;
using CivOne.Graphics;
using CivOne.Persistence.Game;

namespace CivOne.UnitTests
{
	public class MockedICity : ICity
	{
		private BitFlagExtensions _bits = new();
		private MockedCityTile _cityTile;
		public MockedICity(byte id = 1)
		{
			_cityTile = new MockedCityTile();
			Tile = _cityTile.Tile;
			Id = Guid.Parse($"00000000-0000-0000-0000-00000000000{id}");
			Location = new Point(2, 2);
			Size = 5;
			Luxuries = 4;
			EntertainerLuxuries = 0;
			CityOwnerPlayerIndex = 1;
			Name = "TestCity";
			ResourceTiles = [.. _cityTile.Tiles.Cast<ITile>()];
			Specialists = [Citizen.Entertainer, Citizen.Scientist, Citizen.Taxman];
			Shields = 1;
			Food = 2;
			ContinentId = 3;
			Player = null;
			Entertainers = 4;
			Scientists = 5;
			Taxmen = 6;
			CurrentProduction = new MockedProduction();
			Buildings = [new MockedIBuilding()];
			Wonders = [new MockedIWonder()];
			
			Status = 0;
			Status = _bits.SetFlag(Status, City.CityStatus.AutoBuild);
			Status = _bits.SetFlag(Status, City.CityStatus.Riot);

			WasInDisorder = false;
			TradingCities = [];
			VisibleSizes = [
				0, 0, 0, 0,
				0, 0, 0, 0,
				0, 0, 0, 0,
				0, 0, 0, 0
			];
			IsRiot = false;
			IsCoastal = false;
			CelebrationCancelled = false;
			HydroAvailable = true;
			AutoBuild = false;
			TechStolen = true;
			CelebrationOrRapture = false;
			BuildingSold = false;
		}
		public Guid Id { get; set; }

		public Point Location { get; set; }

		public byte Size { get; set; }

		public short Luxuries { get; set; }

		public int EntertainerLuxuries { get; set; }

		public byte CityOwnerPlayerIndex { get; set; }

		public string Name { get; set; }

		public ITile[] ResourceTiles { get; set; }

		public Citizen[] Specialists { get; set; }

		public int Shields { get; set; }

		public int Food { get; set; }

		public int ContinentId { get; set; }


		public IPlayer Player { get; set; }

		public IPlayer PlayerIntf => Player;

		public int Entertainers { get; set; }

		public int Scientists { get; set; }

		public int Taxmen { get; set; }

		public IProduction CurrentProduction { get; set; }

		public IBuilding[] Buildings { get; set; }

		public IWonder[] Wonders { get; set; }

		public byte Status { get; set; }

		public bool WasInDisorder { get; set; }

		public ICity[] TradingCities { get; set; }

		public uint[] VisibleSizes { get; set; }

		public ITile Tile { get; set; }

		public bool IsRiot { get; set; }
		public bool IsCoastal { get; set; }
		public bool CelebrationCancelled { get; set; }
		public bool HydroAvailable { get; set; }
		public bool AutoBuild { get; set; }
		public bool TechStolen { get; set; }
		public bool CelebrationOrRapture { get; set; }
		public bool BuildingSold { get; set; }

		public bool HasBuilding<T>() where T : IBuilding
		{
			throw new NotImplementedException();
		}

		public bool HasWonder<T>() where T : IWonder
		{
			throw new NotImplementedException();
		}

		public void NewTurn()
		{
			throw new NotImplementedException();
		}
	}

}
