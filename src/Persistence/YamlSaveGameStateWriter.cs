using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using CivOne.Civilizations;
using CivOne.Persistence.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CivOne.Persistence
{
    public class PlayerDto
    {
        public CivilizationDto Civilization { get; set; }
    }
    public class TileDto 
    {
    }

    public class MapDto
    {
        TileDto [,] Tiles { get; set; }
    }

    public class UnitDto 
    {
    }

    public class CityDto 
    {
    }

    public enum DifficultyLevel 
    {
        Chieftain = 0,
        Warlord,
        Prince,
        King,
        Emperor,
        Deity
    }
    
    public class GameStateDto
    {
        // wichtig: nicht nur 1:1 abbilden, sondern für menschen lesbar
        // d.h. enums als strings, und wenn wir ein enum haben, 
        // sollte eine weitere eigenschaft, die so ähnlich heißt, alle möglichen werte als liste enthalten
        // ReplayData ignorieren wir erstmal
        public DifficultyLevel Difficulty { get; set; }

        public uint GameTurn { get; set; }
        public ushort HumanPlayer { get; set; }

        public List<PlayerDto> Players { get; set; }

        public uint RandomSeed { get; set; }

        public uint AnthologyTurn { get; set; }

        public uint TerrainSeed { get; set; }

        public MapDto Map { get; set; }

        public List<GameOptionEnum> GameOptions { get; set; }
    }

    public class ExplainedGameStateDto : GameStateDto
    {
        // zusätzlich zu den eigenschaften von GameStateDto, die wir auch in der yaml haben wollen, 
        // können wir hier weitere eigenschaften hinzufügen, die z.b. erklärungen oder menschenlesbare formen der daten enthalten
        public string DifficultyAll { get => string.Join(", ", Enum.GetNames<DifficultyLevel>()); }

        public string GameOptionsAll { get => string.Join(", ", Enum.GetNames<GameOptionEnum>()); }
    }


    public class YamlSaveGameStateWriter : IGameStateWriter
    {
        public YamlSaveGameStateWriter()
        {
        }
        public void Write(Stream stream, GameState snapshot)
        {
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var yaml = serializer.Serialize(toDto(snapshot));
            stream.Write(System.Text.Encoding.UTF8.GetBytes(yaml));
        }

        GameStateDto toDto(GameState snapshot)
        {
            // Conversion logic from GameState to YamlSaveGame goes here
            // This is a placeholder implementation
            return new ExplainedGameStateDto
            {
                Difficulty = (DifficultyLevel)snapshot.Difficulty,
                GameTurn = snapshot.GameTurn,
                Players = MapPlayPlayersToDto(snapshot.Players),
                GameOptions = snapshot.GameOptions
            };
        }

        List<PlayerDto> MapPlayPlayersToDto(Player[] players)
        {
            return [.. players.Select(MapPlayerToDto)];
        }
        PlayerDto MapPlayerToDto(Player player)
        {
            return new PlayerDto
            {
                Civilization = MapCivilizationToDto(player.Civilization)
            };
        }
        CivilizationDto MapCivilizationToDto(ICivilization civ)
        {
            return new CivilizationDto
            {
                LeaderClassName = civ.Leader.GetType().Name
            };
        }
    }
}

// public interface IGameSnapshotSource
	// {
	// 	int Difficulty { get; }
	// 	Player CurrentPlayer { get; }
	// 	Player HumanPlayer { get; }

	// 	Player[] Players { get; }

	// 	List<City> Cities { get; }
	// 	List<IUnit> Units { get; }

	// 	Dictionary<byte, byte> AdvanceOrigin { get; }

	// 	ushort GameTurn { get; }
	// 	ushort AnthologyTurn { get; }

	// 	string[] CityNames { get; }

	// 	List<ReplayData> ReplayData { get; }

	// 	byte PlayerNumber(Player player);

	// 	public bool Animations { get; }
	// 	public bool Sound { get; }
	// 	public bool CivilopediaText { get; }
	// 	public bool EndOfTurn { get; }
	// 	public bool InstantAdvice { get; }
	// 	public bool AutoSave { get; }
	// 	public bool EnemyMoves { get; }
	// 	public bool Palace { get; }

	// 	int TerrainMasterWord { get; }
	// }