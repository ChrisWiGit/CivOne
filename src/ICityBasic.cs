using System;
using System.Drawing;
using CivOne.Buildings;
using CivOne.Enums;
using CivOne.Persistence.Game;
using CivOne.Persistence.Model;
using CivOne.Tiles;
using CivOne.Wonders;

namespace CivOne
{
	public interface ICityTile
	{
		ITile Tile { get; }
	}

	public interface ICityBasic : ICityTile
	{
		Guid Id { get; set; }
		Point Location { get; }
		byte Size { get; }
		short Luxuries { get; }
		public int EntertainerLuxuries { get; }
		byte CityOwnerPlayerIndex { get; set; }
		string Name { get; }

		ITile[] ResourceTiles { get; }
		Citizen[] Specialists { get; }

		int Shields { get; }
		int Food { get; }

		int ContinentId { get; }

		IPlayer PlayerIntf { get; }

		int Entertainers { get; }
		int Scientists { get; }
		int Taxmen { get; }

		IProduction CurrentProduction { get; }

		IBuilding[] Buildings { get; }

		IWonder[] Wonders { get; }

		byte Status { get; }
		bool WasInDisorder { get; }
		ICity[] TradingCities { get; }

		uint[] VisibleSizes { get; }
	}

	public interface ICityStatus
	{
		bool IsRiot { get; set; }
		bool IsCoastal { get; set; }
		bool CelebrationCancelled { get; set; }
		bool HydroAvailable { get; set; }
		bool AutoBuild { get; set; }
		bool TechStolen { get; set; }
		bool CelebrationOrRapture { get; set; }
		bool BuildingSold { get; set; }
	}
}