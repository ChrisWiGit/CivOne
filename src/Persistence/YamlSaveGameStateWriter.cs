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
            return new GameStateDto
            {
                Difficulty = (DifficultyLevel)snapshot.Difficulty,
                GameTurn = snapshot.GameTurn,
                // Players = MapPlayPlayersToDto(snapshot.Players),
                GameOptions = snapshot.GameOptions
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