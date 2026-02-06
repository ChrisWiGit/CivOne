using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CivOne.Persistence
{
    public class YamlSaveGame
    {
        // wichtig: nicht nur 1:1 abbilden, sondern für menschen lesbar
        // d.h. enums als strings, und wenn wir ein enum haben, 
        // sollte eine weitere eigenschaft, die so ähnlich heißt, alle möglichen werte als liste enthalten
        // ReplayData ignorieren wir erstmal
    }
    public class YamlSaveGameStateWriter : IGameStateWriter
    {
        public void Write(Stream stream, GameState snapshot)
        {
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var yaml = serializer.Serialize(snapshot);
            StreamWriter writer = new(stream);
            writer.Write(yaml);
        }

        YamlSaveGame toYaml(GameState snapshot)
        {
            // Conversion logic from GameState to YamlSaveGame goes here
            // This is a placeholder implementation
            return new YamlSaveGame();
        }
    }
}

// public interface IGameSnapshotSource
	{
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