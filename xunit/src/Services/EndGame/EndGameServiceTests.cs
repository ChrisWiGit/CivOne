using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.IO;
using CivOne.Persistence.Game;
using CivOne.Screens;
using CivOne.Screens.Dialogs;
using CivOne.Services.EndGame;
using CivOne.Services.Screen;
using CivOne.Sound;
using CivOne.Sound.Playback;
using Xunit;

namespace CivOne.UnitTests.Services.EndGame
{
	public class EndGameServiceTests
	{
		private readonly List<string> _shown = [];
		private readonly RecordingSoundPlayback _sound = new();
		private readonly FakeEndOfGameState _state = new();
		private PalaceData? _spaceVictoryBackground;
		private TimeSpan? _spaceVictoryDuration;

		private static readonly SpaceVictoryText Text = new(["flight"], ["landing"]);
		private bool _gameEnded;

		[Fact]
		public async Task RetireShowsPowerGraphScoreFamousPersonsAndHallOfFame()
		{
			await CreateService().HandleRetireAsync();

			Assert.Equal(
				["PowerGraph", "CivilizationScore", "FamousPersons", "HallOfFame", "Credits"],
				_shown);
			Assert.True(_gameEnded);
		}

		[Fact]
		public async Task DefeatShowsNoScore()
		{
			await CreateService().HandleDefeatAsync();

			Assert.Equal(["Defeat", "Credits"], _shown);
		}

		[Fact]
		public async Task ConquestShowsVictoryThenFullScore()
		{
			await CreateService().HandleConquestAsync();

			Assert.Equal(
				["Victory", "PowerGraph", "CivilizationScore", "FamousPersons", "HallOfFame", "Credits"],
				_shown);
		}

		[Fact]
		public async Task RetireAfterKeepPlayingShowsOnlyThePowerGraph()
		{
			_state.ScoringCompleted = true;

			await CreateService().HandleRetireAsync();

			Assert.Equal(["PowerGraph", "Credits"], _shown);
		}

		[Fact]
		public async Task YearEndShowsNewsFlowAndAsksToKeepPlaying()
		{
			GameEndSequence sequence = new() { YearEndNews = ["dynasty"], YearEndTune = "tune" };

			bool keepPlaying = await CreateService(keepPlaying: true).HandleGameEndAsync(sequence);

			Assert.True(keepPlaying);
			Assert.Equal(
				["Newspaper", "PowerGraph", "CivilizationScore", "FamousPersons", "HallOfFame", "KeepPlaying"],
				_shown);
			Assert.Equal(["tune", "stop"], _sound.Calls);
			Assert.True(_state.ScoringCompleted);
			Assert.False(_gameEnded);
		}

		[Fact]
		public async Task NoImDoneEndsTheGame()
		{
			GameEndSequence sequence = new() { YearEndNews = ["dynasty"] };

			bool keepPlaying = await CreateService(keepPlaying: false).HandleGameEndAsync(sequence);

			Assert.False(keepPlaying);
			Assert.Equal("Credits", _shown[^1]);
			Assert.True(_gameEnded);
		}

		[Fact]
		public async Task HumanArrivalShowsTheSpaceStationWithWinMusicAfterTheFlight()
		{
			GameEndSequence sequence = new()
			{
				SpaceVictoryText = Text,
				SpaceshipWinnerIsHuman = true,
				HumanPalace = new PalaceData(),
				SpaceshipNews = ["arrives"],
			};

			await CreateService(keepPlaying: true).HandleGameEndAsync(sequence);

			Assert.Equal(
				["SpaceVictory", "Newspaper", "PowerGraph", "CivilizationScore", "FamousPersons", "HallOfFame", "KeepPlaying"],
				_shown);
			Assert.Null(_spaceVictoryBackground);
			Assert.Equal([SoundNames.MusicTitle, "stop", SoundNames.MusicWin, "stop"], _sound.Calls);
		}

		[Fact]
		public async Task RivalArrivalShowsTheHumanPalaceAndKeepsTheTitleMusic()
		{
			PalaceData palace = new();
			GameEndSequence sequence = new()
			{
				SpaceVictoryText = Text,
				SpaceshipWinnerIsHuman = false,
				HumanPalace = palace,
				SpaceshipNews = ["arrives"],
			};

			await CreateService(keepPlaying: true).HandleGameEndAsync(sequence);

			Assert.Equal(
				["SpaceVictory", "Newspaper", "PowerGraph", "CivilizationScore", "FamousPersons", "HallOfFame", "KeepPlaying"],
				_shown);
			Assert.Same(palace, _spaceVictoryBackground);
			Assert.Equal([SoundNames.MusicTitle, "stop"], _sound.Calls);
		}

		[Fact]
		public async Task SpaceVictoryFlightIsTimedToTheTitleMusic()
		{
			TimeSpan titleLength = TimeSpan.FromSeconds(42);
			_sound.Durations[SoundNames.MusicTitle] = titleLength;
			GameEndSequence sequence = new()
			{
				SpaceVictoryText = Text,
				SpaceshipWinnerIsHuman = true,
				SpaceshipNews = ["arrives"],
			};

			await CreateService(keepPlaying: true).HandleGameEndAsync(sequence);

			Assert.Equal(titleLength, _spaceVictoryDuration);
		}

