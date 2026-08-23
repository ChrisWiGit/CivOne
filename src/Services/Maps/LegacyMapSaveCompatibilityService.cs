// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Globalization;

namespace CivOne.Services.Maps
{
	/// <summary>
	/// Result of checking whether the current map can be written as a legacy Civilization I
	/// <c>*.map</c> file instead of the richer <c>*.comap</c> format.
	/// </summary>
	/// <remarks>
	/// The legacy format stores strictly less than <c>*.comap</c> (no start positions, pollution or
	/// fortresses, a fixed 80x50 grid and a 16-bit map seed), so a save is only offered when none of
	/// that extra information would be silently lost.
	/// </remarks>
	public sealed class LegacyMapSaveCompatibilityResult
	{
		/// <summary>
		/// Shared result for the compatible case.
		/// </summary>
		public static LegacyMapSaveCompatibilityResult Compatible { get; } = new(true, string.Empty);

		/// <summary>
		/// Creates a new result.
		/// </summary>
		/// <param name="canSaveAsLegacyMap">Whether the map can be saved as a legacy <c>*.map</c> file.</param>
		/// <param name="reason">Human-readable explanation when <paramref name="canSaveAsLegacyMap"/> is false; empty otherwise.</param>
		public LegacyMapSaveCompatibilityResult(bool canSaveAsLegacyMap, string reason)
		{
			CanSaveAsLegacyMap = canSaveAsLegacyMap;
			Reason = reason ?? string.Empty;
		}

		/// <summary>
		/// Whether the map can be written as a legacy <c>*.map</c> file without losing information.
		/// </summary>
		public bool CanSaveAsLegacyMap { get; }

		/// <summary>
		/// Explanation of why the legacy save is unavailable.
		/// Empty when <see cref="CanSaveAsLegacyMap"/> is true.
		/// </summary>
		public string Reason { get; }
	}

	/// <summary>
	/// Immutable snapshot of the map facts relevant for legacy <c>*.map</c> compatibility.
	/// Passed to <see cref="ILegacyMapSaveCompatibilityService.Evaluate"/> so the rules can be
	/// unit tested without a live <c>Map</c>.
	/// </summary>
	public sealed class LegacyMapSaveCompatibilitySnapshot
	{
		/// <summary>
		/// Creates a new snapshot.
		/// </summary>
		/// <param name="mapWidth">Current map width in tiles.</param>
		/// <param name="mapHeight">Current map height in tiles.</param>
		/// <param name="mapSeed">Map seed as it would be encoded into a <c>*.comap</c> file (unsigned).</param>
		/// <param name="startPositionCount">Number of civilization start positions currently set.</param>
		/// <param name="hasPollution">Whether at least one tile is polluted.</param>
		/// <param name="hasFortress">Whether at least one tile has a fortress.</param>
		public LegacyMapSaveCompatibilitySnapshot(
			int mapWidth,
			int mapHeight,
			uint mapSeed,
			int startPositionCount,
			bool hasPollution,
			bool hasFortress)
		{
			MapWidth = mapWidth;
			MapHeight = mapHeight;
			MapSeed = mapSeed;
			StartPositionCount = startPositionCount;
			HasPollution = hasPollution;
			HasFortress = hasFortress;
		}

		/// <summary>Current map width in tiles.</summary>
		public int MapWidth { get; }

		/// <summary>Current map height in tiles.</summary>
		public int MapHeight { get; }

		/// <summary>Map seed as it would be encoded into a <c>*.comap</c> file (unsigned).</summary>
		public uint MapSeed { get; }

		/// <summary>Number of civilization start positions currently set.</summary>
		public int StartPositionCount { get; }

		/// <summary>Whether at least one tile is polluted.</summary>
		public bool HasPollution { get; }

		/// <summary>Whether at least one tile has a fortress.</summary>
		public bool HasFortress { get; }
	}

	/// <summary>
	/// Evaluates whether a map snapshot can be written as a legacy Civilization I <c>*.map</c> file.
	/// </summary>
	public interface ILegacyMapSaveCompatibilityService
	{
		/// <summary>
		/// Evaluates the snapshot against the legacy <c>*.map</c> format limits.
		/// </summary>
		/// <param name="snapshot">The map snapshot to evaluate.</param>
		/// <returns>A result describing whether the legacy save is available and, if not, why.</returns>
		LegacyMapSaveCompatibilityResult Evaluate(LegacyMapSaveCompatibilitySnapshot snapshot);
	}

	/// <summary>
	/// Stateless implementation of <see cref="ILegacyMapSaveCompatibilityService"/>.
	/// </summary>
	public sealed class LegacyMapSaveCompatibilityService : ILegacyMapSaveCompatibilityService
	{
		private const int LegacyMapWidth = 80;
		private const int LegacyMapHeight = 50;
		private const uint LegacyMaxMapSeed = ushort.MaxValue;

		/// <inheritdoc/>
		public LegacyMapSaveCompatibilityResult Evaluate(LegacyMapSaveCompatibilitySnapshot snapshot)
		{
			System.ArgumentNullException.ThrowIfNull(snapshot);

			if (snapshot.MapWidth != LegacyMapWidth || snapshot.MapHeight != LegacyMapHeight)
			{
				return new LegacyMapSaveCompatibilityResult(false, string.Format(
					CultureInfo.InvariantCulture,
					"Map size {0}x{1} is not supported by the legacy Civ1 map format ({2}x{3} required).",
					snapshot.MapWidth, snapshot.MapHeight, LegacyMapWidth, LegacyMapHeight));
			}

			if (snapshot.MapSeed > LegacyMaxMapSeed)
			{
				return new LegacyMapSaveCompatibilityResult(false, string.Format(
					CultureInfo.InvariantCulture,
					"Map seed {0} exceeds the 16-bit range the legacy Civ1 map format can store.",
					snapshot.MapSeed));
			}

			if (snapshot.StartPositionCount > 0)
			{
				return new LegacyMapSaveCompatibilityResult(false,
					"The legacy Civ1 map format cannot store custom start positions.");
			}

			if (snapshot.HasPollution)
			{
				return new LegacyMapSaveCompatibilityResult(false,
					"The legacy Civ1 map format cannot store pollution.");
			}

			if (snapshot.HasFortress)
			{
				return new LegacyMapSaveCompatibilityResult(false,
					"The legacy Civ1 map format cannot store fortresses.");
			}

			return LegacyMapSaveCompatibilityResult.Compatible;
		}
	}
}
