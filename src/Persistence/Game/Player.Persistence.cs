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
using CivOne.Governments;
using CivOne.Persistence.Model;

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

		IAdvance IPlayerRestorable.CurrentResearch
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

		short IPlayerRestorable.StartX
		{
			get => StartX;
			set => StartX = value;
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

		List<ICity> IPlayerRestorable.Cities
		{
			get => RestoredCities;
			set => RestoredCities = value?.ToList() ?? [];
		}
	}
}
