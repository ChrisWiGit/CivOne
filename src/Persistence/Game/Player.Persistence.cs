// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Collections.Generic;
using System.Linq;
using CivOne.Advances;
using CivOne.Enums;
using CivOne.Governments;
using CivOne.Persistence.Game;
using CivOne.Persistence.Model;
using CivOne.Services.SpaceShip;

namespace CivOne
{
	/// <summary>
	/// Partial class that makes <see cref="Player"/> implement <see cref="IPlayerRestorable"/>.
	/// All setters use explicit interface implementations so they do not pollute the
	/// public Player API, and several bypass normal side-effect logic to allow clean
	/// restoration of saved state (e.g. rate setters skip the rate-compensation logic).
	/// </summary>
	public partial class Player : IPlayerRestorable
	{
		/// <summary>
		/// Temporary city list populated during YAML load, before the <c>Game</c>
		/// instance exists. The <c>Game(GameState)</c> constructor must drain this
		/// list into its own <c>_cities</c> collection.
		/// </summary>
		internal List<ICity> RestoredCities { get; private set; } = [];

		string IPlayerRestorable.TribeName
		{
			get => _tribeName;
			set => _tribeName = value;
		}

		Guid IPlayerRestorable.PlayerGuid
		{
			get => _playerGuid;
			set => _playerGuid = value;
		}

		string IPlayerRestorable.TribeNamePlural
		{
			get => _tribeNamePlural;
			set => _tribeNamePlural = value;
		}

		bool[,] IPlayerRestorable.Explored
		{
			get => _explored;
			set => Array.Copy(value, _explored, value.Length);
		}

		bool[,] IPlayerRestorable.Visible
		{
			get => _visible;
			set => Array.Copy(value, _visible, value.Length);
		}

		List<byte> IPlayerRestorable.Advances
		{
			get => _advances;
			set { _advances.Clear(); if (value != null) _advances.AddRange(value); }
		}

		List<byte> IPlayerRestorable.Embassies
		{
			get => _embassies;
			set { _embassies.Clear(); if (value != null) _embassies.AddRange(value); }
		}

		ushort[] IPlayerRestorable.Diplomacy
		{
			get => _diplomacy;
			set
			{
				Array.Clear(_diplomacy, 0, _diplomacy.Length);
				if (value == null)
				{
					return;
				}

				for (var i = 0; i < _diplomacy.Length && i < value.Length; i++)
				{
					_diplomacy[i] = value[i];
				}
			}
		}

		short IPlayerRestorable.Anarchy
		{
			get => _anarchy;
			set => _anarchy = value;
		}

		short IPlayerRestorable.Gold
		{
			get => _gold;
			// Bypass the clamping in the internal setter so the exact persisted value is restored.
			set => _gold = value;
		}

		IAdvance? IPlayerRestorable.CurrentResearch
		{
			get => _currentResearch;
			set => _currentResearch = value;
		}

		int IPlayerRestorable.CityNamesSkipped
		{
			get => CityNamesSkipped;
			set => CityNamesSkipped = value;
		}

		ushort IPlayerRestorable.FutureTechCount
		{
			get => FutureTechCount;
			set => FutureTechCount = value;
		}

		ushort IPlayerRestorable.HumanContactTurn
		{
			get => HumanContactTurn;
			set => HumanContactTurn = value;
		}

		short IPlayerRestorable.StartX
		{
			get => StartX;
			set => StartX = value;
		}

		(short X, short Y)[] IPlayerRestorable.MapPositions
		{
			get => MapPositions;
			set
			{
				var input = value ?? [];
				for (var i = 0; i < MapPositions.Length; i++)
				{
					MapPositions[i] = i < input.Length ? input[i] : ((short)-1, (short)-1);
				}
			}
		}

		string[] IPlayerRestorable.MapPositionNames
		{
			get => MapPositionNames;
			set
			{
				var input = value ?? [];
				for (var i = 0; i < MapPositionNames.Length; i++)
				{
					MapPositionNames[i] = i < input.Length ? (input[i] ?? string.Empty) : string.Empty;
				}
			}
		}

		(short X, short Y) IPlayerRestorable.LastMapPosition
		{
			get => LastMapPosition;
			set => LastMapPosition = value;
		}

		int IPlayerRestorable.MapZoomBasisPoints
		{
			get => MapZoomBasisPoints;
			set => MapZoomBasisPoints = NormalizeMapZoomBasisPoints(value);
		}

		ushort[] IPlayerRestorable.UnitsLost
		{
			get => _unitsLost;
			set
			{
				Array.Clear(_unitsLost, 0, _unitsLost.Length);
				if (value == null)
				{
					return;
				}

				for (var i = 0; i < _unitsLost.Length && i < value.Length; i++)
				{
					_unitsLost[i] = value[i];
				}
			}
		}

		ushort[] IPlayerRestorable.UnitsDestroyedBy
		{
			get => _unitsDestroyedBy;
			set
			{
				Array.Clear(_unitsDestroyedBy, 0, _unitsDestroyedBy.Length);
				if (value == null)
				{
					return;
				}

				for (var i = 0; i < _unitsDestroyedBy.Length && i < value.Length; i++)
				{
					_unitsDestroyedBy[i] = value[i];
				}
			}
		}

		ushort IPlayerRestorable.EpicRanking
		{
			get => _epicRanking;
			set => _epicRanking = value;
		}

		ushort IPlayerRestorable.MilitaryPower
		{
			get => _militaryPower;
			set => _militaryPower = value;
		}

		ushort IPlayerRestorable.CivilizationScore
		{
			get => _civilizationScore;
			set => _civilizationScore = value;
		}

		IGovernment IPlayerRestorable.Government
		{
			get => _government;
			set { if (value != null) _government = value; }
		}

		// Rate setters bypass the side-effect logic (which adjusts ScienceRate) so that
		// all three rates can be restored independently to exactly their saved values.
		int IPlayerRestorable.LuxuriesRate
		{
			get => _luxuriesRate;
			set => _luxuriesRate = value;
		}

		int IPlayerRestorable.TaxesRate
		{
			get => _taxesRate;
			set => _taxesRate = value;
		}

		int IPlayerRestorable.ScienceRate
		{
			get => _scienceRate;
			set => _scienceRate = value;
		}

		short IPlayerRestorable.Science
		{
			get => Science;
			set => Science = value;
		}

		PalaceData IPlayerRestorable.Palace
		{
			get => _palace;
			set => _palace = value ?? new PalaceData();
		}

		SpaceShipComponentType[,] IPlayerRestorable.SpaceShipGrid
		{
			get => SpaceShipGrid;
			set => SpaceShipGrid = value ?? new SpaceShipComponentType[SpaceShipSlotBlueprintFactoryProvider.CanonicalGridWidth, SpaceShipSlotBlueprintFactoryProvider.CanonicalGridHeight];
		}

		ushort IPlayerRestorable.SpaceShipPopulation
		{
			get => SpaceShipPopulation;
			set => SpaceShipPopulation = value;
		}

		short IPlayerRestorable.SpaceShipLaunchYear
		{
			get => SpaceShipLaunchYear;
			set => SpaceShipLaunchYear = value;
		}

		List<ICity> IPlayerRestorable.Cities
		{
			get => RestoredCities;
			set => RestoredCities = value?.ToList() ?? [];
		}
	}
}
