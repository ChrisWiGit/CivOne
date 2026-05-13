// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Drawing;
using System.Linq;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.IO;
using CivOne.Screens.SpaceShipAssets;
using CivOne.Services;
using CivOne.Services.Random;
using CivOne.Services.SpaceShip;
using CivOne.Tasks;

namespace CivOne.Screens
{
	public interface ISpaceShipResourceService : IResourceFileBitmapProvider, IResourceFontHeightProvider
	{
	}

	[ScreenResizeable]
	internal class SpaceShipView : BaseScreen
	{
		private readonly ISpaceShipResourceService _resources;
		private readonly IGameCalendarService _calendarService;

		// only hard coupling but easy to refactor if needed.
		private readonly static byte[] ColourDark = Common.ColourDark;
		private readonly Player _player;
		private readonly bool _debug;
		private bool _debugLaunch = false;
		private readonly ISpaceShipService _service;
		private readonly ISpaceShipSpriteProvider _sprites;
		private readonly ISpaceShipSlotBlueprint _slotBlueprint;
		private SpaceShipScreenData _data;
		private bool _update = true;

		private readonly StarfieldDelegate _starfield;
		private readonly SpaceShipPaletteAnimationDelegate _paletteAnimation;

		private readonly byte StartFieldBackgroundColorIndex = 96;
		private const int TitleBarTop = 1;

		private const int LeftBottomMargin = 1;

		private static int StarfieldWidth => SidePanelLeft - LeftBottomMargin;

		private const int SidePanelLeft = 236;
		private const int GridCellSize = 16;
		private const int GridCols = 12;
		private const int GridRows = 12;
		private const int GridLeft = 22 + 16;
		private const int GridTop = 8;

		private const int LaunchButtonX = 236;
		private const int LaunchButtonY = 176;
		private const int LaunchButtonWidth = 72;
		private const int LaunchButtonHeight = 13;
		private const int LaunchButtonRenderYOffset = -2;
		private const int LaunchButtonRenderHeightOffset = -2;

		private const int TitleFontId = 0;

		// Starfield parallax speed array - one divisor per layer (0=front to 3=back)
		// Smaller divisor = faster scrolling. Back layers scroll faster for depth effect.
		private static readonly int[] StarfieldScrollDividers = [128, 24, 8, 1];

		private int OffsetX => Math.Max(0, (Width - 320) / 2);
		private int OffsetY => Math.Max(0, (Height - 200) / 2);

		private int ScreenWidth => Math.Min(320, Width);
		private int ScreenHeight => Math.Min(200, Height);
		private int SidePanelOffsetX => OffsetX + 1;
		private int SidePanelOffsetY => OffsetY + _resources.GetFontHeight(TitleFontId);
		private int LaunchButtonLeft => SidePanelOffsetX + LaunchButtonX;
		private int LaunchButtonTop => SidePanelOffsetY + LaunchButtonY + LaunchButtonRenderYOffset;
		private static int LaunchButtonRenderHeight => LaunchButtonHeight + LaunchButtonRenderHeightOffset;

		private byte PlayerColor
		{
			get
			{
				int playerNumber = Game.PlayerNumber(_player);
				if (playerNumber < 0)
				{
					playerNumber = 0;
				}
				if (playerNumber >= ColourDark.Length)
				{
					playerNumber = ColourDark.Length - 1;
				}
				return ColourDark[playerNumber];
			}
		}

		private void RefreshData()
		{
			_data = _service.GetScreenData();
			_update = true;
		}

		/// <summary>
		/// Draws text with automatic translation support.
		/// Translates the text key using <see cref="Translation"/> before rendering.
		/// Supports method chaining like <see cref="BitmapExtensions.DrawText"/>.
		/// </summary>
		private IBitmap DrawLocalizedText(string textKey, int font, byte colour, int x, int y, TextAlign align = TextAlign.Left)
		{
			string displayText = Translation.Translate(textKey);
			return this.DrawText(displayText, font, colour, x, y, align);
		}

		protected bool HasLaunched => _player.SpaceShipLaunchYear != 0 || _debugLaunch;