		[Fact]
		public async Task LaterEventsAfterKeepPlayingAskAgainWithoutScore()
		{
			_state.ScoringCompleted = true;

			GameEndSequence sequence = new()
			{
				SpaceVictoryText = Text,
				SpaceshipWinnerIsHuman = true,
				SpaceshipNews = ["arrives"],
			};

			bool keepPlaying = await CreateService(keepPlaying: true).HandleGameEndAsync(sequence);

			Assert.True(keepPlaying);
			Assert.Equal(["SpaceVictory", "Newspaper", "KeepPlaying"], _shown);
		}

		[Fact]
		public async Task ConquestEndsTheGameWithoutAskingAlsoWithTheYearEnd()
		{
			GameEndSequence sequence = new() { ConquestNews = ["conquered"], YearEndNews = ["dynasty"] };

			bool keepPlaying = await CreateService(keepPlaying: true).HandleGameEndAsync(sequence);

			Assert.False(keepPlaying);
			Assert.Equal(
				[
					"Newspaper", "Conquest", "Newspaper", "Victory",
					"PowerGraph", "CivilizationScore", "FamousPersons", "HallOfFame", "Credits",
				],
				_shown);
			Assert.True(_gameEnded);
		}

		private EndGameService CreateService(bool keepPlaying = false)
		{
			ClosingScreenService screens = new(_shown);
			NamedScreenFactory screenFactory = new(
				keepPlaying,
				background => _spaceVictoryBackground = background,
				duration => _spaceVictoryDuration = duration);

			return new EndGameService(
				screenCommand: screens,
				screenQuery: screens,
				gameService: new FakeGameService(() => _gameEnded = true),
				screens: screenFactory,
				endOfGameState: _state,
				sound: _sound);
		}

		/// <summary>Records each played tune by name and each stop as "stop".</summary>
		private sealed class RecordingSoundPlayback : ISoundPlaybackStrategy
		{
			public List<string> Calls { get; } = [];
			public Dictionary<string, TimeSpan> Durations { get; } = [];

			public bool PlaySound(string soundName)
			{
				Calls.Add(soundName);
				return true;
			}

			public void Abort() => Calls.Add("stop");

			public bool TryGetDuration(string soundName, out TimeSpan duration)
				=> Durations.TryGetValue(soundName, out duration);
		}

		private sealed class NamedScreenFactory(
			bool keepPlaying,
			Action<PalaceData?> onSpaceVictory,
			Action<TimeSpan?> onFlightDuration) : IEndGameScreenFactory
		{
			public IScreen Victory() => new NamedScreen("Victory");
			public IScreen Defeat() => new NamedScreen("Defeat");
			public IScreen SpaceVictory(
				SpaceVictoryText text,
				PalaceData? rivalBackground,
				Action? onLanding,
				TimeSpan? flightDuration)
			{
				onSpaceVictory(rivalBackground);
				onFlightDuration(flightDuration);
				onLanding?.Invoke();
				return new NamedScreen("SpaceVictory");
			}
			public IScreen PowerGraph() => new NamedScreen("PowerGraph");
			public IScreen CivilizationScore() => new NamedScreen("CivilizationScore");
			public IScreen FamousPersons() => new NamedScreen("FamousPersons");
			public IScreen HallOfFame() => new NamedScreen("HallOfFame");
			public IScreen Credits() => new NamedScreen("Credits");
			public IScreen Newspaper(string[] lines) => new NamedScreen("Newspaper");
			public IScreen Conquest() => new NamedScreen("Conquest");
			public IKeepPlayingDialog KeepPlaying() => new NamedDialog("KeepPlaying", keepPlaying);
		}

		private sealed class FakeEndOfGameState : IEndOfGameState
		{
			public bool ScoringCompleted { get; set; }
		}

		private sealed class NamedDialog(string name, bool keepPlaying) : NamedScreen(name), IKeepPlayingDialog
		{
			public bool KeepPlaying { get; } = keepPlaying;
		}

		/// <summary>Records each added screen and closes it at once, so the sequence runs to the end.</summary>
		private sealed class ClosingScreenService(List<string> shown) : IScreenCommandService, IScreenQueryService
		{
			public IScreen[] Screens => [];
			public IScreen? LastScreen => null;
			public IScreen? TopScreen => null;
			public bool HasTopScreen<T>() where T : IScreen => false;
			public bool HasLastScreen<T>() where T : IScreen => false;
			public bool HasScreenType<T>() where T : IScreen => false;

			public void AddScreen(IScreen screen)
			{
				NamedScreen named = (NamedScreen)screen;
				shown.Add(named.Name);
				named.Close();
			}

			public void DestroyScreen(IScreen screen)
			{
			}
		}

		private sealed class FakeGameService(Action onEnd) : IGameService
		{
			public void End() => onEnd();
		}

		private class NamedScreen(string name) : IScreen
		{
			public string Name { get; } = name;
			public event EventHandler? Closed;
			public void Close() => Closed?.Invoke(this, EventArgs.Empty);
			public MouseCursor Cursor => MouseCursor.None;
			public bool UseFullWindowCanvas => false;
			public Palette OriginalColours => null!;
			public Palette Palette => null!;
			public Bytemap Bitmap => null!;
			public bool Update(uint gameTick) => false;
			public bool KeyDown(KeyboardEventArgs args) => false;
			public bool MouseDown(ScreenEventArgs args) => false;
			public bool MouseUp(ScreenEventArgs args) => false;
			public bool MouseWheel(ScreenEventArgs args) => false;
			public bool MouseDrag(ScreenEventArgs args) => false;
			public bool MouseMove(ScreenEventArgs args) => false;
			public void Dispose()
			{
			}
		}
	}
}
