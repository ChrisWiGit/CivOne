using System;
using CivOne.Enums;
using CivOne.Persistence.Yaml;
using CivOne.Services.SpaceShip;
using Xunit;
using YamlDotNet.Serialization;

namespace CivOne.Persistence.Model
{
	public class SpaceShipGridMap2dTest
	{
		[Fact]
		public void ConstructorFromRowsPreservesRowColumnOrientation()
		{
			string[] rows =
			[
				"EEEKKEEEEFPE",
				"EOOKKOOENHHE",
				"EOOEEOOEVFPE",
				"HHHHHHHHNFPE",
				"BBLLBBLLNHHE",
				"BBLLBBLLVFPE",
				"LLBBLLBBVFPE",
				"LLBBLLBBNHHE",
				"HHHHHHHHNFPE",
				"EOOEEOOEVFPE",
				"EOOEEOOENHHE",
				"EEEEEEEEEFPE"
			];

			var actual = new SpaceShipGridMap2D(rows);

			Assert.Equal(rows, actual.Rows);
			Assert.Equal(SpaceShipComponentType.CommandModule, actual[3, 0]);
			Assert.Equal(SpaceShipComponentType.StructureHorizontal, actual[0, 3]);
		}

		[Fact]
		public void YamlSerializationRoundtripPreservesRows()
		{
			string[] rows =
			[
				"EEEKKEEEEFPE",
				"EOOKKOOENHHE",
				"EOOEEOOEVFPE"
			];

			var map = new SpaceShipGridMap2D(rows);
			var serializer = new SerializerBuilder()
				.WithTypeConverter(new SpaceShipGridMapYamlTypeConverter()).Build();
			var yaml = serializer.Serialize(map);

			var deserializer = new DeserializerBuilder()
				.WithTypeConverter(new SpaceShipGridMapYamlTypeConverter()).Build();
			var deserialized = deserializer.Deserialize<SpaceShipGridMap2D>(yaml);

			Assert.Equal(rows, deserialized.Rows);
		}

		[Fact]
		public void YamlWriteNullThrowsArgumentNullException()
		{
			var converter = new SpaceShipGridMapYamlTypeConverter();

			Assert.Throws<ArgumentNullException>(() => converter.WriteYaml(null!, null, typeof(SpaceShipGridMap2D), null!));
		}

		[Fact]
		public void YamlWriteWrongTypeThrowsArgumentException()
		{
			var converter = new SpaceShipGridMapYamlTypeConverter();

			Assert.Throws<ArgumentException>(() => converter.WriteYaml(null!, new object(), typeof(object), null!));
		}

		[Fact]
		public void YamlReadNullReturnsCanonicalEmptyGrid()
		{
			var deserializer = new DeserializerBuilder()
				.WithTypeConverter(new SpaceShipGridMapYamlTypeConverter()).Build();

			var deserialized = deserializer.Deserialize<SpaceShipGridMap2D>("null");

			Assert.Equal(SpaceShipSlotBlueprintFactoryProvider.CanonicalGridWidth, deserialized.Width());
			Assert.Equal(SpaceShipSlotBlueprintFactoryProvider.CanonicalGridHeight, deserialized.Height());
		}

		[Fact]
		public void YamlReadEmptyArrayReturnsCanonicalEmptyGrid()
		{
			var deserializer = new DeserializerBuilder()
				.WithTypeConverter(new SpaceShipGridMapYamlTypeConverter()).Build();

			var deserialized = deserializer.Deserialize<SpaceShipGridMap2D>("[]");

			Assert.Equal(SpaceShipSlotBlueprintFactoryProvider.CanonicalGridWidth, deserialized.Width());
			Assert.Equal(SpaceShipSlotBlueprintFactoryProvider.CanonicalGridHeight, deserialized.Height());
		}
	}
}