		protected override bool HasUpdate(uint gameTick)
		{
			int[] parallaxOffsets = HasLaunched ? _starfield.GetParallaxOffsets(gameTick, StarfieldWidth) : StarfieldDelegate.ZeroOffsets;
			bool needsScroll = _starfield.HasParallaxMoved(parallaxOffsets);
			bool paletteChanged = _paletteAnimation.Update(gameTick);

			if (!_update && !RefreshNeeded() && !needsScroll && !paletteChanged)
			{
				return false;
			}

			_starfield.CommitParallaxOffsets(parallaxOffsets);
			_update = false;

			if (paletteChanged)
			{
				Palette = SpaceShipPalette;
			}

			int fontHeight = _resources.GetFontHeight(TitleFontId);
			int sceneTop = fontHeight;

			int ox = OffsetX; int oy = OffsetY;

			DrawBackground();
			using IBitmap shipOverlay = new Picture(ScreenWidth, ScreenHeight, SpaceShipPalette);
			DrawStarsBackground(shipOverlay, sceneTop, parallaxOffsets);
			DrawShip(shipOverlay);

			// Use shipOverlay indices directly with animated palette
			this.AddLayer(shipOverlay.Bitmap, ox, oy);
			DrawSidePanel(SidePanelOffsetX, SidePanelOffsetY);

			return true;
		}

		private void DrawBackground()
		{
			this.Clear(PlayerColor);

			this.DrawText($"{_player.TribeName} SpaceShip: R.S.S. Caesar", TitleFontId, 5, 160, TitleBarTop, TextAlign.Center);
		}

		private void DrawSidePanel(int ox, int oy)
		{
			const byte textColour = 15;

			this.FillRectangle(ox + SidePanelLeft, oy, ScreenWidth - SidePanelLeft, ScreenHeight, PlayerColor);

			DrawLocalizedText("Population:", 0, textColour, ox + SidePanelLeft, oy + 18, TextAlign.Left)
			.DrawText($"{_data.Population:N0}", 0, textColour, ox + SidePanelLeft, oy + 26, TextAlign.Left);
			DrawLocalizedText("Support:", 0, textColour, ox + SidePanelLeft, oy + 38, TextAlign.Left)
			.DrawText($"{_data.SupportPercent}%", 0, textColour, ox + SidePanelLeft, oy + 46, TextAlign.Left);
			DrawLocalizedText("Energy:", 0, textColour, ox + SidePanelLeft, oy + 56, TextAlign.Left)
			.DrawText($"{_data.EnergyPercent}%", 0, textColour, ox + SidePanelLeft, oy + 64, TextAlign.Left);
			DrawLocalizedText("Mass:", 0, textColour, ox + SidePanelLeft, oy + 76, TextAlign.Left)
			.DrawText($"{_data.MassTons:N0}t", 0, textColour, ox + SidePanelLeft, oy + 84, TextAlign.Left);
			DrawLocalizedText("Fuel:", 0, textColour, ox + SidePanelLeft, oy + 94, TextAlign.Left)
			.DrawText($"{_data.FuelPercent}%", 0, textColour, ox + SidePanelLeft, oy + 102, TextAlign.Left);
			DrawLocalizedText("Flight Time:", 0, textColour, ox + SidePanelLeft, oy + 112, TextAlign.Left)
			.DrawText($"{_data.FlightTimeYears:0.0}y", 0, textColour, ox + SidePanelLeft, oy + 120, TextAlign.Left);
			DrawLocalizedText("Success:", 0, textColour, ox + SidePanelLeft, oy + 130, TextAlign.Left)
			.DrawText($"{_data.SuccessProbabilityPercent}%", 0, textColour, ox + SidePanelLeft, oy + 138, TextAlign.Left);

			if (!HasLaunched)
			{
				DrawLocalizedText("Can Launch:", 0, textColour, ox + 236, oy + 148, TextAlign.Left)
				.DrawText(_data.CanLaunch ? "YES" : "NO", 0, _data.CanLaunch ? (byte)2 : (byte)4, ox + 236, oy + 156, TextAlign.Left);

				if (_data.CanLaunch)
				{
					DrawButton("LAUNCH", 0, 23, 5, LaunchButtonLeft, LaunchButtonTop, LaunchButtonWidth, LaunchButtonRenderHeight);
				}
			}
			else
			{
				DrawLocalizedText("Launched:", 0, textColour, ox + 236, oy + 148, TextAlign.Left)
				.DrawText($"{_player.SpaceShipLaunchYear} AD", 0, textColour, ox + 236, oy + 156, TextAlign.Left);
				DrawLocalizedText("Est. Arrival:", 0, textColour, ox + 236, oy + 166, TextAlign.Left)
				.DrawText($"{_player.SpaceShipLaunchYear + (int)Math.Ceiling(_data.FlightTimeYears)} AD", 0, textColour, ox + 236, oy + 174, TextAlign.Left);

			}

			this.DrawText($"S:{_data.StructuralCount} C:{_data.ComponentCount} M:{_data.ModuleCount}", 0, 15, ox + 16, oy + 176, TextAlign.Left);
			if (_debug)
			{
				DrawLocalizedText("SpaceBackgroundColorId", 0, 15, ox + 16, oy + 184, TextAlign.Left)
				.DrawText($"{_debugSpaceBackgroundColorId}", 0, 15, ox + 184, oy + 184, TextAlign.Left);
			}
		}

