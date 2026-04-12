using System;
using System.Collections.Generic;

namespace CivOne.Persistence.Model
{
	/// <summary>
	/// Resolves and applies units destroyed counts from player GUIDs to player indices.
	/// This class handles the mapping of destruction counts from GUID-based references
	/// to index-based arrays, with proper validation and sanitization.
	/// </summary>
	public class UnitsDestroyedByResolver(
		IValueSanitizer yamlReadValueSanitizer
	)
	{
		/// <summary>
		/// Resolves units destroyed by GUID mapping for all players and applies the resolved counts.
		/// </summary>
		/// <param name="players">Array of mapped players to apply resolved destruction counts to.</param>
		/// <param name="playerDtos">List of player DTOs containing the GUID-based destruction mappings.</param>
		public void ResolveAndApply(IPlayer[] players, List<PlayerDto> playerDtos)
		{
			if (!ValidateInput(players, playerDtos))
			{
				return;
			}

			for (var ownerIndex = 0; ownerIndex < playerDtos.Count && ownerIndex < players.Length; ownerIndex++)
			{
				ResolvePlayerUnitsDestroyedBy(players, playerDtos[ownerIndex], ownerIndex);
			}
		}

		/// <summary>
		/// Resolves units destroyed by GUID mapping for a single player and applies the result.
		/// </summary>
		/// <param name="players">Array of all players (used for GUID lookup).</param>
		/// <param name="playerDto">Player DTO containing the GUID-based destruction mapping.</param>
		/// <param name="ownerIndex">Index of the owner player in the players array.</param>
		private void ResolvePlayerUnitsDestroyedBy(IPlayer[] players, PlayerDto playerDto, int ownerIndex)
		{
			if (!IsResolvablePlayer(playerDto, players, ownerIndex))
			{
				return;
			}

			var ownerRestorable = (IPlayerRestorable)players[ownerIndex];
			var resolvedCounts = InitializeResolvedArray(ownerRestorable, players.Length);

			ApplyGuidMappings(players, playerDto.UnitsDestroyedByByPlayerGuid, resolvedCounts);

			ownerRestorable.UnitsDestroyedBy = resolvedCounts;
		}

		/// <summary>
		/// Validates input parameters before processing.
		/// </summary>
		private static bool ValidateInput(IPlayer[] players, List<PlayerDto> playerDtos)
		{
			return players != null
				&& players.Length > 0
				&& playerDtos != null
				&& playerDtos.Count > 0;
		}

		/// <summary>
		/// Checks if a player can be resolved (has valid data and is restorable).
		/// </summary>
		private static bool IsResolvablePlayer(PlayerDto playerDto, IPlayer[] players, int ownerIndex)
		{
			if (playerDto?.UnitsDestroyedByByPlayerGuid == null || playerDto.UnitsDestroyedByByPlayerGuid.Count == 0)
			{
				return false;
			}

			if (players[ownerIndex] is not IPlayerRestorable)
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// Initializes the resolved array with existing destruction counts if available.
		/// Ensures minimum size to accommodate all possible players.
		/// </summary>
		private static ushort[] InitializeResolvedArray(IPlayerRestorable ownerRestorable, int playerCount)
		{
			var resolved = new ushort[Math.Max(8, playerCount)];
			if (ownerRestorable.UnitsDestroyedBy != null)
			{
				Array.Copy(
					ownerRestorable.UnitsDestroyedBy,
					resolved,
					Math.Min(ownerRestorable.UnitsDestroyedBy.Length, resolved.Length)
				);
			}

			return resolved;
		}

		/// <summary>
		/// Applies GUID-to-index mappings for destruction counts to the resolved array.
		/// </summary>
		private void ApplyGuidMappings(
			IPlayer[] players,
			Dictionary<Guid, long> guidMappings,
			ushort[] resolvedCounts
		)
		{
			foreach (var pair in guidMappings)
			{
				ApplyGuidMapping(players, pair.Key, pair.Value, resolvedCounts);
			}
		}

		/// <summary>
		/// Applies a single GUID-to-index mapping for destruction counts.
		/// Validates the target index before applying the sanitized value.
		/// </summary>
		private void ApplyGuidMapping(IPlayer[] players, Guid targetGuid, long destroyedCount, ushort[] resolvedCounts)
		{
			var targetIndex = FindPlayerIndexByGuid(players, targetGuid);
			if (!IsValidResolvedIndex(targetIndex, resolvedCounts.Length))
			{
				return;
			}

			var sanitizedCount = (ushort)yamlReadValueSanitizer.ClampToInt32(
				destroyedCount,
				nameof(UnitsDestroyedByResolver),
				$"{nameof(PlayerDto.UnitsDestroyedByByPlayerGuid)}[{targetGuid}]",
				min: 0,
				max: ushort.MaxValue
			);

			resolvedCounts[targetIndex] = sanitizedCount;
		}

		/// <summary>
		/// Validates that a resolved index is within bounds of the array.
		/// </summary>
		private static bool IsValidResolvedIndex(int targetIndex, int resolvedLength)
		{
			return targetIndex >= 0 && targetIndex < resolvedLength;
		}

		/// <summary>
		/// Finds a player index by matching their GUID.
		/// Returns -1 if not found or GUID is empty.
		/// </summary>
		private static int FindPlayerIndexByGuid(IPlayer[] players, Guid playerGuid)
		{
			if (playerGuid == Guid.Empty)
			{
				return -1;
			}

			for (var i = 0; i < players.Length; i++)
			{
				if (players[i].PlayerGuid == playerGuid)
				{
					return i;
				}
			}

			return -1;
		}
	}
}
