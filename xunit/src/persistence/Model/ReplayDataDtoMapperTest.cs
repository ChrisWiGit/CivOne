using System.IO;
using Xunit;

namespace CivOne.Persistence.Model
{
	public class ReplayDataDtoMapperTest
	{
		private readonly ReplayDataDtoMapper _testee = new();

		// ── ToDto ────────────────────────────────────────────────────────────────

		[Fact]
		public void ToDto_CityBuilt_MapsAllFields()
		{
			var domain = new ReplayData.CityBuilt(turn: 10, ownerId: 2, cityId: 5, cityNameId: 3, x: 12, y: 7);

			var dto = _testee.ToDto(domain);

			Assert.NotNull(dto);
			Assert.Equal(10, dto.Turn);
			Assert.NotNull(dto.CityBuilt);
			Assert.Equal(2, dto.CityBuilt.OwnerId);
			Assert.Equal(5, dto.CityBuilt.CityId);
			Assert.Equal(3, dto.CityBuilt.CityNameId);
			Assert.Equal(12, dto.CityBuilt.X);
			Assert.Equal(7, dto.CityBuilt.Y);
			Assert.Null(dto.CityDestroyed);
			Assert.Null(dto.CivilizationDestroyed);
		}

		[Fact]
		public void ToDto_CityDestroyed_MapsAllFields()
		{
			var domain = new ReplayData.CityDestroyed(turn: 20, cityId: 8, cityNameId: 4, x: 30, y: 15);

			var dto = _testee.ToDto(domain);

			Assert.NotNull(dto);
			Assert.Equal(20, dto.Turn);
			Assert.NotNull(dto.CityDestroyed);
			Assert.Equal(8, dto.CityDestroyed.CityId);
			Assert.Equal(4, dto.CityDestroyed.CityNameId);
			Assert.Equal(30, dto.CityDestroyed.X);
			Assert.Equal(15, dto.CityDestroyed.Y);
			Assert.Null(dto.CityBuilt);
			Assert.Null(dto.CivilizationDestroyed);
		}

		[Fact]
		public void ToDto_CivilizationDestroyed_MapsAllFields()
		{
			var domain = new ReplayData.CivilizationDestroyed(turn: 42, destroyedId: 3, destroyedById: 1);

			var dto = _testee.ToDto(domain);

			Assert.NotNull(dto);
			Assert.Equal(42, dto.Turn);
			Assert.NotNull(dto.CivilizationDestroyed);
			Assert.Equal(3, dto.CivilizationDestroyed.DestroyedId);
			Assert.Equal(1, dto.CivilizationDestroyed.DestroyedById);
			Assert.Null(dto.CityBuilt);
			Assert.Null(dto.CityDestroyed);
		}

		// ── FromDto ──────────────────────────────────────────────────────────────

		[Fact]
		public void FromDto_CityBuilt_MapsAllFields()
		{
			var dto = new ReplayDataDto
			{
				Turn = 10,
				CityBuilt = new ReplayDataDto.CityBuiltData { OwnerId = 2, CityId = 5, CityNameId = 3, X = 12, Y = 7 }
			};

			var domain = _testee.FromDto(dto);

			var actual = Assert.IsType<ReplayData.CityBuilt>(domain);
			Assert.Equal(10, actual.Turn);
			Assert.Equal(2, actual.OwnerId);
			Assert.Equal(5, actual.CityId);
			Assert.Equal(3, actual.CityNameId);
			Assert.Equal(12, actual.X);
			Assert.Equal(7, actual.Y);
		}

		[Fact]
		public void FromDto_CityDestroyed_MapsAllFields()
		{
			var dto = new ReplayDataDto
			{
				Turn = 20,
				CityDestroyed = new ReplayDataDto.CityDestroyedData { CityId = 8, CityNameId = 4, X = 30, Y = 15 }
			};

			var domain = _testee.FromDto(dto);

			var actual = Assert.IsType<ReplayData.CityDestroyed>(domain);
			Assert.Equal(20, actual.Turn);
			Assert.Equal(8, actual.CityId);
			Assert.Equal(4, actual.CityNameId);
			Assert.Equal(30, actual.X);
			Assert.Equal(15, actual.Y);
		}

