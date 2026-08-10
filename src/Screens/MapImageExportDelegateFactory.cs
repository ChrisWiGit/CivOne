using CivOne.Services;
using CivOne.Services.Maps;
using CivOne.Tasks;
using CivOne.Units;

namespace CivOne.Screens
{
	/// <summary>
	/// Composes <see cref="GamePlay.GamePlayExportMapImageDelegate"/> for every screen that offers the map image export.
	///
	/// Keeping the composition here means the gameplay menu and the debug menu always export with the
	/// same dependencies and, more importantly, with the same rule for how much of the world is drawn.
	/// </summary>
	internal static class MapImageExportDelegateFactory
	{
		/// <summary>
		/// Creates a map image export delegate.
		/// </summary>
		/// <param name="translationService">
		/// The translation service to use, or <see langword="null"/> to use the currently selected one.
		/// </param>
		/// <returns>A delegate ready to run the export dialog.</returns>
		public static GamePlay.GamePlayExportMapImageDelegate Create(ITranslationService? translationService = null)
		{
			ITranslationService translation = translationService ?? TranslationServiceFactory.GetCurrent();

			return new(
				translation,
				Settings.Instance,
				RuntimeHandler.Runtime,
				Map.Instance,
				Game.Instance,
				Game.Instance.SaveMetaDataService,
				new GameCalendarService(translation),
				MapImageExportServiceFactory.Create(),
				new GameTaskCommandQueueAdapter(),
				new MessageServiceAdapter(),
				new DirectoryService(),
				RevealsWholeWorld);
		}

		/// <summary>
		/// Determines whether the map is currently shown without fog of war.
		/// Mirrors the condition the game map itself uses when it renders, so the exported image matches
		/// what the player sees on screen.
		/// </summary>
		/// <returns><c>true</c> when the entire world is visible; otherwise <c>false</c>.</returns>
		private static bool RevealsWholeWorld()
			=> Settings.Instance.RevealWorld || Common.GamePlay?.IsTerrainEditorEnabled == true;
	}
}
