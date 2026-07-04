using System;
using CivOne.IO;

namespace CivOne.Services.Maps
{
	internal sealed class PaletteAwareWeightedMapBitmapScaler : IMapBitmapScaler
	{
		public Bytemap Scale(Bytemap source, int targetWidth, int targetHeight)
		{
			Bytemap output = new(targetWidth, targetHeight);
			if (source == null || targetWidth <= 0 || targetHeight <= 0)
			{
				return output;
			}

			if (source.Width == targetWidth && source.Height == targetHeight)
			{
				output.Dispose();
				return Bytemap.Copy(source);
			}

			double xRatio = targetWidth > 1 ? (double)(source.Width - 1) / (targetWidth - 1) : 0d;
			double yRatio = targetHeight > 1 ? (double)(source.Height - 1) / (targetHeight - 1) : 0d;

			for (int y = 0; y < targetHeight; y++)
			{
				double sampleY = y * yRatio;
				int sourceY = (int)sampleY;
				int sourceY2 = Math.Min(sourceY + 1, source.Height - 1);
				double yBlend = sampleY - sourceY;
				double invYBlend = 1d - yBlend;
				for (int x = 0; x < targetWidth; x++)
				{
					double sampleX = x * xRatio;
					int sourceX = (int)sampleX;
					int sourceX2 = Math.Min(sourceX + 1, source.Width - 1);
					double xBlend = sampleX - sourceX;
					double invXBlend = 1d - xBlend;

					byte c00 = source[sourceX, sourceY];
					byte c10 = source[sourceX2, sourceY];
					byte c01 = source[sourceX, sourceY2];
					byte c11 = source[sourceX2, sourceY2];

					double w00 = invXBlend * invYBlend;
					double w10 = xBlend * invYBlend;
					double w01 = invXBlend * yBlend;
					double w11 = xBlend * yBlend;

					double bestWeight = -1d;
					byte bestIndex = 0;

					double weight00 = w00 + (c10 == c00 ? w10 : 0d) + (c01 == c00 ? w01 : 0d) + (c11 == c00 ? w11 : 0d);
					if (weight00 > bestWeight)
					{
						bestWeight = weight00;
						bestIndex = c00;
					}

					double weight10 = (c00 == c10 ? w00 : 0d) + w10 + (c01 == c10 ? w01 : 0d) + (c11 == c10 ? w11 : 0d);
					if (weight10 > bestWeight || (Math.Abs(weight10 - bestWeight) < 0.0001d && bestIndex == 0 && c10 != 0))
					{
						bestWeight = weight10;
						bestIndex = c10;
					}

					double weight01 = (c00 == c01 ? w00 : 0d) + (c10 == c01 ? w10 : 0d) + w01 + (c11 == c01 ? w11 : 0d);
					if (weight01 > bestWeight || (Math.Abs(weight01 - bestWeight) < 0.0001d && bestIndex == 0 && c01 != 0))
					{
						bestWeight = weight01;
						bestIndex = c01;
					}

					double weight11 = (c00 == c11 ? w00 : 0d) + (c10 == c11 ? w10 : 0d) + (c01 == c11 ? w01 : 0d) + w11;
					if (weight11 > bestWeight || (Math.Abs(weight11 - bestWeight) < 0.0001d && bestIndex == 0 && c11 != 0))
					{
						bestIndex = c11;
					}

					output[x, y] = bestIndex;
				}
			}

			return output;
		}
	}
}