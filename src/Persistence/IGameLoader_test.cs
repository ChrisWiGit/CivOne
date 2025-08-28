using System.IO;
using CivOne.IO;

namespace CivOne.Persistence_TEST
{


	// public class OriginalLoadGameImpl
	// {
	// 	private SaveData RawSaveData { get; set; }

	// 	public OriginalLoadGameImpl(Stream stream)
	// 	{
	// 		// SaveData = new SaveDataAdapter(gameData);
	// 	}

	// 	public IGameData ToGameData() { return null; }
	// }

	// public interface IGameDataAdapter : IGameData
	// {
	// }
	// public interface IGameLoader
	// {
	// 	void Load(Stream stream, IGame game);
	// }

	// public class GameDataBridgeImpl : IGameData
	// {
	// 	private readonly IGameData Game;

	// 	public GameDataBridgeImpl(IGameData game)
	// 	{
	// 		Game = game;
	// 		//Convert Game to IGameData
	// 		// 	// Game.LoadSave.cs: Save()
	// 		// 	public void Save(string sveFile, string mapFile)
	// 		// {
	// 		// 	using (IGameData gameData = new SaveDataAdapter())
	// 		// 	{
	// 		// 		gameData.GameTurn = _gameTurn;
	// 		// 		gameData.HumanPlayer = (ushort)PlayerNumber(HumanPlayer);
	// 		// 		gameData.RandomSeed = Map.Instance.SaveMap(mapFile);
	// 	}
	// }

	// public class OriginalGameLoaderImpl : IGameLoader
	// {
	// 	public void Save(Stream stream, IGame game)
	// 	{
	// 		OriginalLoadGameImpl saveGame = new(stream);
	// 		IGameDataAdapter gameData = new GameDataBridgeImpl(game);

	// 		saveGame.DataStream().CopyTo(stream);
	// 	}
	// }

	// public partial class Game
	// {
	// 	private int score;
	// 	private String playerName;
	// 	private Map<String, Integer> inventory;

	// 	private Game() { } // private ctor

	// 	public static class Builder
	// 	{
	// 		private int score;
	// 		private String playerName;
	// 		private Map<String, Integer> inventory;

	// 		public Builder score(int score) { this.score = score; return this; }
	// 		public Builder playerName(String name) { this.playerName = name; return this; }
	// 		public Builder inventory(Map<String, Integer> inv) { this.inventory = inv; return this; }

	// 		public Game build()
	// 		{
	// 			Game g = new Game();
	// 			g.score = this.score;
	// 			g.playerName = this.playerName;
	// 			g.inventory = this.inventory;
	// 			return g;
	// 		}
	// 	}
	// }

	// public class GameFactory
	// {
	// 	public IGame FromData(IGameData data)
	// 	{
	// 		return new Game.Builder()
	// 			.Score(data.Score)
	// 			.PlayerName(data.PlayerName)
	// 			.Inventory(data.Inventory)
	// 			.Build();
	// 	}
	// }

	// public class FileGameLoaderImpl
	// {
	// 	private readonly IGameLoader gameLoader;
	// 	private readonly GameFactory gameFactory;

	// 	public FileGameLoaderImpl(IGameLoader gameLoader, GameFactory gameFactory)
	// 	{
	// 		this.gameLoader = gameLoader;
	// 		this.gameFactory = gameFactory;
	// 	}

	// 	public IGame Load(string filePath)
	// 	{
	// 		using FileStream fs = new(filePath, FileMode.Open, FileAccess.Read);
	// 		GameData data = gameLoader.Load(fs);
	// 		return gameFactory.FromData(data);
	// 	}
	// }

	// public class OriginalFileGameLoaderImpl(IGame game) : FileGameLoaderImpl(game, new OriginalGameLoaderImpl());

	// public class Program
	// {

	// 	public static void MainOff(string[] args)
	// 	{


	// 	}
	// }
}