namespace CivOne.Persistence
{
	public class GameFactoryImpl : IGameFactory
	{
		public IGame Create(IGameData data)
		{
			return new Game.GameBuilder(data)
				.SetupAll()
				.Build();
		}
	}
}