using CivOne.Agents;
using CivOne.src;
using Xunit;

namespace CivOne.UnitTests
{
	/// <summary>
	/// Verifies that the turn context reports the difficulty of the acting player.
	/// Difficulty is chosen per opponent in the new game screen, so two players running the same AI
	/// must be able to see different values.
	/// </summary>
	public class TurnContextDifficultyTests : TestsBase
	{
		/// <summary>
		/// Each player's own difficulty reaches its turn context.
		/// </summary>
		[Fact]
		public void Difficulty_IsPerPlayer()
		{
			Player first = Game.Instance.GetPlayer(0)!;
			Player second = Game.Instance.GetPlayer(1)!;

			first.AiDifficulty = AiDifficulty.Chieftain;
			second.AiDifficulty = AiDifficulty.Deity;

			Assert.Equal(AiDifficulty.Chieftain, new TurnContext(first).Difficulty);
			Assert.Equal(AiDifficulty.Deity, new TurnContext(second).Difficulty);
		}

		/// <summary>
		/// A player without an explicit difficulty reports Unspecified rather than a made-up value.
		/// </summary>
		[Fact]
		public void Difficulty_IsUnspecifiedWhenNotSet()
		{
			Player player = Game.Instance.GetPlayer(0)!;
			player.AiDifficulty = AiDifficulty.Unspecified;

			Assert.Equal(AiDifficulty.Unspecified, new TurnContext(player).Difficulty);
		}
	}
}
