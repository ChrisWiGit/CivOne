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
	/// <summary>
	/// Helper for building and tracking the FPS overlay bitmap and text.
	/// The overlay shows both the actual FPS (based on frame counts) and the potential FPS (based on average draw time).
	/// See README.md for details on interpreting the overlay metrics.
	/// </summary>
	internal sealed class FpsOverlayDrawDelegate : IDisposable
	{
		internal const int FpsOverlayFontId = 1;
		internal const byte OverlayColour = 11;
		internal const byte ShadowColour = 5;
		internal const int LineSpacing = 1;

		private static readonly NumberFormatInfo NumberFormat = new()
		{
			NumberGroupSeparator = ".",
			NumberDecimalSeparator = ",",
			NumberDecimalDigits = 0,
		};

		private sealed class FpsMetricCounter
		{
			private readonly Stopwatch _fpsWatch = Stopwatch.StartNew();
			private int _fpsFrameCount;
			private int _currentFps;
			private double _drawMsAccum;
			private int _drawMsSamples;
			private double _lastAvgDrawMs;
			private int _lastPotentialFps;

			internal string UpdateAndGetText(bool countFrame, double frameMs)
			{
				if (countFrame)
				{
					_fpsFrameCount++;
					if (frameMs > 0)
					{
						_drawMsAccum += frameMs;
						_drawMsSamples++;
					}
				}

				if (_fpsWatch.ElapsedMilliseconds >= 1000)
				{
					_currentFps = _fpsFrameCount;
					if (_drawMsSamples > 0)
					{
						// On many screens there is no animation and thus no draw updated needed
						// To prevent the potential fps from showing as "0" in those cases, only update it when we have a valid draw time sample.
						_lastAvgDrawMs = _drawMsAccum / _drawMsSamples;
						_lastPotentialFps = _lastAvgDrawMs > 0 ? (int)Math.Round(1000.0 / _lastAvgDrawMs) : 0;
					}
					_fpsFrameCount = 0;
					_drawMsAccum = 0;
					_drawMsSamples = 0;
					_fpsWatch.Restart();
				}

				string potential = _lastPotentialFps.ToString("N0", NumberFormat);
				string actual = _currentFps.ToString("N0", NumberFormat);
				string avgMs = FormatMilliseconds(_lastAvgDrawMs);
				return $"{potential}/{actual}fps/{avgMs}ms";
			}

			internal void Reset()
			{
				_fpsFrameCount = 0;
				_currentFps = 0;
				_drawMsAccum = 0;
				_drawMsSamples = 0;
				_lastAvgDrawMs = 0;
				_lastPotentialFps = 0;
				_fpsWatch.Restart();
			}
		}

		private readonly FpsMetricCounter _gameCounter = new();
		private readonly FpsMetricCounter _renderCounter = new();
		private string _lastOverlayText = string.Empty;

		private static string FormatMilliseconds(double milliseconds)
		{
			string format = milliseconds < 1 ? "{0:0.000}" : "{0:0.00}";
			return string.Format(CultureInfo.InvariantCulture, format, milliseconds).Replace('.', ',');
		}

		internal bool TryBuildOverlayBitmap(
			FpsCorner fpsCorner,
			bool gameFrameUpdated,
			double gameFrameMs,
			double renderFrameMs,
			out Bytemap? overlayBitmap,
			out bool shouldClearTexture)
		{
			overlayBitmap = null;
			shouldClearTexture = false;

			if (fpsCorner == FpsCorner.Off)
			{
				shouldClearTexture = _lastOverlayText.Length > 0;
				Reset();
				return false;
			}

			string gameText = _gameCounter.UpdateAndGetText(gameFrameUpdated, gameFrameMs);
			string renderText = _renderCounter.UpdateAndGetText(true, renderFrameMs);
			string nextText = $"Game: {gameText}\nRender: {renderText}";

			if (string.Equals(_lastOverlayText, nextText, StringComparison.Ordinal))
			{
				// fast opt out if text hasn't changed, which is common when the game is paused or running at a stable frame rate
				return false;
			}

			overlayBitmap = BuildOverlayBitmap(FpsOverlayFontId, $"Game: {gameText}", $"Render: {renderText}");
			_lastOverlayText = nextText;
			return true;
		}

		private static Bytemap BuildOverlayBitmap(int fontId, params string[] lines)
		{
			if (lines == null || lines.Length == 0)
			{
				return new Bytemap(1, 1);
			}

			int maxWidth = 1;
			int totalHeight = 1;

			for (int i = 0; i < lines.Length; i++)
			{
				using var shadowText = Graphics.Resources.Instance.GetText(lines[i], fontId, ShadowColour);
				if (shadowText.Bitmap.Width > maxWidth) maxWidth = shadowText.Bitmap.Width;
				totalHeight += shadowText.Bitmap.Height;
				if (i < lines.Length - 1) totalHeight += LineSpacing;
			}

			Bytemap overlayBitmap = new(maxWidth + 1, totalHeight + 1);
			int y = 0;
			for (int i = 0; i < lines.Length; i++)
			{
				using var shadowText = Graphics.Resources.Instance.GetText(lines[i], fontId, ShadowColour);
				using var fpsText = Graphics.Resources.Instance.GetText(lines[i], fontId, OverlayColour);
				overlayBitmap.AddLayer(shadowText.Bitmap, 1, y + 1);
				overlayBitmap.AddLayer(fpsText.Bitmap, 0, y);
				y += shadowText.Bitmap.Height + LineSpacing;
			}

			return overlayBitmap;
		}

		private static (int X, int Y) GetOverlayCanvasPosition(FpsCorner corner, int canvasWidth, int canvasHeight, int textWidth, int textHeight)
			=> corner switch
			{
				FpsCorner.TopRight => (Math.Max(2, canvasWidth - textWidth - 2), 2),
				FpsCorner.BottomLeft => (2, Math.Max(2, canvasHeight - textHeight - 2)),
				FpsCorner.BottomRight => (Math.Max(2, canvasWidth - textWidth - 2), Math.Max(2, canvasHeight - textHeight - 2)),
				_ => (2, 2),
			};

		internal static (int X, int Y, int Width, int Height) GetOverlayTargetRect(
			FpsCorner corner,
			int canvasWidth,
			int canvasHeight,
			int drawWidth,
			int drawHeight,
			int drawOffsetX,
			int drawOffsetY,
			int textWidth,
			int textHeight)
		{
			if (drawWidth <= 0 || drawHeight <= 0 || canvasWidth <= 0 || canvasHeight <= 0)
			{
				return (0, 0, 0, 0);
			}

			float scaleX = (float)drawWidth / canvasWidth;
			float scaleY = (float)drawHeight / canvasHeight;
			(int canvasX, int canvasY) = GetOverlayCanvasPosition(corner, canvasWidth, canvasHeight, textWidth, textHeight);

			int targetX = drawOffsetX + (int)Math.Round(canvasX * scaleX);
			int targetY = drawOffsetY + (int)Math.Round(canvasY * scaleY);
			int targetWidth = Math.Max(1, (int)Math.Round(textWidth * scaleX));
			int targetHeight = Math.Max(1, (int)Math.Round(textHeight * scaleY));
			return (targetX, targetY, targetWidth, targetHeight);
		}

		internal void Reset()
		{
			_gameCounter.Reset();
			_renderCounter.Reset();
			_lastOverlayText = string.Empty;
		}

		public void Dispose()
		{
			Reset();
		}
	}
}