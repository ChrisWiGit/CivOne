using System;
using System.Drawing;
using CivOne.Buildings;
using CivOne.Enums;
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
		byte Owner { get; set; }
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


/*
	public byte Status
		{
			get => _status;
		}

		public void SetupStatus(byte status)
		{
			_status = status;

			// recalculate these specific flags, because older versions may not have set them
			SetupCoastalFlag();
			SetupHydroFlag();
		}

		public bool IsRiot
		{
			get => bitFlagExtensions.HasFlag(_status, CityStatus.RIOT);
			set => SetStatusFlag(CityStatus.RIOT, value);
		}

		public bool IsCoastal => bitFlagExtensions.HasFlag(_status, CityStatus.COASTAL);

		public bool CelebrationCancelled
		{
			get => bitFlagExtensions.HasFlag(_status, CityStatus.CELEBRATION_CANCELLED);
			set => SetStatusFlag(CityStatus.CELEBRATION_CANCELLED, value);
		}

		public bool HydroAvailable	{
			get => bitFlagExtensions.HasFlag(_status, CityStatus.HYDRO_AVAILABLE);
		}

		public bool AutoBuild
		{
			get => bitFlagExtensions.HasFlag(_status, CityStatus.AUTO_BUILD);
			set => SetStatusFlag(CityStatus.AUTO_BUILD, value);
		}

		public bool TechStolen
		{
			get => bitFlagExtensions.HasFlag(_status, CityStatus.TECH_STOLEN);
			set => SetStatusFlag(CityStatus.TECH_STOLEN, value);
		}

		public bool CelebrationOrRapture
		{
			get => bitFlagExtensions.HasFlag(_status, CityStatus.CELEBRATION_RAPTURE);
			set => SetStatusFlag(CityStatus.CELEBRATION_RAPTURE, value);
		}
		
		/// <summary>
		/// Was a building sold in this turn?
		/// </summary>
		public bool BuildingSold
		{
			get => bitFlagExtensions.HasFlag(_status, CityStatus.IMPROVEMENT_SOLD);
			set => SetStatusFlag(CityStatus.IMPROVEMENT_SOLD, value);
		}
*/