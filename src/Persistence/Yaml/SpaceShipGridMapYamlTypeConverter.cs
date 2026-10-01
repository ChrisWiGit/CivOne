using System;
using CivOne.Persistence.Model;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace CivOne.Persistence.Yaml
{
	/// <summary>
	/// YAML converter for SpaceShipGridMap2d. Serializes/deserializes the 12×12 grid
	/// as an array of strings where each character represents a component type:
	/// 'E' = Empty, 'S' = Structural, 'C' = Component, 'M' = Module, '0' = Empty (legacy).
	/// 
	/// Example YAML output:
	/// Grid:
	///   - ECMM000000000
	///   - S00000000000
	///   - M00000000000
	///   - ...
	/// </summary>
	public class SpaceShipGridMapYamlTypeConverter : IYamlTypeConverter
	{
		public bool Accepts(Type type) => type == typeof(SpaceShipGridMap2D);

		public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
		{
			var rows = (string[]?)rootDeserializer(typeof(string[]));

			if (rows == null || rows.Length == 0)
			{
				return new SpaceShipGridMap2D();
			}

			return new SpaceShipGridMap2D(rows);
		}

		public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
		{
			ArgumentNullException.ThrowIfNull(value);

			if (value is not SpaceShipGridMap2D grid)
			{
				throw new ArgumentException($"WriteYaml expected a {nameof(SpaceShipGridMap2D)} but received {value.GetType().FullName}.");
			}

			serializer(grid.Rows, typeof(string[]));
		}
	}
}