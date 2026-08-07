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
using CivOne.Services.Pathfinding;
using CivOne.Tiles;
using CivOne.Units;

namespace CivOne.Screens
{
	[Modal, ScreenResizeable]
	internal class Goto : BaseScreen
	{
		private const uint UnitBlinkPeriodTicks = 4;
		private const uint UnitBlinkOnTicks = UnitBlinkPeriodTicks / 2;
		private const uint PathBlinkPeriodTicks = 32;
		private const uint PathBlinkOnTicks = PathBlinkPeriodTicks / 2;

		private readonly int _x, _y;
		private readonly GotoDelegate _delegate;

		private bool _update = true;
		private bool? _lastUnitBlinkOn;
		private bool? _lastPathBlinkOn;

		public int X { get; private set; }
		public int Y { get; private set; }

		private static bool IsUnitBlinkOn(uint gameTick)
			=> (gameTick % UnitBlinkPeriodTicks) < UnitBlinkOnTicks;

		private static bool IsPathBlinkOn(uint gameTick)
			=> (gameTick % PathBlinkPeriodTicks) < PathBlinkOnTicks;

		protected override bool HasUpdate(uint gameTick)
		{
			bool unitBlinkOn = IsUnitBlinkOn(gameTick);
			bool pathBlinkOn = IsPathBlinkOn(gameTick);
			if (_update || _lastUnitBlinkOn != unitBlinkOn || _lastPathBlinkOn != pathBlinkOn)
			{
				_lastUnitBlinkOn = unitBlinkOn;
				_lastPathBlinkOn = pathBlinkOn;
				_update = false;
				_delegate.Render(gameTick);
				return true;
			}
			return false;
		}

		public override bool KeyDown(KeyboardEventArgs args)
		{
			if (_delegate.KeyDown(args, out Point? destination, out bool closeScreen))
			{
				if (destination.HasValue)
				{
					X = destination.Value.X;
					Y = destination.Value.Y;
				}

				if (closeScreen)
				{
					Destroy();
				}

				return true;
			}

			Destroy();
			return true;
		}

		protected override void Resize(int width, int height)
		{
			base.Resize(width, height);
			_lastUnitBlinkOn = null;
			_lastPathBlinkOn = null;
			_update = true;
		}

		public override bool MouseDown(ScreenEventArgs args)
		{
			if (_delegate.TrySelect(args, out Point destination))
			{
				X = destination.X;
				Y = destination.Y;
			}

			Destroy();
			return true;
		}

		public override bool MouseMove(ScreenEventArgs args)
		{
			if (_delegate.TryHover(args, out bool changed) && changed)
			{
				_update = true;
				return true;
			}

			return false;
		}

		internal Goto(int x, int y) : base(MouseCursor.Goto)
		{
			_x = x;
			_y = y;
			X = -1;
			Y = -1;
			_delegate = new GotoDelegate(this, _x, _y);

			Palette = CurrentGamePlay.Palette;
		}

		private static GamePlay CurrentGamePlay => Common.Screens.OfType<GamePlay>().First();

		private sealed class GotoDelegate(Goto gotoScreen, int originX, int originY)
		{
			private const int MaxPreviewDistance = 35;
			private const byte PreviewLightColour = 15;
			private const byte PreviewDarkColour = 0;
			private const byte KeyboardCursorLightColour = 15;
			private const byte KeyboardCursorDarkColour = 0;

			private readonly Goto _gotoScreen = gotoScreen;
			private readonly IUnitGotoService _unitGotoService = UnitGotoServiceFactory.Create();
			private int _viewX = originX;
			private int _viewY = originY;
			private bool _keyboardMode;
			private int _cursorX = -1;
			private int _cursorY = -1;
			private Point? _previewTarget;
			private ITile[] _previewPath = [];

