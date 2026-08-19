using System;
using System.Collections.Generic;
using System.Linq;
using CivOne.Civilizations;
using CivOne.Services;

namespace CivOne.Screens.NewGamePanels
{
	/// <summary>
	/// Holds the rules of the new game setup: how the opponent count offered by the menus maps to the
	/// player count a game is created with, which civilizations may be picked for a given opponent count,
	/// and which difficulty levels are offered.
	///
	/// The class is free of screen and rendering concerns, so the rules can be verified without a
	/// running game engine.
	/// </summary>
	internal class NewGameRulesDelegate
	{
		/// <summary>
		/// Highest civilization id of the original game.
		/// Everything above is an addition of this project and only offered for larger games.
		/// </summary>
		public const int ClassicCivilizationMaxId = 14;

		/// <summary>
		/// Largest opponent count that is still considered a classic sized game.
		/// </summary>
		public const int ClassicMaxOpponents = 6;

		private readonly ICivilization[]? _civilizations;
		private readonly Func<bool>? _deityEnabled;
		private readonly ITranslationService? _translationService;

		/// <summary>
		/// Creates the rules for a new game setup.
		/// </summary>
		/// <param name="civilizations">All known civilizations. Falls back to <see cref="Common.Civilizations"/>.</param>
		/// <param name="deityEnabled">Tells whether the deity difficulty is unlocked. Falls back to the game settings.</param>
		/// <param name="translationService">Translation service used for the menu texts. Falls back to the active service.</param>
		public NewGameRulesDelegate(ICivilization[]? civilizations = null, Func<bool>? deityEnabled = null, ITranslationService? translationService = null)
		{
			_civilizations = civilizations;
			_deityEnabled = deityEnabled;
			_translationService = translationService;
		}

		/// <summary>
		/// All known civilizations.
		/// Resolved on first use so a plain construction never touches the game singletons.
		/// </summary>
		protected virtual ICivilization[] Civilizations => _civilizations ?? Common.Civilizations;

		/// <summary>
		/// Translation service used for menu texts.
		/// </summary>
		protected virtual ITranslationService Translation => _translationService ?? TranslationServiceFactory.GetCurrent();

		/// <summary>
		/// Tells whether the deity difficulty is unlocked.
		/// </summary>
		protected virtual bool DeityEnabled => _deityEnabled?.Invoke() ?? Settings.Instance.DeityEnabled;

		/// <summary>
		/// Lowest opponent count a game can be created with.
		/// </summary>
		public virtual int MinOpponents => Game.MinCompetition - 1;

		/// <summary>
		/// Highest opponent count a game can be created with.
		/// </summary>
		public virtual int MaxOpponents => Game.MaxCompetition - 1;

		/// <summary>
		/// Lowest opponent count offered on the first competition menu page.
		/// </summary>
		public virtual int MainMenuMinOpponents => 2;

		/// <summary>
		/// Highest opponent count offered on the first competition menu page.
		/// </summary>
		public virtual int MainMenuMaxOpponents => ClassicMaxOpponents;

		/// <summary>
		/// Number of opponent counts that can be offered in total.
		/// </summary>
		public virtual int OpponentCount => MaxOpponents - MinOpponents + 1;

		/// <summary>
		/// Converts the number of opponents selected in the menus into the number of non-barbarian
		/// players <see cref="Game.CreateGame"/> expects: the human player plus the opponents.
		/// The barbarians always get their own slot on top and are not part of this count.
		/// </summary>
		/// <param name="opponents">The number of AI opponents.</param>
		/// <returns>The number of non-barbarian players, clamped to the range a game can be created with.</returns>
		public virtual int OpponentsToCompetition(int opponents)
		{
			return Math.Clamp(opponents + 1, Game.MinCompetition, Game.MaxCompetition);
		}

		/// <summary>
		/// Tells whether a civilization was added by this project instead of the original game.
		/// </summary>
		/// <param name="civilization">The civilization to check.</param>
		/// <returns><see langword="true"/> when the civilization is not part of the original game.</returns>
		public virtual bool IsExtendedCivilization(ICivilization civilization)
		{
			ArgumentNullException.ThrowIfNull(civilization);

			return civilization.Id > ClassicCivilizationMaxId;
		}

		/// <summary>
		/// Returns the civilizations that may be picked for the given number of opponents.
		/// Classic sized games (up to <see cref="ClassicMaxOpponents"/> opponents) only offer the
		/// civilizations of the original game, larger games offer the full list.
		/// The classic civilizations always come first, each group ordered by id.
		/// </summary>
		/// <param name="opponents">The number of AI opponents.</param>
		/// <returns>The selectable civilizations, in menu order.</returns>
		public virtual ICivilization[] GetSelectableCivilizations(int opponents)
		{
			IEnumerable<ICivilization> selectable = Civilizations
				.Where(c => c.PreferredPlayerNumber > 0);

			if (opponents <= ClassicMaxOpponents)
			{
				selectable = selectable.Where(c => c.Id <= ClassicCivilizationMaxId);
			}

			return [.. selectable
				.OrderBy(IsExtendedCivilization)
				.ThenBy(c => c.Id)];
		}

		/// <summary>
		/// Builds the entries of the difficulty menu.
		/// The toughest level depends on whether the deity difficulty is unlocked.
		/// </summary>
		/// <returns>The difficulty menu entries, from easiest to toughest.</returns>
		public virtual string[] BuildDifficultyMenuItems()
		{
			string easiest = Translation.TranslateFormatted("{0} (easiest)", Common.DifficultyName(0));
			string toughestEnabled = Translation.TranslateFormatted("{0} (toughest)", Common.DifficultyName(5));
			string toughestDefault = Translation.TranslateFormatted("{0} (toughest)", Common.DifficultyName(4));

			if (DeityEnabled)
			{
				return [
					easiest,
					Common.DifficultyName(1),
					Common.DifficultyName(2),
					Common.DifficultyName(3),
					Common.DifficultyName(4),
					toughestEnabled
				];
			}

			return [
				easiest,
				Common.DifficultyName(1),
				Common.DifficultyName(2),
				Common.DifficultyName(3),
				toughestDefault
			];
		}

		/// <summary>
		/// Builds the menu text for an opponent count, showing the resulting number of civilizations.
		/// </summary>
		/// <param name="opponents">The number of AI opponents.</param>
		/// <returns>The translated menu text.</returns>
		public virtual string FormatCivilizationCount(int opponents)
		{
			return Translation.TranslateFormatted("{0} Civilizations", OpponentsToCompetition(opponents));
		}
	}
}
