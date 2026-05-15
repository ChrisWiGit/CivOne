// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Linq;
using CivOne.Enums;
using CivOne.Services.SpaceShip;

namespace CivOne.Persistence.Model
{
	/// <summary>
	/// Specialized Map2d for canonical spaceship component grids. Encodes
	/// SpaceShipComponentType values as single-character strings in YAML:
	/// 'E' for Empty, 'S' for Structural, 'C' for Component, 'M' for Module,
	/// and dedicated characters for detailed spaceship parts.
	/// 
	/// YAML output example:
	/// - ECMM000000000
	/// - S00000000000
	/// - M00000000000
	/// (canonical spaceship grid dimensions)
	/// </summary>
	public class SpaceShipGridMap2D : Map2d<SpaceShipComponentType>
	{
		public SpaceShipGridMap2D() : base(SpaceShipSlotBlueprintFactoryProvider.CanonicalGridWidth, SpaceShipSlotBlueprintFactoryProvider.CanonicalGridHeight)
		{
		}

		internal SpaceShipGridMap2D(SpaceShipComponentType[,] ownsData) : base(ownsData)
		{
		}

		internal SpaceShipGridMap2D(SpaceShipComponentType[][] ownsData) : base(ownsData)
		{
		}

		public SpaceShipGridMap2D(string[] rows)
			: base()
		{
			Rows = rows;
		}

		public SpaceShipGridMap2D(SpaceShipGridMap2D other) : base((Map2d<SpaceShipComponentType>)other)
		{
		}

		public static implicit operator SpaceShipGridMap2D(SpaceShipComponentType[,] data) => new(data);
		public static implicit operator SpaceShipComponentType[,](SpaceShipGridMap2D map) => map.Data;

		public string[] Rows
		{
			get => ToRows();
			set
			{
				var jagged = FromRows(value);
				int height = jagged.Length;
				int width = height == 0 ? 0 : jagged[0].Length;
				Data = new SpaceShipComponentType[width, height];
				for (int y = 0; y < height; y++)
				{
					for (int x = 0; x < width; x++)
						Data[x, y] = jagged[y][x];
				}
			}
		}

		private string[] ToRows()
		{
			var result = new string[Height()];

			for (int y = 0; y < Height(); y++)
			{
				char[] row = new char[Width()];

				for (int x = 0; x < Width(); x++)
				{
					row[x] = ComponentTypeToChar(Data[x, y]);
				}

				result[y] = new string(row);
			}

			return result;
		}

		private static SpaceShipComponentType[][] FromRows(string[] rows)
		{
			if (rows == null)
				return [];

			var data = new SpaceShipComponentType[rows.Length][];

			for (int y = 0; y < rows.Length; y++)
			{
				if (rows[y] == null)
					throw new ArgumentException($"Row {y} cannot be null.");

				data[y] = [.. rows[y].Select(CharToComponentType)];
			}

			return data;
		}

		private static char ComponentTypeToChar(SpaceShipComponentType type)
			=> type switch
			{
				SpaceShipComponentType.Empty => 'E',
				SpaceShipComponentType.Structural => 'S',
				SpaceShipComponentType.Component => 'C',
				SpaceShipComponentType.Module => 'M',
				SpaceShipComponentType.StructureHorizontal => 'H',
				SpaceShipComponentType.StructureVertical => 'V',
				SpaceShipComponentType.StructureNode => 'N',
				SpaceShipComponentType.CommandModule => 'K',
				SpaceShipComponentType.LifeSupportModule => 'L',
				SpaceShipComponentType.HabitationModule => 'B',
				SpaceShipComponentType.SolarPanelModule => 'O',
				SpaceShipComponentType.FuelComponent => 'F',
				SpaceShipComponentType.PropulsionComponent => 'P',
				_ => 'E'
			};

		private static SpaceShipComponentType CharToComponentType(char c)
			=> c switch
			{
				'E' => SpaceShipComponentType.Empty,
				'0' => SpaceShipComponentType.Empty,   // Legacy support: 0 = empty
				'S' => SpaceShipComponentType.Structural,
				'C' => SpaceShipComponentType.Component,
				'M' => SpaceShipComponentType.Module,
				'H' => SpaceShipComponentType.StructureHorizontal,
				'V' => SpaceShipComponentType.StructureVertical,
				'N' => SpaceShipComponentType.StructureNode,
				'K' => SpaceShipComponentType.CommandModule,
				'L' => SpaceShipComponentType.LifeSupportModule,
				'B' => SpaceShipComponentType.HabitationModule,
				'O' => SpaceShipComponentType.SolarPanelModule,
				'F' => SpaceShipComponentType.FuelComponent,
				'P' => SpaceShipComponentType.PropulsionComponent,
				_ => throw new FormatException($"Invalid spaceship component character '{c}'.")
			};
	}
}
