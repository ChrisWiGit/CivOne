using System;
using System.IO;
using System.Linq;
using CivOne.Graphics;
using CivOne.IO;

namespace CivOne.Mcp.Automation
{
	public sealed class RuntimeLayerScreenshotRoutine : IMcpScreenshotRoutine
	{
		private readonly IRuntime _runtime;
		private readonly IMcpArtifactWriter _artifactWriter;
		private readonly IMcpGameTickProvider _gameTickProvider;

		public McpScreenshotResult CaptureFull(string sessionId, bool includeCursor)
		{
			using IBitmap fullFrame = BuildFullFrame(includeCursor);
			return SaveResult(sessionId, fullFrame);
		}

		public McpScreenshotResult CaptureRegion(string sessionId, int x, int y, int width, int height, bool includeCursor)
		{
			using Picture fullFrame = BuildFullFrame(includeCursor);
			ClampRectangle(fullFrame.Bitmap.Width, fullFrame.Bitmap.Height, ref x, ref y, ref width, ref height);

			if (width <= 0 || height <= 0)
			{
				throw new InvalidOperationException("Region has no visible area after clamping.");
			}

			using Bytemap cropped = fullFrame.Crop(x, y, width, height);
			using IBitmap region = new Picture(cropped, fullFrame.Palette.Copy());
			return SaveResult(sessionId, region);
		}

		private Picture BuildFullFrame(bool includeCursor)
		{
			Palette? runtimePalette = _runtime.Palette;
			if (_runtime.Layers == null || runtimePalette == null || runtimePalette == Palette.Empty)
				throw new InvalidOperationException("Runtime frame is not available.");

			int width = Math.Max(1, _runtime.CanvasWidth);
			int height = Math.Max(1, _runtime.CanvasHeight);
			Picture output = new(width, height, runtimePalette.Copy());
			if (output.Palette == null)
			{
				throw new InvalidOperationException("Runtime palette is not available.");
			}
			output.Palette[0] = Colour.Black;

			foreach (Bytemap bytemap in _runtime.Layers)
			{
				output.AddLayer(bytemap);
			}

			// Cursor capture needs a readable runtime cursor source.
			// The current runtime contract only exposes a write-only cursor property,
			// so this flag is kept for forward compatibility.
			_ = includeCursor;

			return output;
		}

		private McpScreenshotResult SaveResult(string sessionId, IBitmap bitmap)
		{
			byte[] bytes = PngWriter.Write(bitmap.Bitmap, bitmap.Palette.Entries.ToArray());
			string artifactPath = _artifactWriter.WriteArtifact(sessionId, "png", bytes);
			return new McpScreenshotResult(
				sessionId,
				_gameTickProvider.CurrentTick,
				DateTime.UtcNow,
				bitmap.Bitmap.Width,
				bitmap.Bitmap.Height,
				"png",
				artifactPath.Replace(Path.DirectorySeparatorChar, '/'));
		}

		private static void ClampRectangle(int maxWidth, int maxHeight, ref int x, ref int y, ref int width, ref int height)
		{
			if (x < 0)
			{
				width += x;
				x = 0;
			}
			if (y < 0)
			{
				height += y;
				y = 0;
			}
			if (x + width > maxWidth) width = maxWidth - x;
			if (y + height > maxHeight) height = maxHeight - y;
		}

		public RuntimeLayerScreenshotRoutine(IRuntime runtime, IMcpArtifactWriter artifactWriter, IMcpGameTickProvider gameTickProvider)
		{
			_runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
			_artifactWriter = artifactWriter ?? throw new ArgumentNullException(nameof(artifactWriter));
			_gameTickProvider = gameTickProvider ?? throw new ArgumentNullException(nameof(gameTickProvider));
		}
	}
}