		private byte _debugSpaceBackgroundColorId = 0;

		private void DrawStarsBackground(IBitmap target, int oy, int[] parallaxOffsets)
		{
			int starFieldHeight = target.Height() - oy - LeftBottomMargin;
			target.FillRectangle(LeftBottomMargin, oy, StarfieldWidth, starFieldHeight, StartFieldBackgroundColorIndex);
			_starfield.DrawStarfield(target, LeftBottomMargin, oy, StarfieldWidth, starFieldHeight, parallaxOffsets);
		}

		private static bool IsLargeSprite(SpaceShipComponentType type) =>
			type is SpaceShipComponentType.CommandModule
			or SpaceShipComponentType.SolarPanelModule
			or SpaceShipComponentType.HabitationModule
			or SpaceShipComponentType.LifeSupportModule;

		private static bool IsOriginCell(SpaceShipComponentType[,] grid, int col, int row)
		{
			SpaceShipComponentType cell = grid[col, row];
			if (col > 0 && grid[col - 1, row] == cell) return false;
			if (row > 0 && grid[col, row - 1] == cell) return false;
			return true;
		}

		private void DrawShip(IBitmap target)
		{
			SpaceShipComponentType[,] grid = _player.SpaceShipGrid;
			int left = GridLeft;
			int top = GridTop;
			SpaceShipOverlaySprite[] visibleOverlays = BuildVisibleOverlays(grid);

			for (int row = 0; row < GridRows; row++)
			{
				for (int col = 0; col < GridCols; col++)
				{
					DrawShipCell(target, grid, left, top, col, row, visibleOverlays);
				}
			}

			DrawOverlaySprites(target, left, top, visibleOverlays);
		}

		private SpaceShipOverlaySprite[] BuildVisibleOverlays(SpaceShipComponentType[,] grid)
		{
			SpaceShipPartCounts counts = SpaceShipPartCounter.Count(grid);
			bool showCommandModule = (counts.LifeSupportModule + counts.HabitationModule) >= 3;

			return [.. _slotBlueprint.OverlaySprites.Select(sprite =>
				sprite.SpriteId == SpaceShipOverlaySpriteIds.CommandModule
					? sprite.WithVisibility(showCommandModule)
					: sprite)];
		}

		private void DrawOverlaySprites(IBitmap target, int left, int top, SpaceShipOverlaySprite[] visibleOverlays)
		{
			foreach (SpaceShipOverlaySprite overlay in visibleOverlays.Where(sprite => sprite.IsVisible()).OrderBy(sprite => sprite.ZIndex))
			{
				int cx = left + overlay.PixelX(GridCellSize);
				int cy = top + overlay.PixelY(GridCellSize);

				if (_sprites.TryGetPartSprite(overlay.SpriteType, out Picture sprite))
				{
					target.AddLayer(sprite, cx, cy);
				}
			}
		}



		private void DrawShipCell(IBitmap target, SpaceShipComponentType[,] grid, int left, int top, int col, int row, SpaceShipOverlaySprite[] visibleOverlays)
		{
			SpaceShipComponentType cell = grid[col, row];
			if (cell == SpaceShipComponentType.Empty) return;
			if (visibleOverlays.Any(sprite => sprite.IsVisible() && sprite.SpriteType == cell)) return;
			if (IsLargeSprite(cell) && !IsOriginCell(grid, col, row)) return;

			int cx = left + col * GridCellSize;
			int cy = top + row * GridCellSize;

			if (_sprites.TryGetPartSprite(cell, out Picture sprite))
			{
				target.AddLayer(sprite, cx, cy);
			}
		}

		public override bool KeyDown(KeyboardEventArgs args)
		{
			if (args[Key.Escape])
			{
				Destroy();
				return true;
			}

			if (!_debug)
			{
				return true;
			}

			return HandleDebugKey(args);
		}