			public void Render(uint gameTick)
			{
				GamePlay gamePlay = Goto.CurrentGamePlay;
				gamePlay.SetViewOrigin(_viewX, _viewY);
				gamePlay.Update(gameTick);
				_gotoScreen.Clear(5).AddLayer(gamePlay.Bitmap);
				DrawBlinkingActiveUnit(gameTick, gamePlay);
				DrawPreviewPath(gamePlay, gameTick);
				if (_keyboardMode)
				{
					DrawKeyboardCursor(gameTick, gamePlay);
				}
			}

			public bool KeyDown(KeyboardEventArgs args, out Point? destination, out bool closeScreen)
			{
				destination = null;
				closeScreen = false;

				if (args.Key == Key.Tab)
				{
					ToggleKeyboardMode();
					_gotoScreen._update = true;
					return true;
				}

				if (!_keyboardMode)
				{
					if (TryGetKeyboardDelta(args, out int initialRelX, out int initialRelY))
					{
						ToggleKeyboardMode();
						MoveCursor(initialRelX, initialRelY);
						_gotoScreen._update = true;
						return true;
					}

					return false;
				}

				if (args.Key == Key.Enter)
				{
					_keyboardMode = false;
					destination = Normalize(_cursorX, _cursorY);
					closeScreen = true;
					return true;
				}

				if (args.Key == Key.Escape)
				{
					closeScreen = true;
					return true;
				}

				if (!TryGetKeyboardDelta(args, out int relX, out int relY))
				{
					return true;
				}

				MoveCursor(relX, relY);
				_gotoScreen._update = true;
				return true;
			}

			public bool TrySelect(ScreenEventArgs args, out Point destination)
			{
				if (TrySelectFromCanvas(args.X, args.Y, out destination))
				{
					return true;
				}

				return TrySelectFromMinimap(args.X, args.Y, out destination);
			}

			public bool TryHover(ScreenEventArgs args, out bool changed)
			{
				changed = false;

				if (TrySelectFromCanvas(args.X, args.Y, out Point destination)
					|| TrySelectFromMinimap(args.X, args.Y, out destination))
				{
					changed = UpdatePreviewTarget(destination);
					return true;
				}

				if (ClearPreview())
				{
					changed = true;
					return true;
				}

				return false;
			}

			private bool TrySelectFromCanvas(int x, int y, out Point destination)
			{
				GamePlay gamePlay = Goto.CurrentGamePlay;
				int offsetX = Settings.RightSideBar ? 0 : 80;
				int offsetY = 8;
				int tilePixelSize = Math.Max(1, gamePlay.TilePixelSize);

				int xx = (int)Math.Floor((double)(x - offsetX) / tilePixelSize);
				int yy = (int)Math.Floor((double)(y - offsetY) / tilePixelSize);

				if (xx < 0 || yy < 0 || xx >= gamePlay.VisibleTilesX || yy >= gamePlay.VisibleTilesY)
				{
					destination = default;
					return false;
				}

				ITile? tile = Map[_viewX + xx, _viewY + yy];
				if (tile == null)
				{
					destination = default;
					return false;
				}

				destination = Normalize(tile.X, tile.Y);
				return true;
			}

			private static bool TrySelectFromMinimap(int x, int y, out Point destination)
			{
				int offsetX = Settings.RightSideBar ? 241 : 1;
				int offsetY = 9;

				int xx = x - offsetX;
				int yy = y - offsetY;
				if (xx < 0 || yy < 0)
				{
					destination = default;
					return false;
				}

				ITile? tile = Map[xx, yy];
				if (tile == null)
				{
					destination = default;
					return false;
				}

				destination = Normalize(tile.X, tile.Y);
				return true;
			}

			private static Point Normalize(int x, int y)
			{
				while (x < 0)
				{
					x += Map.WIDTH;
				}

				while (x >= Map.WIDTH)
				{
					x -= Map.WIDTH;
				}

				return new Point(x, y);
			}

