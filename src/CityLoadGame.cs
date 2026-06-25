using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using CivOne.Enums;
using CivOne.Tiles;

namespace CivOne
{
	#pragma warning disable CA1822 // Mark members as static
	[SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "This class is intended to be used internally for loading game data, and using List<T> provides flexibility and ease of use for the operations it performs. We do not need to expose this class as part of a public API, so we can accept the use of List<T> here without concern for the usual guidelines that apply to public-facing code.")]
	public class CityLoadGame
	{
		public List<ITile> GetResourceTilesFromGameData(City city, byte[] gameData)
		{
			List<ITile> resourceTiles = [];

			Debug.Assert(gameData.Length == 6, $"Invalid Resource game data for {city.Name} - expected 6 bytes, got {gameData.Length}");


			if (((gameData[0] >> 0) & 1) > 0) resourceTiles.Add(city.Tile[0, -1]);
			if (((gameData[0] >> 1) & 1) > 0) resourceTiles.Add(city.Tile[1, 0]);
			if (((gameData[0] >> 2) & 1) > 0) resourceTiles.Add(city.Tile[0, 1]);
			if (((gameData[0] >> 3) & 1) > 0) resourceTiles.Add(city.Tile[-1, 0]);
			if (((gameData[0] >> 4) & 1) > 0) resourceTiles.Add(city.Tile[1, -1]);
			if (((gameData[0] >> 5) & 1) > 0) resourceTiles.Add(city.Tile[1, 1]);
			if (((gameData[0] >> 6) & 1) > 0) resourceTiles.Add(city.Tile[-1, 1]);
			if (((gameData[0] >> 7) & 1) > 0) resourceTiles.Add(city.Tile[-1, -1]);

			if (((gameData[1] >> 0) & 1) > 0) resourceTiles.Add(city.Tile[0, -2]);
			if (((gameData[1] >> 1) & 1) > 0) resourceTiles.Add(city.Tile[2, 0]);
			if (((gameData[1] >> 2) & 1) > 0) resourceTiles.Add(city.Tile[0, 2]);
			if (((gameData[1] >> 3) & 1) > 0) resourceTiles.Add(city.Tile[-2, 0]);
			if (((gameData[1] >> 4) & 1) > 0) resourceTiles.Add(city.Tile[-1, -2]);
			if (((gameData[1] >> 5) & 1) > 0) resourceTiles.Add(city.Tile[1, -2]);
			if (((gameData[1] >> 6) & 1) > 0) resourceTiles.Add(city.Tile[2, -1]);
			if (((gameData[1] >> 7) & 1) > 0) resourceTiles.Add(city.Tile[2, 1]);

			if (((gameData[2] >> 0) & 1) > 0) resourceTiles.Add(city.Tile[1, 2]);
			if (((gameData[2] >> 1) & 1) > 0) resourceTiles.Add(city.Tile[-1, 2]);
			if (((gameData[2] >> 2) & 1) > 0) resourceTiles.Add(city.Tile[-2, 1]);
			if (((gameData[2] >> 3) & 1) > 0) resourceTiles.Add(city.Tile[-2, -1]);

			return resourceTiles;
		}

		public List<Citizen> GetSpecialistsFromGameData(byte[] gameData)
		{
			List<Citizen> specialists = [];
			int specialistBits = GetSpecialistBitsFromGameData(gameData);

			for (int index = 0; index < 8; index++)
			{
				Citizen? specialist = GetSpecialistTypeFromBits(specialistBits, index);
				if (specialist.HasValue)
				{
					specialists.Add(specialist.Value);
				}
			}
			return specialists;
		}

		internal int GetSpecialistBitsFromGameData(byte[] gameData)
		{
			return (gameData[4] & 0xFF) | ((gameData[5] & 0xFF) << 8);
		}

		internal Citizen? GetSpecialistTypeFromBits(int specialistBits, int index)
		{
			int type = (specialistBits >> (2 * index)) & 0b11;
			switch (type)
			{
				case 1: return Citizen.Taxman;
				case 2: return Citizen.Scientist;
				case 3: return Citizen.Entertainer;
				default: return null; // 0 = NONE
			}
		}

		public static readonly (int dx, int dy)[][] Offsets =
		{
			[
				(0, -1), (1, 0), (0, 1), (-1, 0),
				(1, -1), (1, 1), (-1, 1), (-1, -1)
			],
			[
				(0, -2), (2, 0), (0, 2), (-2, 0),
				(-1, -2), (1, -2), (2, -1), (2, 1)
			],
			[
				(1, 2), (-1, 2), (-2, 1), (-2, -1)
			]
		};
	}
}