using System.IO;
using System.Linq;
using CivOne.Civilizations;
using CivOne.Persistence;
using CivOne.Tiles;
using Xunit;

namespace CivOne.UnitTests.Persistence
{
    public class YamlSaveGameStateWriterTest
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
                Players = [new(new Roman())]
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
}
