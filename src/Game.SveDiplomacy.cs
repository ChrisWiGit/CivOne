using System.Linq;
using CivOne.Persistence.Game;
using CivOne.Services.Diplomacy;

namespace CivOne
{
	/// <summary>
	/// SVE persistence of the players' diplomacy flags and embassies.
	/// </summary>
	public partial class Game
	{
		private void StoreDiplomacy(SaveDataAdapter gameData)
		{
			gameData.Diplomacy = new SveDiplomacyDelegate().ToSve([.. _players.Select(p => ((IPlayer)p).Embassies)]);
		}

		private void RestoreDiplomacy(IGameData gameData)
		{
			SveDiplomacyDelegate sveDiplomacy = new();
			ushort[] block = gameData.Diplomacy;
			for (int i = 0; i < _players.Length; i++)
			{
				IPlayerRestorable player = _players[i];
				ushort[] row = sveDiplomacy.RowOf(block, i);
				player.Diplomacy = row;
				player.Embassies = sveDiplomacy.EmbassiesOf(row);
			}
		}
	}
}
