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
using CivOne.Civilizations;
using CivOne.Enums;
using CivOne.Persistence.Model;
using CivOne.Services.Random;
using CivOne.Units;

namespace CivOne.Services.StartPositions
{
	/// <summary>
	/// Assigns random starting positions to the playable civilizations, using a selectable
	/// <see cref="IStartPositionService"/> algorithm.
	///
	/// Used by the terrain editor to fill in missing start positions without touching the
	/// ones a user placed manually: existing positions are passed to the algorithm as fixed
	/// anchors so the freshly generated ones stay clear of them.
	///
	/// Candidates are all non-barbarian civilizations, not just the players in the current game,
	/// so a custom map can be populated with a start position for every civilization the editor
	/// can place one for.
	/// </summary>
	/// <param name="playerGame">The current game, used to enumerate units and cities to avoid.</param>
	/// <param name="civilizations">All known civilizations; barbarians are skipped.</param>
	/// <param name="mapEditor">The map to read existing start positions from and write new ones to.</param>
	/// <param name="randomService">The random number generator used for reproducible placement.</param>
	internal sealed class AutoStartPositionDelegate(
		IPlayerGame playerGame,
		ICivilization[] civilizations,
		IMapEditor mapEditor,
		IRandomService randomService)
	{
		private readonly IPlayerGame _playerGame = playerGame ?? throw new ArgumentNullException(nameof(playerGame));
		private readonly ICivilization[] _civilizations = civilizations ?? throw new ArgumentNullException(nameof(civilizations));
		private readonly IMapEditor _mapEditor = mapEditor ?? throw new ArgumentNullException(nameof(mapEditor));
		private readonly IRandomService _randomService = randomService ?? throw new ArgumentNullException(nameof(randomService));

		/// <summary>
		/// Finds and stores starting positions for the playable civilizations.
		///
		/// When <paramref name="overwriteExisting"/> is <see langword="false"/> (the default), existing
		/// start positions are kept and used as fixed anchors, so only civilizations without one get a fresh
		/// position and newly generated positions stay clear of the existing ones.
		///
		/// When <paramref name="overwriteExisting"/> is <see langword="true"/>, every civilization is
		/// redistributed from scratch, ignoring and overwriting any current start positions.
		/// </summary>
		/// <param name="algorithm">The placement algorithm to use.</param>
		/// <param name="overwriteExisting">Whether to redistribute all civilizations instead of only the ones without a position.</param>
		/// <returns>The number of start positions that were assigned.</returns>
		public int AssignStartPositions(Settings.StartPositionAlgorithmType algorithm, bool overwriteExisting = false)
		{
			ICivilization[] civilizations = [.. _civilizations.Where(c => c is not Barbarian)];
			if (civilizations.Length == 0)
			{
				return 0;
			}

			StartPositionCandidate[] candidates = [.. civilizations.Select(c => BuildCandidate(c, overwriteExisting))];
			StartPositionContext context = BuildContext(candidates);

			IStartPositionService service = StartPositionServiceFactory.Create(algorithm);
			IReadOnlyList<StartPositionResult> results = service.FindStartPositions(candidates, context);

			int assigned = 0;
			for (int i = 0; i < candidates.Length; i++)
			{
				StartPositionResult result = results[i];

				// When only filling gaps, keep manually placed positions; when overwriting, replace them all.
				if (!result.Success || (!overwriteExisting && candidates[i].MapStartPosition != null))
				{
					continue;
				}

				_mapEditor.SetStartPosition((Civilization)result.Civilization.Id, result.Position);
				assigned++;
			}

			return assigned;
		}

		private StartPositionCandidate BuildCandidate(ICivilization civilization, bool overwriteExisting)
		{
			// When overwriting, ignore any current position so the algorithm treats every civilization as unplaced.
			MapLocation? existing = !overwriteExisting && _mapEditor.TryGetStartPosition(civilization, out MapLocation? location) ? location : null;
			return new StartPositionCandidate
			{
				Civilization = civilization,
				MapStartPosition = existing,
			};
		}

		private StartPositionContext BuildContext(IReadOnlyList<StartPositionCandidate> candidates)
		{
			IUnit[] units = _playerGame.GetUnits();

			// Only honour existing map positions as fixed anchors. The hardcoded civilization defaults
			// (Earth start coordinates) are meaningless on a custom map, so IsFirstGameTurn is tied to
			// AnyFixedMapStartPosition: when there is nothing to anchor, every position is computed and
			// the default-position branch in the resolver never fires.
			bool anyFixed = candidates.Any(c => c.MapStartPosition != null);
			return new StartPositionContext
			{
				Map = _mapEditor,
				RandomService = _randomService,
				IsFirstGameTurn = anyFixed,
				AnyFixedMapStartPosition = anyFixed,
				GameTurn = 0,
				OccupiedTiles = [.. units.Select(u => new MapLocation((uint)u.X, (uint)u.Y))],
				CityLocations = [.. _playerGame.GetCities().Select(c => new MapLocation((uint)c.X, (uint)c.Y))],
				SettlerLocations = [.. units.Where(u => u is Settlers).Select(u => new MapLocation((uint)u.X, (uint)u.Y))],
			};
		}
	}
}
