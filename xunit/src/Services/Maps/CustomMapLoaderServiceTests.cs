// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Collections.Generic;
using CivOne.Enums;
using CivOne.Persistence.Model;
using Xunit;

namespace CivOne.Services.Maps
{
	/// <summary>
	/// Tests for <see cref="CustomMapLoaderService.ResolveStartPositions"/>, the validation step that
	/// turns a loaded map file's raw <c>StartPositions</c> dictionary (civilization name -&gt; location)
	/// into a <see cref="Civilization"/>-keyed one, rejecting unknown names, Barbarians, and out-of-bounds
	/// coordinates. Pure: takes a <see cref="MapDto"/> and returns a value, no static/engine state involved.
	/// </summary>
	public class CustomMapLoaderServiceTests
	{
		private static MapDto MapDto(int width, int height, Dictionary<string, MapLocation>? startPositions)
			=> new()
			{
				Tiles = new Map2d<TileDto>(width, height),
				StartPositions = startPositions
			};

		[Fact]
		public void ResolveStartPositionsReturnsNullWhenStartPositionsIsNull()
		{
			Dictionary<Civilization, MapLocation>? actual = CustomMapLoaderService.ResolveStartPositions(MapDto(10, 10, null));

			Assert.Null(actual);
		}

		[Fact]
		public void ResolveStartPositionsReturnsNullWhenStartPositionsIsEmpty()
		{
			Dictionary<Civilization, MapLocation>? actual = CustomMapLoaderService.ResolveStartPositions(MapDto(10, 10, []));

			Assert.Null(actual);
		}

		[Fact]
		public void ResolveStartPositionsThrowsArgumentNullExceptionWhenMapDtoIsNull()
		{
			Assert.Throws<ArgumentNullException>(() => CustomMapLoaderService.ResolveStartPositions(null!));
		}

		[Fact]
		public void ResolveStartPositionsThrowsFormatExceptionForUnknownCivilizationName()
		{
			MapDto mapDto = MapDto(10, 10, new Dictionary<string, MapLocation> { ["NotACivilization"] = new(1, 1) });

			Assert.Throws<FormatException>(() => CustomMapLoaderService.ResolveStartPositions(mapDto));
		}

		[Fact]
		public void ResolveStartPositionsThrowsFormatExceptionForBarbarians()
		{
			MapDto mapDto = MapDto(10, 10, new Dictionary<string, MapLocation> { [nameof(Civilization.Barbarians)] = new(1, 1) });

			Assert.Throws<FormatException>(() => CustomMapLoaderService.ResolveStartPositions(mapDto));
		}

		[Theory]
		[InlineData(10u, 5u)] // X == width, out of bounds
		[InlineData(5u, 10u)] // Y == height, out of bounds
		public void ResolveStartPositionsThrowsFormatExceptionWhenLocationOutsideMapBounds(uint x, uint y)
		{
			MapDto mapDto = MapDto(10, 10, new Dictionary<string, MapLocation> { [nameof(Civilization.Romans)] = new(x, y) });

			Assert.Throws<FormatException>(() => CustomMapLoaderService.ResolveStartPositions(mapDto));
		}

		[Fact]
		public void ResolveStartPositionsResolvesValidEntries()
		{
			MapDto mapDto = MapDto(10, 10, new Dictionary<string, MapLocation>
			{
				[nameof(Civilization.Romans)] = new(3, 4),
				[nameof(Civilization.Babylonians)] = new(6, 7)
			});

			Dictionary<Civilization, MapLocation>? actual = CustomMapLoaderService.ResolveStartPositions(mapDto);

			Assert.NotNull(actual);
			Assert.Equal(2, actual.Count);
			MapLocation romans = Assert.Contains(Civilization.Romans, actual);
			Assert.Equal(3u, romans.X);
			Assert.Equal(4u, romans.Y);
			MapLocation babylonians = Assert.Contains(Civilization.Babylonians, actual);
			Assert.Equal(6u, babylonians.X);
			Assert.Equal(7u, babylonians.Y);
		}
	}
}
