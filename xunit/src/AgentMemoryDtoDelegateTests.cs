using CivOne.Agents;
using Xunit;

namespace CivOne.UnitTests
{
	public class AgentMemoryDtoDelegateTests
	{
		[Fact]
		public void GetMemory_SerializesSnapshotDtoToYamlString()
		{
			// Arrange
			MemoryDto state = new()
			{
				Policy = "economic",
				AggressionLevel = 2,
				ConservativeMovement = true
			};
			AgentMemoryDtoDelegate<MemoryDto> testee = new(
				snapshotDelegate: () => state,
				restoreDelegate: dto => state = dto,
				createDefaultDelegate: () => new MemoryDto());

			// Act
			string actual = testee.GetMemory();

			// Assert
			Assert.Contains("Policy", actual);
			Assert.Contains("economic", actual);
			Assert.Contains("AggressionLevel", actual);
		}

		[Fact]
		public void SetMemory_WhenYamlValid_RestoresDto()
		{
			// Arrange
			MemoryDto state = new();
			AgentMemoryDtoDelegate<MemoryDto> testee = new(
				snapshotDelegate: () => state,
				restoreDelegate: dto => state = dto,
				createDefaultDelegate: () => new MemoryDto());
			string yaml = "Policy: expansion\nAggressionLevel: 4\nConservativeMovement: false\n";

			// Act
			testee.SetMemory(yaml);

			// Assert
			Assert.NotNull(state);
			Assert.Equal("expansion", state.Policy);
			Assert.Equal(4, state.AggressionLevel);
			Assert.False(state.ConservativeMovement);
		}

		[Fact]
		public void SetMemory_WhenYamlEmpty_UsesDefaultDto()
		{
			// Arrange
			MemoryDto state = new()
			{
				Policy = "custom",
				AggressionLevel = 9,
				ConservativeMovement = false
			};
			AgentMemoryDtoDelegate<MemoryDto> testee = new(
				snapshotDelegate: () => state,
				restoreDelegate: dto => state = dto,
				createDefaultDelegate: () => new MemoryDto
				{
					Policy = "default",
					AggressionLevel = 1,
					ConservativeMovement = true
				});

			// Act
			testee.SetMemory(string.Empty);

			// Assert
			Assert.Equal("default", state.Policy);
			Assert.Equal(1, state.AggressionLevel);
			Assert.True(state.ConservativeMovement);
		}

		[Fact]
		public void SetMemory_WhenYamlInvalidAndFallbackEnabled_UsesDefaultDto()
		{
			// Arrange
			MemoryDto state = new()
			{
				Policy = "custom",
				AggressionLevel = 9,
				ConservativeMovement = false
			};
			AgentMemoryDtoDelegate<MemoryDto> testee = new(
				snapshotDelegate: () => state,
				restoreDelegate: dto => state = dto,
				createDefaultDelegate: () => new MemoryDto
				{
					Policy = "default",
					AggressionLevel = 1,
					ConservativeMovement = true
				},
				useDefaultOnDeserializationError: true);

			// Act
			testee.SetMemory("not: [valid");

			// Assert
			Assert.Equal("default", state.Policy);
			Assert.Equal(1, state.AggressionLevel);
			Assert.True(state.ConservativeMovement);
		}

		private sealed class MemoryDto
		{
			public string Policy { get; set; } = string.Empty;
			public int AggressionLevel { get; set; }
			public bool ConservativeMovement { get; set; }
		}
	}
}
