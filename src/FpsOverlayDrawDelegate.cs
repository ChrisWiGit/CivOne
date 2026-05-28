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
using System.Globalization;
using CivOne.Enums;
using CivOne.IO;

namespace CivOne
{
	internal sealed class FpsOverlayDrawDelegate : IDisposable
	{
		private const int OverlayWidth = 110;
		private const byte OverlayColour = 11; // Civ palette: yellow
		private const byte ShadowColour = 5;   // Civ palette: dark / black

		private static readonly NumberFormatInfo NumberFormat = new()
		{
			NumberGroupSeparator = ".",
			NumberDecimalSeparator = ",",
			NumberDecimalDigits = 0,
		};

		private readonly Stopwatch _fpsWatch = Stopwatch.StartNew();
		private int _fpsFrameCount;
		private int _currentFps;
		private double _drawMsAccum;
		private int _drawMsSamples;
		private double _lastAvgDrawMs;
		private int _lastPotentialFps;
		private Bytemap? _fpsOverlay;

		private static string FormatMilliseconds(double milliseconds)
		{
			string format = milliseconds < 1 ? "{0:0.000}" : "{0:0.00}";
			return string.Format(CultureInfo.InvariantCulture, format, milliseconds).Replace('.', ',');
		}

		internal void Draw(FpsCorner fpsCorner, int canvasWidth, int canvasHeight, Bytemap[] runtimeLayers, double lastFrameMs)
		{
			if (fpsCorner == FpsCorner.Off)
			{
				ReleaseOverlay();
				RuntimeHandler.Runtime.Layers = runtimeLayers;
				return;
			}

			_fpsFrameCount++;
			if (lastFrameMs > 0)
			{
				_drawMsAccum += lastFrameMs;
				_drawMsSamples++;
			}
			if (_fpsWatch.ElapsedMilliseconds >= 1000)
			{
				_currentFps = _fpsFrameCount;
				_lastAvgDrawMs = _drawMsSamples > 0 ? _drawMsAccum / _drawMsSamples : 0;
				_lastPotentialFps = _lastAvgDrawMs > 0 ? (int)Math.Round(1000.0 / _lastAvgDrawMs) : 0;
				_fpsFrameCount = 0;
				_drawMsAccum = 0;
				_drawMsSamples = 0;
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
			string potential = _lastPotentialFps.ToString("N0", NumberFormat);
			string actual = _currentFps.ToString("N0", NumberFormat);
			string avgMs = FormatMilliseconds(_lastAvgDrawMs);
			string text = $"{potential}/{actual}fps/{avgMs}ms";

			const byte FontId = 0;
			using (var shadowText = CivOne.Graphics.Resources.Instance.GetText(text, FontId, ShadowColour))
			{
				_fpsOverlay.AddLayer(shadowText.Bitmap, x + 1, y + 1);
			}
			using (var fpsText = CivOne.Graphics.Resources.Instance.GetText(text, FontId, OverlayColour))
			{
				_fpsOverlay.AddLayer(fpsText.Bitmap, x, y);
			}
			RuntimeHandler.Runtime.Layers = [ ..runtimeLayers, _fpsOverlay ];
		}

		private static (int X, int Y) GetCornerPosition(FpsCorner fpsCorner, int canvasWidth, int canvasHeight)
		{
			return fpsCorner switch
			{
				FpsCorner.TopRight => (canvasWidth - OverlayWidth, 2),
				FpsCorner.BottomLeft => (2, canvasHeight - 10),
				FpsCorner.BottomRight => (canvasWidth - OverlayWidth, canvasHeight - 10),
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