			private void ToggleKeyboardMode()
			{
				_keyboardMode = !_keyboardMode;
				if (!_keyboardMode)
				{
					ClearPreview();
					return;
				}

				if (Game.ActiveUnit is IUnit activeUnit)
				{
					_cursorX = activeUnit.X;
					_cursorY = activeUnit.Y;
				}
				else
				{
					_cursorX = _viewX;
					_cursorY = _viewY;
				}

				EnsureCursorVisible();
				UpdatePreviewTarget(Normalize(_cursorX, _cursorY));
			}

			private void MoveCursor(int relX, int relY)
			{
				_cursorX += relX;
				while (_cursorX < 0)
				{
					_cursorX += Map.WIDTH;
				}

				while (_cursorX >= Map.WIDTH)
				{
					_cursorX -= Map.WIDTH;
				}

				_cursorY = Math.Clamp(_cursorY + relY, 0, Map.HEIGHT - 1);
				EnsureCursorVisible();
				if (_keyboardMode)
				{
					UpdatePreviewTarget(Normalize(_cursorX, _cursorY));
				}
			}

			private bool UpdatePreviewTarget(Point destination)
			{
				if (Game.ActiveUnit is not IUnit activeUnit || activeUnit.Moving)
				{
					return ClearPreview();
				}

				Point normalizedDestination = Normalize(destination.X, destination.Y);
				if (_previewTarget.HasValue && _previewTarget.Value == normalizedDestination)
				{
					return false;
				}

				_previewTarget = normalizedDestination;
				if (WrappedDistance(activeUnit.X, activeUnit.Y, normalizedDestination.X, normalizedDestination.Y) > MaxPreviewDistance)
				{
					_previewPath = [];
					return true;
				}

				_previewPath = _unitGotoService.GetPath(activeUnit, normalizedDestination);
				return true;
			}

			private bool ClearPreview()
			{
				if (_previewTarget == null && _previewPath.Length == 0)
				{
					return false;
				}

				_previewTarget = null;
				_previewPath = [];
				return true;
			}

			private static int WrappedDistance(int x1, int y1, int x2, int y2)
			{
				int dx = Math.Abs(x2 - x1);
				dx = Math.Min(dx, Map.WIDTH - dx);
				int dy = Math.Abs(y2 - y1);
				return Math.Max(dx, dy);
			}

			private void DrawPreviewPath(GamePlay gamePlay, uint gameTick)
			{
				if (_previewPath.Length == 0)
				{
					return;
				}

				int mapOffsetX = Settings.RightSideBar ? 0 : 80;
				int mapOffsetY = 8;
				int tileSize = gamePlay.TilePixelSize;
				int markerSize = Math.Max(2, tileSize / 4);
				int inset = Math.Max(0, (tileSize - markerSize) / 2);
				bool blinkOn = IsPathBlinkOn(gameTick);

				for (int i = 0; i < _previewPath.Length; i++)
				{
					ITile tile = _previewPath[i];
					int localX = tile.X - gamePlay.X;
					if (localX < 0)
					{
						localX += Map.WIDTH;
					}

					int localY = tile.Y - gamePlay.Y;
					if (localX < 0 || localY < 0 || localX >= gamePlay.VisibleTilesX || localY >= gamePlay.VisibleTilesY)
					{
						continue;
					}

					int left = mapOffsetX + (localX * tileSize) + inset;
					int top = mapOffsetY + (localY * tileSize) + inset;
					bool isTargetMarker = i == _previewPath.Length - 1;
					byte colour = isTargetMarker
						? (blinkOn ? PreviewDarkColour : PreviewLightColour)
						: (blinkOn ? PreviewLightColour : PreviewDarkColour);
					_gotoScreen.DrawRectangle(left, top, markerSize, markerSize, colour);
				}
			}

