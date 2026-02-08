using System;
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
    public class YamlSaveGameStateWriterTest : IDisposable
    {
        private RuntimeSettings rs;
        private MockRuntime runtime;
        private GameState mockGameState;
        private GameStateDto gameStateDto;

        private Player[] players;

        private YamlSaveGameStateWriter actualWriter;

        protected void SetupRuntime()
        {
            rs = new RuntimeSettings();
            rs.InitialSeed = 7595;
            runtime = new MockRuntime(rs);
        }

        // setup
        public YamlSaveGameStateWriterTest()
        {
            SetupRuntime();

            players =
			[
				new (new Roman()),
                new (new German())
            ];
            // players[0].Id = 1;
            // Create a mock GameState with some test data
            mockGameState = new GameState
            {
                Difficulty = 3, // King
                Players = players
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

        public void Dispose()
        {
            // Tear everything down
            Map.Reset();
            runtime?.Dispose();
            RuntimeHandler.Wipe();
        }
    }
}
