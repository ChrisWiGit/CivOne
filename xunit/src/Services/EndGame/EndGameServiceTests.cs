using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.IO;
using CivOne.Persistence.Game;
using CivOne.Screens;
using CivOne.Services.EndGame;
using CivOne.Services.Screen;
using Xunit;

namespace CivOne.UnitTests.Services.EndGame
{
	public class EndGameServiceTests
	{
		private readonly List<string> _shown = [];
		private bool _gameEnded;

		[Fact]
		public async Task RetireShowsPowerGraphScoreFamousPersonsAndHallOfFame()
		{
			await CreateService().HandleRetireAsync();

			Assert.Equal(["PowerGraph", "CivilizationScore", "FamousPersons", "HallOfFame", "Credits"], _shown);
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

			Assert.Equal(["Victory", "PowerGraph", "CivilizationScore", "FamousPersons", "HallOfFame", "Credits"], _shown);
		}

		[Fact]
		public async Task AlphaCentauriShowsSpaceVictoryThenFullScore()
		{
			await CreateService().HandleAlphaCentauriAsync();

			Assert.Equal(["SpaceVictory", "PowerGraph", "CivilizationScore", "FamousPersons", "HallOfFame", "Credits"], _shown);
		}

		[Fact]
		public async Task RivalSpaceArrivalShowsFullScoreIncludingHallOfFame()
		{
			await CreateService().HandleRivalSpaceArrivalAsync();

			Assert.Equal(["PowerGraph", "CivilizationScore", "FamousPersons", "HallOfFame", "Credits"], _shown);
		}

		private EndGameService CreateService()
		{
			ClosingScreenService screens = new(_shown);
			return new EndGameService(screens, screens, new FakeGameService(() => _gameEnded = true), null!, new NamedScreenFactory());
		}

		private sealed class NamedScreenFactory : IEndGameScreenFactory
		{
			public IScreen Victory() => new NamedScreen("Victory");
			public IScreen Defeat() => new NamedScreen("Defeat");
			public IScreen SpaceVictory(IPlayer winner) => new NamedScreen("SpaceVictory");
			public IScreen PowerGraph() => new NamedScreen("PowerGraph");
			public IScreen CivilizationScore() => new NamedScreen("CivilizationScore");
			public IScreen FamousPersons() => new NamedScreen("FamousPersons");
			public IScreen HallOfFame() => new NamedScreen("HallOfFame");
			public IScreen Credits() => new NamedScreen("Credits");
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

		private sealed class NamedScreen(string name) : IScreen
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
