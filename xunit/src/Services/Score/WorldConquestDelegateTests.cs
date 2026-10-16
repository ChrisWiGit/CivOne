using System.Collections.Generic;
using CivOne.Services.Score;
using Xunit;

namespace CivOne.UnitTests.Services.Score
{
	public class WorldConquestDelegateTests
	{
		[Fact]
		public void IsTrueOnceAllRivalsAreDestroyed()
		{
			Assert.True(new WorldConquestDelegate().IsConquered(CreateGame(rivalDestroyed: true)));
		}

		[Fact]
		public void IsFalseWhileARivalSurvives()
		{
			Assert.False(new WorldConquestDelegate().IsConquered(CreateGame(rivalDestroyed: false)));
		}

		[Fact]
		public void LivingBarbariansDoNotCountAsRival()
		{
			List<Participant> civilizations = CreateGame(rivalDestroyed: true);

			Assert.Contains(civilizations, c => c.IsBarbarian && !c.IsDestroyed);
			Assert.True(new WorldConquestDelegate().IsConquered(civilizations));
		}

		[Fact]
		public void DestroyedHumanIsNoConquest()
		{
			List<Participant> civilizations = CreateGame(rivalDestroyed: true);
			civilizations[1] = new Participant(IsHuman: true, IsBarbarian: false, IsDestroyed: true);

			Assert.False(new WorldConquestDelegate().IsConquered(civilizations));
		}

		[Fact]
		public void StaysTrueWhenANewCivilizationAppearsAfterConquest()
		{
			List<Participant> civilizations = CreateGame(rivalDestroyed: true);
			WorldConquestDelegate conquest = new();
			Assert.True(conquest.IsConquered(civilizations));

			civilizations.Add(new Participant(IsHuman: false, IsBarbarian: false, IsDestroyed: false));

			Assert.True(conquest.IsConquered(civilizations));
		}

		private static List<Participant> CreateGame(bool rivalDestroyed) =>
		[
			new(IsHuman: false, IsBarbarian: true, IsDestroyed: false),
			new(IsHuman: true, IsBarbarian: false, IsDestroyed: false),
			new(IsHuman: false, IsBarbarian: false, IsDestroyed: true),
			new(IsHuman: false, IsBarbarian: false, IsDestroyed: rivalDestroyed),
		];

		private sealed record Participant(bool IsHuman, bool IsBarbarian, bool IsDestroyed) : IConquestParticipant;
	}
}
