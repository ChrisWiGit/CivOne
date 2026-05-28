// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Diagnostics;
using CivOne.Enums;
using CivOne.IO;

namespace CivOne
{
	internal sealed class FpsOverlayDrawDelegate : IDisposable
	{
		private readonly Stopwatch _fpsWatch = Stopwatch.StartNew();
		private int _fpsFrameCount;
		private int _currentFps;
		private Bytemap? _fpsOverlay;

		internal void Draw(FpsCorner fpsCorner, int canvasWidth, int canvasHeight, Bytemap[] runtimeLayers)
		{
			if (fpsCorner == FpsCorner.Off)
			{
				ReleaseOverlay();
				RuntimeHandler.Runtime.Layers = runtimeLayers;
				return;
			}

			_fpsFrameCount++;
			if (_fpsWatch.ElapsedMilliseconds >= 1000)
			{
				_currentFps = _fpsFrameCount;
				_fpsFrameCount = 0;
				_fpsWatch.Restart();
			}

			if (_fpsOverlay == null || _fpsOverlay.Width != canvasWidth || _fpsOverlay.Height != canvasHeight)
			{
				ReleaseOverlay();
				_fpsOverlay = new Bytemap(canvasWidth, canvasHeight);
			}
			else
			{
				_fpsOverlay.Clear();
			}

			(int x, int y) = GetCornerPosition(fpsCorner, canvasWidth, canvasHeight);
			using (var fpsText = CivOne.Graphics.Resources.Instance.GetText($"{_currentFps} FPS", 1, 15))
			{
				_fpsOverlay.AddLayer(fpsText.Bitmap, x, y);
			}
			RuntimeHandler.Runtime.Layers = [ ..runtimeLayers, _fpsOverlay ];
		}

		private static (int X, int Y) GetCornerPosition(FpsCorner fpsCorner, int canvasWidth, int canvasHeight)
		{
			return fpsCorner switch
			{
				FpsCorner.TopRight => (canvasWidth - 34, 2),
				FpsCorner.BottomLeft => (2, canvasHeight - 10),
				FpsCorner.BottomRight => (canvasWidth - 34, canvasHeight - 10),
				_ => (2, 2),
			};
		}

		private void ReleaseOverlay()
		{
			_fpsOverlay?.Dispose();
			_fpsOverlay = null;
		}

		public void Dispose()
		{
			ReleaseOverlay();
		}
	}
}