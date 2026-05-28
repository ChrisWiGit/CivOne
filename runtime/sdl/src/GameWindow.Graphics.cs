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

		/// <summary>
		/// Cached SDL textures for each layer of the most recent <see cref="Runtime.InvokeDraw"/> result.
		/// Re-uploading large bytemaps to GPU memory on every frame is the dominant cost when the
		/// canvas is at full display resolution (e.g. fullscreen wizard). The cache is invalidated
		/// whenever layer content actually changes; pure cursor moves reuse the existing textures.
		/// </summary>
		private SDL.Texture[] _cachedLayerTextures = null;
		private bool _layerTexturesDirty = true;

		/// <summary>
		/// Marks the cached layer textures as needing a rebuild on the next render.
		/// Called whenever <see cref="Runtime.InvokeDraw"/> has run and may have mutated layer bitmaps.
		/// </summary>
		private void InvalidateLayerTextureCache() => _layerTexturesDirty = true;

		private void DisposeCachedLayerTextures()
		{
			if (_cachedLayerTextures == null) return;
			foreach (SDL.Texture texture in _cachedLayerTextures)
			{
				texture?.Dispose();
			}
			_cachedLayerTextures = null;
		}

		private void RebuildLayerTextureCacheIfNeeded()
		{
			if (!_layerTexturesDirty && _cachedLayerTextures != null
				&& _runtime.Layers != null
				&& _cachedLayerTextures.Length == _runtime.Layers.Length)
			{
				return;
			}

			DisposeCachedLayerTextures();
			if (_runtime.Layers == null)
			{
				_layerTexturesDirty = false;
				return;
			}

			_cachedLayerTextures = new SDL.Texture[_runtime.Layers.Length];
			for (int i = 0; i < _runtime.Layers.Length; i++)
			{
				_cachedLayerTextures[i] = CreateTexture(_runtime.Palette, _runtime.Layers[i]);
			}
			_layerTexturesDirty = false;
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
			if (_runtime.Layers == null) return;
			RebuildLayerTextureCacheIfNeeded();
			if (_cachedLayerTextures == null) return;
			foreach (SDL.Texture canvas in _cachedLayerTextures)
			{
				canvas.Draw(x1, y1, (x2 - x1), (y2 - y1));
			}

			DrawCursorOverlay(x1, y1);
		}

		private Size SetCanvasSize()
		{
			if (RuntimeHandler.IsFullWindowCanvasRequested)
			{
				return new Size(Width, Height);
			}

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