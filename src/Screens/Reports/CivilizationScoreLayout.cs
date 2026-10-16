using System.Collections.Generic;

namespace CivOne.Screens.Reports
{
	/// <summary>
	/// Positions on the civilization score screen, in 320x200 space.
	/// Built by <see cref="CivilizationScoreLayoutDelegate"/>.
	/// </summary>
	/// <param name="CitizensHeaderY">Y of the "Citizens" line.</param>
	/// <param name="CitizenIcons">Top-left of each citizen icon that fits, in drawing order.</param>
	/// <param name="AchievementsHeaderY">Y of the "Achievements" line.</param>
	/// <param name="Wonders">Icon and name position of each wonder that fits, in drawing order.</param>
	/// <param name="SpaceShipY">Y of the spaceship line, or <c>null</c> if it is not drawn.</param>
	/// <param name="OtherTermsY">Y of the pollution/peace/future tech line, or <c>null</c> if it is not drawn.</param>
	/// <param name="TotalY">Y of the total line.</param>
	internal sealed record CivilizationScoreLayout(
		int CitizensHeaderY,
		IReadOnlyList<(int X, int Y)> CitizenIcons,
		int AchievementsHeaderY,
		IReadOnlyList<(int IconX, int IconY, int NameX, int NameY)> Wonders,
		int? SpaceShipY,
		int? OtherTermsY,
		int TotalY);
}
