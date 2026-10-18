using System;
using System.Linq;
using CivOne.Civilizations;
using CivOne.Enums;
using CivOne.Screens.PalaceAssets;
using CivOne.Services.Historian;
using CivOne.src;
using Xunit;

namespace CivOne.UnitTests.Services.Historian
{
	public class HistorianPalaceServiceTests : TestsBase2
	{
		[Fact]
		public void PlayerWithoutCitiesHasNoPalace()
		{
			Player ai = AiPlayer();

			MiniPalaceLayout actual = Service(HistorianAiPalace.BuiltOnly).PalaceFor(ai);

			Assert.Same(MiniPalaceLayout.Empty, actual);
		}

		[Fact]
		public void AiPalaceIsHiddenWithoutEmbassyAboveTheEasiestDifficulty()
		{
			Player ai = AiPlayerWithCityAndBuiltPalace();

			MiniPalaceLayout actual = Service(HistorianAiPalace.BuiltOnly).PalaceFor(ai);

			Assert.Same(MiniPalaceLayout.Empty, actual);
		}

		[Fact]
		public void BuiltOnlyShowsTheBuiltAiPalace()
		{
			Player ai = KnownAiPlayerWithCityAndBuiltPalace();

			MiniPalaceLayout actual = Service(HistorianAiPalace.BuiltOnly).PalaceFor(ai);

			Assert.Equal(3, actual.Level(4));
			Assert.Equal(PalaceStyle.Islamic, actual.Style(4));
		}

		[Fact]
		public void BuiltOrGeneratedPrefersTheBuiltAiPalace()
		{
			Player ai = KnownAiPlayerWithCityAndBuiltPalace();

			MiniPalaceLayout actual = Service(HistorianAiPalace.BuiltOrGenerated).PalaceFor(ai);

			Assert.Equal(PalaceStyle.Islamic, actual.Style(4));
		}

		[Fact]
		public void GeneratedIgnoresTheBuiltAiPalace()
		{
			Player ai = KnownAiPlayerWithCityAndBuiltPalace();

			MiniPalaceLayout actual = Service(HistorianAiPalace.Generated).PalaceFor(ai);

			PalaceStyle playerStyle = (PalaceStyle)((Game.Instance.PlayerNumber(ai) % 3) + 1);
			Assert.Equal(playerStyle, actual.Style(4));
			Assert.NotEqual(3, actual.Level(4));
		}

		[Fact]
		public void BuiltOrGeneratedGeneratesForAnUnbuiltAiPalace()
		{
			Player ai = KnownAiPlayerWithCity();

			MiniPalaceLayout generated = Service(HistorianAiPalace.Generated).PalaceFor(ai);
			MiniPalaceLayout actual = Service(HistorianAiPalace.BuiltOrGenerated).PalaceFor(ai);

			int[] expected = [.. Enumerable.Range(0, MiniPalaceLayout.SlotCount).Select(generated.Level)];
			Assert.Equal(expected, Enumerable.Range(0, MiniPalaceLayout.SlotCount).Select(actual.Level));
		}

		private static HistorianPalaceService Service(HistorianAiPalace mode)
			=> new(settings: new FakeSettings(mode));

		private static Player AiPlayer()
			=> Game.Instance.Players.First(p => !p.IsHuman && p.Civilization is not Barbarian);

		private static Player AiPlayerWithCityAndBuiltPalace()
		{
			Player ai = AiPlayer();
			Assert.NotNull(Game.Instance.AddCity(ai, 0, 40, 30));
			ai.Palace.SetPalace(3, (byte)PalaceStyle.Islamic, 3);
			return ai;
		}

		private static Player KnownAiPlayerWithCity()
		{
			Player ai = AiPlayer();
			Assert.NotNull(Game.Instance.AddCity(ai, 0, 40, 30));
			Game.Instance.HumanPlayer.EstablishEmbassy(ai);
			return ai;
		}

		private static Player KnownAiPlayerWithCityAndBuiltPalace()
		{
			Player ai = KnownAiPlayerWithCity();
			ai.Palace.SetPalace(3, (byte)PalaceStyle.Islamic, 3);
			return ai;
		}

		private sealed class FakeSettings(HistorianAiPalace mode) : ISettings
		{
			public string StorageDirectory => throw new NotImplementedException();
			public string CaptureDirectory => throw new NotImplementedException();
			public string DataDirectory => throw new NotImplementedException();
			public string PluginsDirectory => throw new NotImplementedException();
			public string SavesDirectory => throw new NotImplementedException();
			public string CosSavesDirectory => throw new NotImplementedException();
			public string MapsDirectory => throw new NotImplementedException();
			public string PicturesDirectory => throw new NotImplementedException();
			public string SoundsDirectory => throw new NotImplementedException();
			public GameOption Sound => throw new NotImplementedException();
			public bool RevealWorld => false;
			public bool DebugMenu => false;
			public bool AutoSaveOnQuit => false;
			public bool ConfirmExit => true;
			public HistorianAiPalace HistorianAiPalace => mode;
		}
	}
}
