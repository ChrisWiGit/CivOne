using System;
using System.Linq;
using CivOne.Screens.Services;

namespace CivOne.Services.Historian
{
	/// <summary>
	/// Default <see cref="IHistorianPalaceService"/>, as in the original: the human player's real palace,
	/// and for AI players a palace generated from their city sum by <see cref="MiniPalaceGeneratorDelegate"/>.
	/// </summary>
	/// <remarks>
	/// The original accumulates <c>ActualSize + Happy - Unhappy</c> per city during the real turn processing and the
	/// Historian reads it before the wonder bonus and pollution are added.
	/// CivOneX computes the same sum live; red shirts count as unhappy.
	/// See <c>docs/plan-civilizationScore.impl.md</c>, Phase 5.
	/// </remarks>
	/// <param name="game">The game for player slots; <c>null</c> reads <see cref="Game.Instance"/>.</param>
	/// <param name="map">The map for the random seed; <c>null</c> reads <see cref="Map.Instance"/>.</param>
	internal sealed class HistorianPalaceService(
		Game? game = null,
		IMapTerrain? map = null) : IHistorianPalaceService
	{
		private readonly MiniPalaceGeneratorDelegate _generator = new();
		private readonly Game? _game = game;
		private readonly IMapTerrain? _map = map;

		private Game Game => _game ?? Game.Instance;

		private IMapTerrain Map => _map ?? CivOne.Map.Instance;

		/// <inheritdoc />
		public IPalaceData PalaceFor(Player player)
		{
			ArgumentNullException.ThrowIfNull(player);

			if (player.IsHuman)
			{
				return player.Palace;
			}

			int buildCount = PalaceLevel(CitySum(player));
			return _generator.Generate(Game.PlayerNumber(player), buildCount, Map.TerrainMasterWord);
		}

		/// <inheritdoc />
		public int CitySum(Player player)
		{
			ArgumentNullException.ThrowIfNull(player);

			return player.Cities.Sum(city =>
			{
				CitizenTypes citizens = city.GetCitizenTypes();
				return city.Size + citizens.happy - (citizens.unhappy + citizens.redShirt);
			});
		}

		/// <inheritdoc />
		public int PalaceLevel(int citySum)
		{
			int level = 0;
			do
			{
				level += 5;
				citySum -= level;
			}
			while (citySum > 0);

			return (level / 5) - 1;
		}
	}
}
