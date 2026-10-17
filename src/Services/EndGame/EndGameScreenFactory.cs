using System;
using CivOne.Graphics;
using CivOne.IO.Text;
using CivOne.Screens;
using CivOne.Screens.Dialogs;
using CivOne.Screens.Reports;

namespace CivOne.Services.EndGame
{
	/// <summary>
	/// Default <see cref="IEndGameScreenFactory"/> creating the real game screens.
	/// </summary>
	internal sealed class EndGameScreenFactory : IEndGameScreenFactory
	{
		/// <inheritdoc />
		public IScreen Victory() => new VictoryScreen();

		/// <inheritdoc />
		public IScreen Defeat() => new DefeatScreen();

		/// <inheritdoc />
		public IScreen SpaceVictory(SpaceVictoryText text, PalaceData? rivalBackground, Action? onLanding, TimeSpan? flightDuration) => rivalBackground == null
			? new SpaceVictory(text, Resources.Instance["SPACEST"], humanArrival: true, onLanding, flightDuration)
			: new SpaceVictory(text, PalaceView.CreatePicture(palaceData: rivalBackground), humanArrival: false, onLanding, flightDuration);

		/// <inheritdoc />
		public IScreen PowerGraph() => new Screens.PowerGraph();

		/// <inheritdoc />
		public IScreen CivilizationScore() => new Screens.Reports.CivilizationScore();

		/// <inheritdoc />
		public IScreen FamousPersons() => TopLeaderScreenFactory.Create();

		/// <inheritdoc />
		public IScreen HallOfFame() => HallOfFameScreenFactory.AddScore();

		/// <inheritdoc />
		public IScreen Credits() => new Screens.Credits();

		public IScreen Newspaper(string[] lines) => new Newspaper(null, lines);

		public IScreen Conquest() => new Screens.Conquest();

		public IKeepPlayingDialog KeepPlaying() => KeepPlayingDialog.FromGameText(TextFileFactory.Get().GetGameText("KING/FAME"));
	}
}
