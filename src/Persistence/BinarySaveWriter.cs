using System.IO;

namespace CivOne.Persistence
{
    public class BinarySaveWriter : IGameStateWriter
    {
        public void Write(Stream stream, GameState snapshot)
        {
            using SaveDataAdapter gameData = new();

            gameData.GameTurn = snapshot.GameTurn;
            gameData.HumanPlayer = snapshot.HumanPlayer;
            gameData.RandomSeed = (ushort)snapshot.RandomSeed;
            gameData.Difficulty = (ushort)snapshot.Difficulty;

            gameData.ActiveCivilizations = snapshot.ActiveCivilizations;
            gameData.CivilizationIdentity = snapshot.CivilizationIdentity;
            gameData.CurrentResearch = snapshot.CurrentResearch;
            gameData.DiscoveredAdvanceIDs = snapshot.DiscoveredAdvanceIDs;

            gameData.LeaderNames = snapshot.LeaderNames;
            gameData.CivilizationNames = snapshot.CivilizationNames;
            gameData.CitizenNames = snapshot.CitizenNames;
            gameData.CityNames = snapshot.CityNames;

            gameData.PlayerGold = snapshot.PlayerGold;
            gameData.ResearchProgress = snapshot.ResearchProgress;
            gameData.TaxRate = snapshot.TaxRate;
            gameData.ScienceRate = snapshot.ScienceRate;
            gameData.StartingPositionX = snapshot.StartingPositionX;
            gameData.Government = snapshot.Government;

            gameData.Cities = snapshot.Cities;
            gameData.Units = snapshot.Units;
            gameData.Wonders = snapshot.Wonders;
            gameData.TileVisibility = snapshot.TileVisibility;
            gameData.AdvanceFirstDiscovery = snapshot.AdvanceFirstDiscovery;
            gameData.GameOptions = snapshot.GameOptions;

            gameData.NextAnthologyTurn = snapshot.NextAnthologyTurn;
            gameData.OpponentCount = snapshot.OpponentCount;
            gameData.ReplayData = snapshot.ReplayData;

            byte[] data = gameData.GetBytes();
            stream.Write(data, 0, data.Length);
        }
    }
}