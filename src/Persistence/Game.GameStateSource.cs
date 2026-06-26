using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using CivOne.Services.GlobalWarming;
using CivOne.Tiles;
using CivOne.Units;

namespace CivOne
{
	[SuppressMessage("Microsoft.Interoperability", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes", Justification = "The IGameSnapshotSource members are not intended to be called directly, and making them public would pollute the Game API.")]
	public partial class Game : IGameSnapshotSource
	{
		List<IUnit> IGameSnapshotSource.Units => _units;

		Dictionary<byte, byte> IGameSnapshotSource.AdvanceOrigin => _advanceOrigin;

		ushort IGameSnapshotSource.AnthologyTurn => _anthologyTurn;

		ushort IGameSnapshotSource.PeaceTurns => _peaceTurns;

		public ushort PlayerFutureTech => HumanPlayer?.FutureTechCount ?? _playerFutureTech;

		[SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists", Justification = "A list is suffice and changing it would require unnecessary changes to the GameState and related code.")]
		public List<ReplayData> ReplayData => _replayData;

		public uint? GameRandomSeed
		{
			get
			{
				if (Common.Random == null)
				{
					return null;
				}

				int[] status = Common.Random.GetStatus();
				// Combine the two 16-bit values into a single 32-bit value
				return ((uint)(ushort)status[1] << 16) | (ushort)status[0];
			}
		}


		public int TerrainMasterWord => Map.Instance.TerrainMasterWord;

		IGlobalWarmingService IGameSnapshotSource.GlobalWarmingService => _globalWarmingService;

		public ITile[,] MapTiles => Map.Instance.Tiles;

		Player IGameSnapshotSource.CurrentPlayer => CurrentPlayer;

		Player IGameSnapshotSource.HumanPlayer => HumanPlayer;

		Player[] IGameSnapshotSource.Players => _players;

		List<City> IGameSnapshotSource.Cities => _cities;

		string[] IGameSnapshotSource.CityNames => CityNames;

		byte IGameSnapshotSource.PlayerNumber(Player player)
		{
			return PlayerNumber(player);
		}
		
		(short X, short Y)? IGameSnapshotSource.GetHumanLastMapPosition()
		{
			var gamePlay = Common.GamePlay;
			if (gamePlay == null)
			{
				return null;
			}

			var x = gamePlay.X;
			var y = gamePlay.Y;
			if (x < 0 || y < 0 || x >= Map.WIDTH || y >= Map.HEIGHT)
			{
				return null;
			}

			return ((short)x, (short)y);
		}
	}
}