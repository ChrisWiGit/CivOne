using System;
using System.Collections.Generic;
using CivOne.Enums;

namespace CivOne.Services.SpaceShip
{
	/// <summary>
	/// Translates the space race between CivOneX and the spaceship fields of an SVE file.
	/// </summary>
	/// <remarks>
	/// The original keeps a flag word: bit <c>1 &lt;&lt; p</c> marks player <c>p</c>'s ship as in flight,
	/// bit <c>1 &lt;&lt; (p + 8)</c> marks that player <c>p</c> has spaceship parts. The barbarian slot of the high byte,
	/// bit <c>0x100</c>, is the "scoring completed" latch set after the end of the game.
	/// Each player's arrival year is stored next to it (<c>-1</c> = none), the launch year and the population in
	/// separate arrays. The spaceship parts are not translated; CivOneX keeps them in its own grid format.
	/// Known gap: a loaded ship in flight therefore has an empty grid and a success rate of 0, which costs its
	/// score bonus. See <c>TODO.md</c>, chapter "Spaceship".
	/// See <c>docs/plan-calendar.impl.md</c>, Phase 5.
	/// </remarks>
	/// <param name="arrivalService">Decides which ships are in flight and their success rate.</param>
	internal sealed class SveSpaceRaceDelegate(ISpaceShipArrivalService arrivalService)
	{
		/// <summary>
		/// The "scoring completed" bit of the flag word.
		/// </summary>
		public const ushort ScoringCompletedFlag = 0x100;

		private const int SlotCount = 8;
		private const short NoArrival = -1;

		private readonly ISpaceShipArrivalService _arrivalService = arrivalService ?? throw new ArgumentNullException(nameof(arrivalService));

		/// <summary>
		/// Builds the flag word.
		/// </summary>
		/// <param name="players">The players by slot; slot 0 (barbarians) is ignored.</param>
		/// <param name="scoringCompleted">Whether the "scoring completed" latch is set.</param>
		/// <returns>The flag word.</returns>
		public ushort BuildFlags(IReadOnlyList<IPlayerSpaceRace> players, bool scoringCompleted)
		{
			ArgumentNullException.ThrowIfNull(players);

			int flags = scoringCompleted ? ScoringCompletedFlag : 0;
			for (int slot = 1; slot < Math.Min(players.Count, SlotCount); slot++)
			{
				if (_arrivalService.IsInFlight(players[slot]))
				{
					flags |= 1 << slot;
				}

				if (HasParts(players[slot]))
				{
					flags |= 1 << (slot + 8);
				}
			}

			return (ushort)flags;
		}

		/// <summary>
		/// Builds the arrival years; slots without a ship in flight get <c>-1</c>.
		/// </summary>
		/// <param name="players">The players by slot.</param>
		/// <returns>8 arrival years.</returns>
		public short[] BuildArrivalYears(IReadOnlyList<IPlayerSpaceRace> players)
		{
			ArgumentNullException.ThrowIfNull(players);

			short[] years = new short[SlotCount];
			Array.Fill(years, NoArrival);
			for (int slot = 1; slot < Math.Min(players.Count, SlotCount); slot++)
			{
				int? arrivalYear = _arrivalService.ArrivalYear(players[slot]);
				if (arrivalYear.HasValue)
				{
					years[slot] = (short)arrivalYear.Value;
				}
			}

			return years;
		}

		/// <summary>
		/// Gets the success rate of the first ship in flight that matches <paramref name="human"/>.
		/// </summary>
		/// <param name="players">The players by slot.</param>
		/// <param name="humanSlot">The human player's slot.</param>
		/// <param name="human"><c>true</c> for the human ship, <c>false</c> for the first AI ship.</param>
		/// <returns>The success rate in percent, or 0 without such a ship.</returns>
		public short SuccessRate(IReadOnlyList<IPlayerSpaceRace> players, int humanSlot, bool human)
		{
			ArgumentNullException.ThrowIfNull(players);

			for (int slot = 1; slot < Math.Min(players.Count, SlotCount); slot++)
			{
				if ((slot == humanSlot) == human && _arrivalService.IsInFlight(players[slot]))
				{
					return (short)_arrivalService.SuccessRate(players[slot]);
				}
			}

			return 0;
		}

		/// <summary>
		/// Restores each player's arrival year: only a ship marked in flight keeps its stored year.
		/// </summary>
		/// <param name="players">The players by slot.</param>
		/// <param name="flags">The flag word.</param>
		/// <param name="arrivalYears">The stored arrival years.</param>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This class is a delegate, not a static utility.")]
		public void RestoreArrivalYears(IReadOnlyList<IPlayerSpaceRace> players, ushort flags, short[] arrivalYears)
		{
			ArgumentNullException.ThrowIfNull(players);
			ArgumentNullException.ThrowIfNull(arrivalYears);

			for (int slot = 0; slot < players.Count; slot++)
			{
				bool inFlight = slot > 0 && slot < SlotCount && slot < arrivalYears.Length && (flags & (1 << slot)) != 0;
				players[slot].SpaceShipEtaYear = inFlight && arrivalYears[slot] != NoArrival ? arrivalYears[slot] : NoArrival;
			}
		}

		/// <summary>
		/// Reads the "scoring completed" latch from the flag word.
		/// </summary>
		/// <param name="flags">The flag word.</param>
		/// <returns><c>true</c> if the latch is set.</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This class is a delegate, not a static utility.")]
		public bool IsScoringCompleted(ushort flags) => (flags & ScoringCompletedFlag) != 0;

		private static bool HasParts(IPlayerSpaceRace player)
		{
			SpaceShipComponentType[,]? grid = player.SpaceShipGrid;
			if (grid == null)
			{
				return false;
			}

			foreach (SpaceShipComponentType part in grid)
			{
				if (part != SpaceShipComponentType.Empty)
				{
					return true;
				}
			}

			return false;
		}
	}
}
