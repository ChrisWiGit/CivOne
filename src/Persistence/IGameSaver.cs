using System.IO;
using CivOne.IO;

namespace CivOne.Persistence_TEST
{
	// public interface IArrayService
	// {
	// 	void SetArray<T>(ref T structure, string fieldName, params byte[] values) where T : struct;
	// }

	// public class OriginalSaveGameImpl
	// {
	// 	private SaveData RawSaveData { get; set; }

	// 	public OriginalSaveGameImpl(IGameData gameData)
	// 	{
	// 		// SaveData = new SaveDataAdapter(gameData);
	// 	}

	// 	public Stream DataStream() { return null; }
	// }

	// public interface IGameDataAdapter : IGameData
	// {
	// }
	// public interface IGameSaver
	// {
	// 	void Save(Stream stream, IGame game);
	// }

	// public class GameDataBridgeImpl : IGameDataAdapter
	// {
	// 	private IGame Game { get; }

	// 	public GameDataBridgeImpl(IGame game)
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

	// public class OriginalGameSaverImpl : IGameSaver
	// {
	// 	public void Save(Stream stream, IGame game)
	// 	{
	// 		IGameDataAdapter gameData = new GameDataBridgeImpl(game);
	// 		OriginalSaveGameImpl saveGame = new(gameData);

	// 		saveGame.DataStream().CopyTo(stream);
	// 	}
	// }

	// public class FileGameSaverImpl(IGame game, IGameSaver gameSaver)
	// {
	// 	private IGameSaver GameSaver { get; } = gameSaver;
	// 	private IGame Game { get; } = game;

	// 	public void Save(string filePath)
	// 	{
	// 		using FileStream fs = new(filePath, FileMode.Create, FileAccess.Write);
	// 		GameSaver.Save(fs, Game);
	// 	}
	// }

	// public class OriginalFileGameSaverImpl(IGame game) : FileGameSaverImpl(game, new OriginalGameSaverImpl());

	// public class Program
	// {

	// 	public static void MainOff(string[] args)
	// 	{


	// 	}
	// }



	// public interface IGameLoader
	// {
	// 	void Load(Stream stream);
	// }

	// public interface ILoadAdapter
	// {
	// 	// LoadDataAdapter LoadData { get; }
	// }

}