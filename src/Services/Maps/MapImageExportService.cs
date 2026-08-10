using System;
using CivOne.Graphics;
using CivOne.Graphics.ImageFormats;
using CivOne.Tiles;

namespace CivOne.Services.Maps
{
	/// <summary>
	/// Renders the whole map at the original tile size and stores it as an 8-bit indexed bitmap.
	/// </summary>
	public class MapImageExportService : IMapImageExportService
	{
		private readonly BmpImageWriterDelegate _imageWriter = new();

		/// <inheritdoc/>
		public void ExportToFile(IMap map, Player? visibilityPlayer, string filePath)
		{
			ArgumentNullException.ThrowIfNull(map);
			ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

			ITile[,] tiles = map[0, 0, map.Width, map.Height];

			// Passing no pixel size keeps the original 16x16 tiles the game was designed around.
			using IBitmap picture = tiles.ToBitmap(player: visibilityPlayer);
			_imageWriter.Write(picture, filePath);
		}
	}
}
