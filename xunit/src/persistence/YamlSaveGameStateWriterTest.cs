using System.IO;
using System.Linq;
using CivOne.Civilizations;
using CivOne.Enums;
using CivOne.Leaders;
using CivOne.Persistence;
using CivOne.Tiles;
using Xunit;

namespace CivOne.UnitTests.Persistence
{
    public class YamlSaveGameStateWriterTest : TestsBase2
    {
        private GameState mockGameState;
        private GameStateDto gameStateDto;

        private YamlSaveGameStateWriter actualWriter;

        // setup
        public YamlSaveGameStateWriterTest()
        {
            // Create a mock GameState with some test data
            mockGameState = new GameState
            {
                Difficulty = 3, // King
                Players = [new (new MockPlayerCiv())]
            };

            actualWriter = new YamlSaveGameStateWriter();
        }

        [Fact]
        public void TestWrite()
        {
           // TextStream
            using var stream = new MemoryStream();
            actualWriter.Write(stream, mockGameState);

            // stream to text
            stream.Seek(0, SeekOrigin.Begin);
            // to file
            using var reader = new StreamReader(stream);
            string yamlOutput = reader.ReadToEnd();
            File.WriteAllText("test_output.yaml", yamlOutput);
        }
    }

    public class PlayerLeader : BaseLeader
	{
		protected override Leader Leader => Leader.Montezuma;

		public PlayerLeader() : base("Caesar", "DOESNOTSTARTWITHKING", 40, 22)
		{
            // King name disables usage of global Resource instance (null reference exception), 
            // so we set the name to something else, and set the default name to "Caesar"
			Development = DevelopmentLevel.Expansionistic;
			Militarism = MilitarismLevel.Civilized;
		}
	}

    internal class MockPlayerCiv : BaseCivilization<PlayerLeader>
	{
		public MockPlayerCiv() : base(Enums.Civilization.Romans, "Roman", "Romans", "ceas")
		{
			StartX = 36;
			StartY = 19;
			CityNames = new[]
			{
				"Rome",
				"Caesarea",
				"Carthage",
				"Nicopolis",
				"Byzantium",
				"Brundisium",
				"Syracuse",
				"Antioch",
				"Palmyra",
				"Cyrene",
				"Gordion",
				"Tyrus",
				"Jerusalem",
				"Seleucia",
				"Ravenna",
				"Artaxata"
			};
		}
    }
}
