using System.Collections.Generic;
using CivOne.Units;

namespace CivOne
{
	public partial class Game : IGameSnapshotSource
	{
		public List<IUnit> Units => _units;

		public Dictionary<byte, byte> AdvanceOrigin => _advanceOrigin;

		public ushort AnthologyTurn => _anthologyTurn;

		public List<ReplayData> ReplayData => _replayData;

		public int TerrainMasterWord => Map.Instance.TerrainMasterWord;

		Player IGameSnapshotSource.CurrentPlayer => CurrentPlayer;

		Player IGameSnapshotSource.HumanPlayer => HumanPlayer;

		Player[] IGameSnapshotSource.Players => _players;

		List<City> IGameSnapshotSource.Cities => _cities;

		string[] IGameSnapshotSource.CityNames => CityNames;

		byte IGameSnapshotSource.PlayerNumber(Player player)
		{
			return PlayerNumber(player);
		}
	}
}