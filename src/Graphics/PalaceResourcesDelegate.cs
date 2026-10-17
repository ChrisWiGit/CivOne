using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using CivOne.Enums;

namespace CivOne.Graphics
{
	/// <summary>
	/// Cuts the single palace sprites from CASTLE0..4 like the original game and caches them.
	/// </summary>
	/// <remarks>
	/// Each CASTLE picture holds four quadrants: medieval top left, classical top right, islamic bottom left,
	/// and the wider classical end towers bottom right. Row 0 of a quadrant is a border; every sprite is 99 pixels high.
	/// </remarks>
	public sealed class PalaceResourcesWrapper(Func<string, Picture> getPictureByName)
	{
		private const int PIECE_HEIGHT = 99;
		private const int CLASSICAL_TOWER_WIDTH = 35;

		private readonly Func<string, Picture> _getPictureByName = getPictureByName ?? throw new ArgumentNullException(nameof(getPictureByName));
		private readonly Dictionary<int, Picture> _cache = [];

		internal void ClearCache()
		{
			foreach (Picture picture in _cache.Values)
			{
				picture.Dispose();
			}

			_cache.Clear();
		}

		/// <summary>
		/// Cuts one single palace sprite.
		/// </summary>
		/// <param name="level">The picture level, 0..4.</param>
		/// <param name="style">The style; <see cref="PalaceStyle.None"/> uses the medieval quadrant.</param>
		/// <param name="kind">The sprite to cut.</param>
		/// <returns>A 99 pixel high sprite, owned by the cache.</returns>
		[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Cache ownership is transferred to the caller and disposed when the cache is cleared.")]
		internal Picture GetPalacePiece(int level, PalaceStyle style, PalacePieceKind kind)
		{
			int key = (level * 100) + ((int)style * 10) + (int)kind;
			if (_cache.TryGetValue(key, out Picture? cached))
			{
				return cached;
			}

			int quadrantX = style == PalaceStyle.Classical ? 160 : 0;
			int quadrantY = style == PalaceStyle.Islamic ? 101 : 1;
			bool classicalTower = style == PalaceStyle.Classical && kind is PalacePieceKind.LeftTower or PalacePieceKind.RightTower;

			(int x, int y, int width) = kind switch
			{
				PalacePieceKind.Center => (quadrantX, quadrantY, 52),
				PalacePieceKind.WallShadow => (quadrantX + 53, quadrantY, 24),
				PalacePieceKind.Wall => (quadrantX + 78, quadrantY, 24),
				PalacePieceKind.LeftTower when classicalTower => (160, 101, CLASSICAL_TOWER_WIDTH),
				PalacePieceKind.RightTower when classicalTower => (196, 101, CLASSICAL_TOWER_WIDTH),
				PalacePieceKind.LeftTower => (quadrantX + 103, quadrantY, 28),
				PalacePieceKind.RightTower => (quadrantX + 132, quadrantY, 27),
				_ => throw new ArgumentOutOfRangeException(nameof(kind), $"Unknown palace piece: {kind}"),
			};

			Picture piece = _getPictureByName($"CASTLE{level}")[x, y, width, PIECE_HEIGHT];
			_cache[key] = piece;
			return piece;
		}
	}
}