			private void EnsureCursorVisible()
			{
				GamePlay gamePlay = Goto.CurrentGamePlay;
				int tilesX = Math.Max(1, gamePlay.VisibleTilesX);
				int tilesY = Math.Max(1, gamePlay.VisibleTilesY);

				int relX = _cursorX - _viewX;
				if (relX < 0)
				{
					_viewX = _cursorX;
				}
				else if (relX >= tilesX)
				{
					_viewX = _cursorX - tilesX + 1;
				}

				while (_viewX < 0)
				{
					_viewX += Map.WIDTH;
				}

				while (_viewX >= Map.WIDTH)
				{
					_viewX -= Map.WIDTH;
				}

				int relY = _cursorY - _viewY;
				if (relY < 0)
				{
					_viewY = _cursorY;
				}
				else if (relY >= tilesY)
				{
					_viewY = _cursorY - tilesY + 1;
				}

				_viewY = Math.Clamp(_viewY, 0, Math.Max(0, Map.HEIGHT - tilesY));
			}

			private void DrawKeyboardCursor(uint gameTick, GamePlay gamePlay)
			{
				if (_cursorX < 0 || _cursorY < 0)
				{
					return;
				}

				int relX = _cursorX - _viewX;
				if (relX < 0)
				{
					relX += Map.WIDTH;
				}

				int relY = _cursorY - _viewY;
				if (relX < 0 || relY < 0 || relX >= gamePlay.VisibleTilesX || relY >= gamePlay.VisibleTilesY)
				{
					return;
				}

				int mapOffsetX = Settings.RightSideBar ? 0 : 80;
				int mapOffsetY = 8;
				int left = mapOffsetX + (relX * gamePlay.TilePixelSize);
				int top = mapOffsetY + (relY * gamePlay.TilePixelSize);
				byte colour = IsUnitBlinkOn(gameTick) ? KeyboardCursorLightColour : KeyboardCursorDarkColour;
				_gotoScreen.DrawRectangle(left, top, gamePlay.TilePixelSize, gamePlay.TilePixelSize, colour);
			}

			private static bool TryGetKeyboardDelta(KeyboardEventArgs args, out int relX, out int relY)
			{
				relX = 0;
				relY = 0;

				switch (args.Key)
				{
					case Key.Left:
					case Key.NumPad4:
						relX = -1;
						return true;
					case Key.Right:
					case Key.NumPad6:
						relX = 1;
						return true;
					case Key.Up:
					case Key.NumPad8:
						relY = -1;
						return true;
					case Key.Down:
					case Key.NumPad2:
						relY = 1;
						return true;
					case Key.Home:
					case Key.NumPad7:
						relX = -1;
						relY = -1;
						return true;
					case Key.PageUp:
					case Key.NumPad9:
						relX = 1;
						relY = -1;
						return true;
					case Key.End:
					case Key.NumPad1:
						relX = -1;
						relY = 1;
						return true;
					case Key.PageDown:
					case Key.NumPad3:
						relX = 1;
						relY = 1;
						return true;
					default:
						return false;
				}
			}

			private void DrawBlinkingActiveUnit(uint gameTick, GamePlay gamePlay)
			{
				if (Game.ActiveUnit is not IUnit activeUnit || activeUnit.Moving)
				{
					return;
				}

				ITile tile = activeUnit.Tile;
				int localX = tile.X - gamePlay.X;
				if (localX < 0)
				{
					localX += Map.WIDTH;
				}

				if (localX < 0 || localX >= gamePlay.VisibleTilesX)
				{
					return;
				}

				int localY = tile.Y - gamePlay.Y;
				if (localY < 0 || localY >= gamePlay.VisibleTilesY)
				{
					return;
				}

				bool blinkOn = IsUnitBlinkOn(gameTick);
				TileSettings blinkState = blinkOn ? TileSettings.BlinkOn : TileSettings.BlinkOff;
				int mapOffsetX = Settings.RightSideBar ? 0 : 80;
				int mapOffsetY = 8;
				using IBitmap activeTile = tile.ToBitmap(blinkState, pixelSize: gamePlay.TilePixelSize);
				_gotoScreen.AddLayer(activeTile.Bitmap, mapOffsetX + (localX * gamePlay.TilePixelSize), mapOffsetY + (localY * gamePlay.TilePixelSize));
			}
		}
	}
}