using CivOne.Persistence.Factories;
using CivOne.Persistence.Game;
using CivOne.Services.Palace;

namespace CivOne
{
	/// <summary>
	/// SVE persistence of the human player's palace and its build counter.
	/// Like the original, the SVE file holds only the human player's palace.
	/// </summary>
	public partial class Game
	{
		private void StorePalace(SaveDataAdapter gameData)
		{
			PalaceData palace = HumanPlayer.Palace;
			gameData.Palace = new SvePalaceDelegate().ToSve(palace);
			gameData.PalaceBuildCount = CVS.CheckedUInt16(palace.BuildCount, nameof(Game), "Save.PalaceBuildCount");
		}

		private void RestorePalace(IGameData gameData)
		{
			SvePalaceDelegate svePalace = new(new RuntimeLogger());
			((IPlayerRestorable)HumanPlayer).Palace = svePalace.FromSve(gameData.Palace, gameData.PalaceBuildCount);
		}
	}
}
