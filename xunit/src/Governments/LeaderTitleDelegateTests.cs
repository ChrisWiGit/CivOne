using CivOne.Governments;
using CivOne.Services;
using Xunit;

namespace CivOne.UnitTests.Governments
{
	public class LeaderTitleDelegateTests
	{
		private readonly LeaderTitleDelegate _testee = new(new TranslationIdentityService());

		[Fact]
		public void TitlesFollowTheOriginalGame()
		{
			Assert.Equal("Mr.", _testee.Title(new Anarchy()));
			Assert.Equal("Emperor", _testee.Title(new Despotism()));
			Assert.Equal("King", _testee.Title(new Monarchy()));
			Assert.Equal("Comrade", _testee.Title(new Communism()));
			Assert.Equal("President", _testee.Title(new Republic()));
			Assert.Equal("President", _testee.Title(new Democracy()));
		}
	}
}
