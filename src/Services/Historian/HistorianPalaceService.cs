using System;
using System.Linq;
using CivOne.Enums;
using CivOne.Screens.PalaceAssets;
using CivOne.Screens.Services;

namespace CivOne.Services.Historian
{
	/// <summary>
	/// Default <see cref="IHistorianPalaceService"/>, as in the original: the human player's real palace,
	/// and for AI players a palace generated from their city sum by <see cref="MiniPalaceGeneratorDelegate"/>.
	/// A player without cities gets no palace, and an AI palace needs the easiest difficulty or an embassy,
	/// as in the original.
	/// <see cref="ISettings.HistorianAiPalace"/> can show the AI player's built palace instead.
	/// </summary>
	/// <remarks>
	/// The original accumulates <c>ActualSize + Happy - Unhappy</c> per city during the real turn processing and the
	/// Historian reads it before the wonder bonus and pollution are added.
	/// CivOneX computes the same sum live; red shirts count as unhappy.
	/// See <c>docs/plan-civilizationScore.impl.md</c>, Phase 5.
	/// </remarks>
	/// <param name="game">The game for player slots; <c>null</c> reads <see cref="Game.Instance"/>.</param>
	/// <param name="map">The map for the random seed; <c>null</c> reads <see cref="Map.Instance"/>.</param>
	/// <param name="settings">Chooses the AI palace (<see cref="ISettings.HistorianAiPalace"/>); <c>null</c> reads <see cref="Settings.Instance"/>.</param>
	internal sealed class HistorianPalaceService(
		Game? game = null,
		IMapTerrain? map = null,
		ISettings? settings = null) : IHistorianPalaceService
	{
		private readonly MiniPalaceGeneratorDelegate _generator = new();
		private readonly Game? _game = game;
		private readonly IMapTerrain? _map = map;

		private Game Game => _game ?? Game.Instance;

		private IMapTerrain Map => _map ?? CivOne.Map.Instance;

		private readonly ISettings? _settings = settings;

		private ISettings Settings => _settings ?? CivOne.Settings.Instance;

		/// <inheritdoc />
		public MiniPalaceLayout PalaceFor(Player player)
		{
			ArgumentNullException.ThrowIfNull(player);

			if (player.Cities.Length == 0)
			{
				return MiniPalaceLayout.Empty;
			}

			if (player.IsHuman)
			{
				return MiniPalaceLayout.FromPalace(player.Palace);
			}

			if (!IsAiPalaceKnown(player))
			{
				return MiniPalaceLayout.Empty;
			}

			return Settings.HistorianAiPalace switch
			{
				HistorianAiPalace.BuiltOnly => MiniPalaceLayout.FromPalace(player.Palace),
				HistorianAiPalace.BuiltOrGenerated when player.Palace.UpgradeCount > 0 => MiniPalaceLayout.FromPalace(player.Palace),
				_ => Generate(player),
			};
		}

		private MiniPalaceLayout Generate(Player player)
		{
			int buildCount = PalaceLevel(CitySum(player));
			return _generator.Generate(Game.PlayerNumber(player), buildCount, Map.TerrainMasterWord);
		}

		/// <summary>
		/// Like the original, an AI palace is shown only on the easiest difficulty or with an embassy.
		/// </summary>
		private bool IsAiPalaceKnown(Player player)
		{
			Player? human = Game.HumanPlayer;
			return Game.Difficulty == 0 || (human != null && human.HasEmbassy(player));
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
