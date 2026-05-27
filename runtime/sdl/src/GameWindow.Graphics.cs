// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Drawing;
using System.Linq;
using CivOne.Enums;
using CivOne.IO;

namespace CivOne
{
	internal partial class GameWindow
	{
		private SDL.Texture CursorTexture = null;

		// Set from any thread when the cursor changes; consumed on the render thread in Draw().
		private volatile bool _cursorDirty;

		// Persistent per-layer streaming-texture cache. Reuses GPU textures + managed
		// pixel/palette buffers across frames; was previously recreated 60 FPS × layer-count
		// times per second (see P0.6 in code.review.md).
		private SDL.Texture[] _layerTextures;

		/// <summary>
		/// Disposes the layer texture cache. 
		/// Should be called whenever the layer count changes (e.g. new game, load game) to avoid keeping around stale textures.
		/// Also called from Dispose() to clean up GPU resources on shutdown.
		/// </summary>
		private void DisposeLayerTextureCache()
		{
			if (_layerTextures == null) return;
			for (int i = 0; i < _layerTextures.Length; i++)
			{
				_layerTextures[i]?.Dispose();
				_layerTextures[i] = null;
			}
			_layerTextures = null;
		}

		private static Size DefaultCanvasSize
		{
			get
			{
				if (Settings.AspectRatio != AspectRatio.Expand)
					return new Size(320, 200);
				if (Settings.ExpandWidth > 0 && Settings.ExpandHeight > 0)
					return new Size(Settings.ExpandWidth, Settings.ExpandHeight);
				return new Size(320, 200);
			}
		}

		/// <summary>
		/// Draws the cursor overlay on top of the rendered layers, if a cursor is set.
		///
		/// Accounts for the current aspect ratio mode and scale settings to position and size the cursor correctly.
		/// Both <see cref="AspectRatio.Scaled"/> and <see cref="AspectRatio.ScaledFixed"/> use the same
		/// scaled-mouse calculation via <c>GetScaleF()</c>.
		///
		/// Refactoring note: <c>ScaledFixed</c> previously had a separate branch with commented-out offset
		/// additions (<c>_mouseX + x1</c>, <c>_mouseY + y1</c>) that were never active.
		/// After confirming both branches produced identical results, they were merged into one case.
		/// </summary>
		/// <param name="x1">The x-coordinate of the top-left corner of the drawing area.</param>
		/// <param name="y1">The y-coordinate of the top-left corner of the drawing area.</param>
		private void DrawCursorOverlay(int x1, int y1)
		{
			if (CursorTexture == null) return;
			switch (Settings.AspectRatio)
			{
				case AspectRatio.Scaled:
				case AspectRatio.ScaledFixed:
					{
						PointF scaleF = GetScaleF();
						CursorTexture.Draw((int)(_mouseX * scaleF.X), (int)(_mouseY * scaleF.Y), (int)(CursorTexture.Width * scaleF.X), (int)(CursorTexture.Height * scaleF.Y));
					}
					break;
				default:
					CursorTexture.Draw(x1 + (_mouseX * ScaleX), y1 + (_mouseY * ScaleY), CursorTexture.Width * ScaleX, CursorTexture.Height * ScaleY);
					break;
			}
		}

		private void Render()
		{
			switch(Settings.AspectRatio)
			{
				case AspectRatio.Scaled:
				case AspectRatio.ScaledFixed:
					if (!PixelScale) PixelScale = true;
					break;
				default:
					if (PixelScale) PixelScale = false;
					break;
			}

			Clear(Color.Black);
			GetBorders(out int x1, out int y1, out int x2, out int y2);

			// Snapshot the layer reference: Runtime.Layers may be reassigned from another thread
			// (MCP / game loop), and iterating the array directly without a snapshot risks a
			// torn read between the null-check and the for-loop.
			Bytemap[] layers = _runtime.Layers;
			if (layers == null) return;

			if (_layerTextures == null || _layerTextures.Length != layers.Length)
			{
				DisposeLayerTextureCache();
				_layerTextures = new SDL.Texture[layers.Length];
			}

			for (int i = 0; i < layers.Length; i++)
			{
				Bytemap bytemap = layers[i];
				if (bytemap == null) continue;

				SDL.Texture cached = _layerTextures[i];
				if (cached == null || cached.IsEmpty || cached.Width != bytemap.Width || cached.Height != bytemap.Height)
				{
					cached?.Dispose();
					cached = CreateLayerTexture(bytemap.Width, bytemap.Height);
					_layerTextures[i] = cached;
				}

				cached.UpdateFrom(_runtime.Palette, bytemap);
				cached.Draw(x1, y1, (x2 - x1), (y2 - y1));
			}

			DrawCursorOverlay(x1, y1);
		}

