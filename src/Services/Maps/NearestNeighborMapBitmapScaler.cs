using CivOne.IO;

namespace CivOne.Services.Maps
{
	internal sealed class NearestNeighborMapBitmapScaler : IMapBitmapScaler
	{
		public Bytemap Scale(Bytemap source, int targetWidth, int targetHeight)
		{
			Bytemap output = new(targetWidth, targetHeight);
			if (source == null || targetWidth <= 0 || targetHeight <= 0)
			{
				return output;
			}

			for (int y = 0; y < targetHeight; y++)
			{
				int sourceY = targetHeight == 1 ? 0 : y * (source.Height - 1) / (targetHeight - 1);

				for (int x = 0; x < targetWidth; x++)
				{
					int sourceX = targetWidth == 1 ? 0 : x * (source.Width - 1) / (targetWidth - 1);
					output[x, y] = source[sourceX, sourceY];
				}
			}

			return output;
		}
	}
}