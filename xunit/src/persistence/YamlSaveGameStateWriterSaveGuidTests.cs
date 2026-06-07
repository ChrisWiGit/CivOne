using System;
using System.IO;
using CivOne.Persistence;
using CivOne.Persistence.Model;
using CivOne.Persistence.Yaml;
using Xunit;

namespace CivOne.UnitTests.Persistence
{
	public sealed class YamlSaveGameStateWriterSaveGuidTests
	{
		[Fact]
		public void WriteWithSaveMetaDataWritesRootSaveGuid()
		{
			GameState snapshot = new()
			{
				Difficulty = 0,
				Players = []
			};

			SaveFileMetaData saveMetaData = new();
			saveMetaData.InitializeForNewGame("test-version", DateTimeOffset.UtcNow);

			StubYamlSaveGameStateWriter testee = new();
			using MemoryStream stream = new();
			testee.Write(stream, snapshot, saveMetaData);

			stream.Position = 0;
			using StreamReader reader = new(stream);
			string yaml = reader.ReadToEnd();

			SaveGame1FileRootDto actual = YamlReader
				.OfString(yaml)
				.WithStandard()
				.WithTypeConverter(new MapDtoTileDtoYamlConverter())
				.As<SaveGame1FileRootDto>();

			Assert.NotNull(actual);
			Assert.NotNull(actual.Meta);
			Assert.True(actual.Meta.SaveGuid.HasValue);
			Assert.NotEqual(Guid.Empty, actual.Meta.SaveGuid.Value);
			Assert.Equal(saveMetaData.SaveGuid, actual.Meta.SaveGuid.Value);
		}

		private sealed class StubYamlSaveGameStateWriter : YamlSaveGameStateWriter
		{
			protected override GameStateDto CreateDto(GameState snapshot)
				=> new()
				{
					Difficulty = DifficultyLevel.Chieftain,
					GameTurn = 1,
					Players = [],
					Map = null
				};
		}
	}
}