		[Fact]
		public void FromDto_CivilizationDestroyed_MapsAllFields()
		{
			var dto = new ReplayDataDto
			{
				Turn = 42,
				CivilizationDestroyed = new ReplayDataDto.CivilizationDestroyedData { DestroyedId = 3, DestroyedById = 1 }
			};

			var domain = _testee.FromDto(dto);

			var actual = Assert.IsType<ReplayData.CivilizationDestroyed>(domain);
			Assert.Equal(42, actual.Turn);
			Assert.Equal(3, actual.DestroyedId);
			Assert.Equal(1, actual.DestroyedById);
		}

		// ── Roundtrip ────────────────────────────────────────────────────────────

		[Fact]
		public void RoundTrip_CityBuilt_PreservesAllFields()
		{
			var original = new ReplayData.CityBuilt(turn: 10, ownerId: 2, cityId: 5, cityNameId: 3, x: 12, y: 7);

			var restored = Assert.IsType<ReplayData.CityBuilt>(_testee.FromDto(_testee.ToDto(original)));

			Assert.Equal(original.Turn, restored.Turn);
			Assert.Equal(original.OwnerId, restored.OwnerId);
			Assert.Equal(original.CityId, restored.CityId);
			Assert.Equal(original.CityNameId, restored.CityNameId);
			Assert.Equal(original.X, restored.X);
			Assert.Equal(original.Y, restored.Y);
		}

		[Fact]
		public void RoundTrip_CityDestroyed_PreservesAllFields()
		{
			var original = new ReplayData.CityDestroyed(turn: 20, cityId: 8, cityNameId: 4, x: 30, y: 15);

			var restored = Assert.IsType<ReplayData.CityDestroyed>(_testee.FromDto(_testee.ToDto(original)));

			Assert.Equal(original.Turn, restored.Turn);
			Assert.Equal(original.CityId, restored.CityId);
			Assert.Equal(original.CityNameId, restored.CityNameId);
			Assert.Equal(original.X, restored.X);
			Assert.Equal(original.Y, restored.Y);
		}

		[Fact]
		public void RoundTrip_CivilizationDestroyed_PreservesAllFields()
		{
			var original = new ReplayData.CivilizationDestroyed(turn: 42, destroyedId: 3, destroyedById: 1);

			var restored = Assert.IsType<ReplayData.CivilizationDestroyed>(_testee.FromDto(_testee.ToDto(original)));

			Assert.Equal(original.Turn, restored.Turn);
			Assert.Equal(original.DestroyedId, restored.DestroyedId);
			Assert.Equal(original.DestroyedById, restored.DestroyedById);
		}

		// ── Multi-set guard ──────────────────────────────────────────────────────

		[Fact]
		public void FromDto_MultiplePropertiesSet_Throws()
		{
			var dto = new ReplayDataDto
			{
				Turn = 1,
				CityBuilt = new ReplayDataDto.CityBuiltData { OwnerId = 1 },
				CivilizationDestroyed = new ReplayDataDto.CivilizationDestroyedData { DestroyedId = 1, DestroyedById = 2 }
			};

			Assert.Throws<InvalidDataException>(() => _testee.FromDto(dto));
		}

		// ── List helpers ─────────────────────────────────────────────────────────

		[Fact]
		public void ToDtoList_NullInput_ReturnsEmptyList()
		{
			var result = _testee.ToDtoList(null);

			Assert.Empty(result);
		}

		[Fact]
		public void FromDtoList_NullInput_ReturnsEmptyList()
		{
			var result = _testee.FromDtoList(null);

			Assert.Empty(result);
		}

		[Fact]
		public void ToDtoList_PreservesOrder()
		{
			var items = new ReplayData[]
			{
				new ReplayData.CivilizationDestroyed(turn: 1, destroyedId: 0, destroyedById: 1),
				new ReplayData.CivilizationDestroyed(turn: 2, destroyedId: 2, destroyedById: 3),
				new ReplayData.CivilizationDestroyed(turn: 3, destroyedId: 4, destroyedById: 5),
			};

			var dtos = _testee.ToDtoList(items);

			Assert.Equal(3, dtos.Count);
			Assert.Equal(1, dtos[0].Turn);
			Assert.Equal(2, dtos[1].Turn);
			Assert.Equal(3, dtos[2].Turn);
		}
	}
}