		private Size SetCanvasSize()
		{
			if (Settings.AspectRatio != AspectRatio.Expand)
			{
				return DefaultCanvasSize;
			}

			int cw = ClientRectangle.Width, ch = ClientRectangle.Height;
			int scale = new int[] { (cw - (cw % 320)) / 320, (ch - (ch % 200)) / 200 }.Min();

			bool hasExplicitExpandSize = Settings.ExpandWidth > 0 && Settings.ExpandHeight > 0;
			if (hasExplicitExpandSize)
			{
				cw = Settings.ExpandWidth;
				ch = Settings.ExpandHeight;
			}
			else
			{
				if (scale < 1) scale = 1;
				cw /= scale;
				ch /= scale;
			}

			// Make sure the canvas resolution is a multiple of 8
			cw -= (cw % 8);
			ch -= (ch % 8);

			// CW: Keep auto-expand conservative for stability, but allow larger explicit user values.
			// Original: https://github.com/Solen1985/CivOne/wiki/Settings#expand-experimental
			int maxWidth  = hasExplicitExpandSize ? Settings.MaxExpandWidth  : Settings.AutoExpandMaxWidth;
			int maxHeight = hasExplicitExpandSize ? Settings.MaxExpandHeight : Settings.AutoExpandMaxHeight;
			if (cw > maxWidth) cw = maxWidth;
			if (ch > maxHeight) ch = maxHeight;

			return new Size(cw, ch);
		}

		private static int InitialCanvasWidth => 320;
		private static int InitialCanvasHeight => 200;

		private static int InitialWidth => Settings.WindowWidth > 0 ? Settings.WindowWidth : InitialCanvasWidth * Settings.Scale;
		private static int InitialHeight => Settings.WindowHeight > 0 ? Settings.WindowHeight : InitialCanvasHeight * Settings.Scale;

		private Size ClientRectangle => new Size(Width, Height);

		private void ResetWindowScale()
		{
			SetWindowSize(InitialWidth, InitialHeight);
		}
		
		private int ScaleX
		{
			get
			{
				int cw = CanvasWidth, ch = CanvasHeight;
				if (cw == 0) cw = DefaultCanvasSize.Width;
				if (ch == 0) ch = DefaultCanvasSize.Height;

				switch (Settings.AspectRatio)
				{
					case AspectRatio.Fixed:
					case AspectRatio.ScaledFixed:
					case AspectRatio.Expand:
						int scaleX = (ClientRectangle.Width - (ClientRectangle.Width % cw)) / cw;
						int scaleY = (ClientRectangle.Height - (ClientRectangle.Height % ch)) / ch;
						if (scaleX > scaleY)
								return scaleY < 1 ? 1 : scaleY;
							return scaleX < 1 ? 1 : scaleX;
					default:
						return (ClientRectangle.Width - (ClientRectangle.Width % cw)) / cw;
				}
			}
		}

		private int ScaleY
		{
			get
			{
				int cw = CanvasWidth, ch = CanvasHeight;
				if (cw == 0) cw = DefaultCanvasSize.Width;
				if (ch == 0) ch = DefaultCanvasSize.Height;

				switch (Settings.Instance.AspectRatio)
				{
					case AspectRatio.Fixed:
					case AspectRatio.ScaledFixed:
					case AspectRatio.Expand:
						int scaleX = (ClientRectangle.Width - (ClientRectangle.Width % cw)) / cw;
						int scaleY = (ClientRectangle.Height - (ClientRectangle.Height % ch)) / ch;
						if (scaleY > scaleX)
								return scaleX < 1 ? 1 : scaleX;
							return scaleY < 1 ? 1 : scaleY;
					default:
						return (ClientRectangle.Height - (ClientRectangle.Height % ch)) / ch;
				}
			}
		}

		private int CanvasWidth => Runtime.CanvasSize.Width;
		private int CanvasHeight => Runtime.CanvasSize.Height;

		private int DrawWidth => CanvasWidth * ScaleX;
		private int DrawHeight => CanvasHeight * ScaleY;

		private void GetBorders(out int x1, out int y1, out int x2, out int y2)
		{
			x1 = (ClientRectangle.Width - DrawWidth) / 2;
			y1 = (ClientRectangle.Height - DrawHeight) / 2;
			x2 = x1 + DrawWidth;
			y2 = y1 + DrawHeight;

			switch (Settings.AspectRatio)
			{
				case AspectRatio.Scaled:
					x1 = 0;
					y1 = 0;
					x2 = ClientRectangle.Width;
					y2 = ClientRectangle.Height;
					break;
				case AspectRatio.ScaledFixed:
					float scaleX = (float)ClientRectangle.Width / CanvasWidth;
					float scaleY = (float)ClientRectangle.Height / CanvasHeight;
					if (scaleX > scaleY) scaleX = scaleY;
					else if (scaleY > scaleX) scaleY = scaleX;

					int drawWidth = (int)((float)CanvasWidth * scaleX);
					int drawHeight = (int)((float)CanvasHeight * scaleY);

					x1 = (ClientRectangle.Width - drawWidth) / 2;
					y1 = (ClientRectangle.Height - drawHeight) / 2;
					x2 = x1 + drawWidth;
					y2 = y1 + drawHeight;
					break;
			}
		}
	}
}