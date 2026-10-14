using System;
using CivOne.Civilizations;

namespace CivOne.Services.Score
{
	/// <summary>
	/// <see cref="IConquestParticipant"/> view of a <see cref="Player"/>.
	/// </summary>
	internal sealed class PlayerConquestParticipant : IConquestParticipant
	{
		private readonly Player _player;

		/// <summary>
		/// Creates a view of <paramref name="player"/>.
		/// </summary>
		/// <param name="player">The player to read.</param>
		public PlayerConquestParticipant(Player player)
		{
			ArgumentNullException.ThrowIfNull(player);
			_player = player;
		}

		/// <inheritdoc />
		public bool IsHuman => _player.IsHuman;

		/// <inheritdoc />
		public bool IsBarbarian => _player.Civilization is Barbarian;

		/// <inheritdoc />
		/// <remarks>
		/// <see cref="Player.IsDestroyed"/> runs the extinction check, which disbands the units of a player without
		/// cities or settlers. The turn loop runs the same check every turn, so this adds no new effect.
		/// </remarks>
		public bool IsDestroyed => _player.IsDestroyed;
	}
}