		private bool HandleDebugKey(KeyboardEventArgs args)
		{
			if (IsCharacterKey(args, 'l'))
			{
				_debugLaunch = true;
				RefreshData();
				return true;
			}

			if (args[Key.F1])
			{
				if (args.Shift)
				{
					_debugSpaceBackgroundColorId = (byte)((_debugSpaceBackgroundColorId - 1) % 255);
				}
				else
				{
					_debugSpaceBackgroundColorId = (byte)((_debugSpaceBackgroundColorId + 1) % 255);
				}
				RefreshData();
				return true;
			}

			if (args[Key.NumPad1] || IsCharacterKey(args, '1'))
			{
				_service.TryAddPart(SpaceShipComponentType.Structural);
				RefreshData();
				return true;
			}

			if (args[Key.NumPad2] || IsCharacterKey(args, '2'))
			{
				_service.TryAddPart(SpaceShipComponentType.Component);
				RefreshData();
				return true;
			}

			if (args[Key.NumPad3] || IsCharacterKey(args, '3'))
			{
				_service.TryAddPart(SpaceShipComponentType.Module);
				RefreshData();
				return true;
			}

			if (IsCharacterKey(args, 'v'))
			{
				GameTask.Enqueue(Show.Screen(new SpaceVictory(_player)));
				return true;
			}

			return true;
		}

		private static bool IsCharacterKey(KeyboardEventArgs args, char key) => args[Key.Character] && (args.KeyChar == key || args.KeyChar == char.ToUpperInvariant(key));

		public override bool MouseDown(ScreenEventArgs args)
		{
			bool launchHit = args.X >= LaunchButtonLeft
				&& args.X < (LaunchButtonLeft + LaunchButtonWidth)
				&& args.Y >= LaunchButtonTop
				&& args.Y < (LaunchButtonTop + LaunchButtonRenderHeight);

			if (launchHit)
			{
				if (_player.SpaceShipLaunchYear != 0)
				{
					GameTask.Enqueue(Message.Advisor(Advisor.Science, true, T("Space ship"), T("already launched.")));
					return true;
				}

				if (!_service.CanLaunch())
				{
					GameTask.Enqueue(Message.Advisor(Advisor.Science, true, T("Space ship"), T("is not ready for launch.")));
					return true;
				}

				_player.SpaceShipLaunchYear = (short)_calendarService.TurnToYear(Game.GameTurn);
				RefreshData();
				GameTask.Enqueue(Message.Newspaper(null, $"{_player.TribeName} {T("space ship")}", T("launches for"), T("Alpha Centauri!")));
				return true;
			}

			if ((args.Buttons & MouseButton.Right) > 0)
			{
				Destroy();
			}

			return true;
		}

		private string T(string key) => Translation.Translate(key);

		private readonly Palette SpaceShipPalette;

		public SpaceShipView(Player player = null, bool debug = false,
				ISpaceShipServiceFactory spaceShipServiceFactory = null,
				ISpaceShipSpriteProvider spaceShipSpriteProvider = null,
				ISpaceShipSlotBlueprint slotBlueprint = null,
				ISpaceShipResourceService resources = null,
				IGameCalendarService calendarService = null,
				IRandomService randomService = null) : base(MouseCursor.Pointer)
		{
			_player = player ?? Human;
			_debug = debug;

			IRandomService effectiveRandomService = randomService ?? RandomServiceFactory.Create();
			_starfield = new StarfieldDelegate(effectiveRandomService, StarfieldScrollDividers);
			_slotBlueprint = slotBlueprint ?? SpaceShipSlotBlueprintFactoryProvider.GetInstance().Create();

			ISpaceShipServiceFactory serviceFactory = spaceShipServiceFactory ?? SpaceShipServiceFactoryProvider.GetInstance();

			_resources = resources ?? new SpaceShipResourceServiceAdapter(Resources.Instance, Resources.Instance);


			_calendarService = calendarService ?? new GameCalendarService(Translation);

			_service = serviceFactory.Create(_player);
			_sprites = spaceShipSpriteProvider ?? SpaceShipSpriteProviderFactory.GetInstance();
			_data = _service.GetScreenData();

			SpaceShipPalette = _resources["DOCKER"].Palette.Copy();
			SpaceShipPalette[StartFieldBackgroundColorIndex] = new Colour(0, 0, 48);
			_paletteAnimation = new SpaceShipPaletteAnimationDelegate(SpaceShipPalette);

			Palette = SpaceShipPalette;

		}

		public void LoadPalette()
		{
			Palette = SpaceShipPalette;
			Refresh();
		}

		internal class SpaceShipResourceServiceAdapter(IResourceFileBitmapProvider bitmapProvider, IResourceFontHeightProvider fontHeightProvider) : ISpaceShipResourceService
		{
			private readonly IResourceFileBitmapProvider _bitmapProvider = bitmapProvider;
			private readonly IResourceFontHeightProvider _fontHeightProvider = fontHeightProvider;

			public IBitmap this[string filename] => _bitmapProvider[filename];

			public bool Exists(string filename)
			{
				return _bitmapProvider.Exists(filename);
			}

			public int GetFontHeight(int FontId) => _fontHeightProvider.GetFontHeight(FontId);
		}
	}
}
