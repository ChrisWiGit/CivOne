using System;
using CivOne.IO;

namespace CivOne.Services.Maps
{
	internal sealed class NearestNeighborMapBitmapScaler : IMapBitmapScaler
	{
		public Bytemap Scale(Bytemap source, int targetWidth, int targetHeight)
		{
			if (source == null || targetWidth <= 0 || targetHeight <= 0)
			{
				return new (1, 1);
			}
			Bytemap output = new(targetWidth, targetHeight);

			// Scaling runs once per rendered map tile, so the per-pixel handle and range validation of
			// the Bytemap indexer dominates the cost. Row spans move that validation out of the loop.
			for (int y = 0; y < targetHeight; y++)
			{
				int sourceY = targetHeight == 1 ? 0 : y * (source.Height - 1) / (targetHeight - 1);
				ReadOnlySpan<byte> sourceRow = source.Row(sourceY);
				Span<byte> targetRow = output.Row(y);

				for (int x = 0; x < targetWidth; x++)
				{
					int sourceX = targetWidth == 1 ? 0 : x * (source.Width - 1) / (targetWidth - 1);
					targetRow[x] = sourceRow[sourceX];
				}
			}

			return output;
		}
	}
}