using System;
using System.Collections.Generic;
using System.Linq;
using CivOne.Civilizations;
using CivOne.Persistence.Model.Attributes;
using CivOne.Tiles;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace CivOne.Persistence.Model
{
    public interface TileFactory 
    {
        ITile CreateTile(int x, int y, Terrain terrain);
    }
    
    public class TileDtoMapper(
    ) : DtoMapper<TileDto, ITile>
    {
        public ITile FromDto(TileDto dto)
        {
            throw new NotImplementedException();
        }



        public TileDto ToDto(ITile tile)
        {
            return new TileDto
            {
                Terrain = tile.Type,
                Road = tile.Road,
                RailRoad = tile.RailRoad,
                Irrigation = tile.Irrigation,
                Pollution = tile.Pollution,
                Fortress = tile.Fortress,
                Mine = tile.Mine,
                Hut = tile.Hut,
                LandValue = tile.LandValue,
                LandScore = tile.LandScore
            };
        }
    }
}

// 	public interface IMapTiles
// {
// 	ITile this[int x, int y] { get; }

// 	int Width { get; }
// 	int Height { get; }
// }


/*

    public class TileDto
    {
        public Terrain Terrain { get; set; }
        public bool Road { get; set; }
        public bool RailRoad { get; set; }
        public bool Irrigation { get; set; }
        public bool Pollution { get; set; }
        public bool Fortress { get; set; }
        public bool Mine { get; set; }
        public bool Hut { get; set; }

        public byte LandValue { get; set; }
        public byte LandScore { get; set; }
    }
public interface ITile : ICivilopedia
	{
		int X { get; }
		int Y { get; }
		Terrain Type { get; }
		bool Special { get; }
		byte ContinentId { get; set; }
		byte LandValue { get; set; }
		byte LandScore { get; }
		byte Movement { get; }
		byte Defense { get; }
		sbyte Food { get; }
		sbyte Shield { get; }
		sbyte Trade { get; }	
		sbyte IrrigationFoodBonus { get; }
		/// <summary>
		/// Number of turns required to irrigate this terrain
		/// </summary>
		byte IrrigationCost { get; }
		sbyte MiningShieldBonus { get; }
		/// <summary>
		/// Number of turns required to build mines on this terrain
		/// </summary>
		byte MiningCost { get; }
		byte Borders { get; }
		bool Road { get; set; }
		/// <summary>
		/// Number of turns required to build a road on this terrain
		/// </summary>
		byte RoadCost { get; }
		bool RailRoad { get; set; }
		/// <summary>
		/// Number of turns required to build a railroad on terrain with road
		/// </summary>
		byte RailRoadCost { get; }
		bool Irrigation { get; set; }
		bool Pollution { get; set; }
		byte PollutionCost { get; }
		bool Fortress { get; set; }
		byte FortressCost { get; set; }
		bool Mine { get; set; }
		bool Hut { get; set; }
		byte Visited { get; }
		void Visit(byte owner);
		bool IsOcean { get; }
		City City { get; }
        bool HasCity { get; }

        IUnit[] Units { get; }
		ITile this[int relativeX, int relativeY] { get; }
		
		bool SameLocationAs(ITile other)
		{
			return this.X == other.X && this.Y == other.Y;
		}
	}
}
*/