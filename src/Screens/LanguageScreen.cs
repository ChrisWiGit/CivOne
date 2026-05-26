// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Collections.Generic;
using System.Linq;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.Screens.Debug;
using CivOne.Services;
using CivOne.Services.Translation;
using CivOne.Tasks;

namespace CivOne.Screens
{
	/// <summary>
	/// Screen for selecting the active game language.
	///
	/// Can be opened from GameOptions (in-game) or Credits (main menu).
	/// After selection the screen destroys itself; the calling screen stays on the stack.
	/// </summary>
	[ScreenResizeable]
	internal class LanguageScreen : BaseScreen
	{
		private readonly string[] _postfixes;
		private readonly string[] _labels;
		private readonly int _defaultIndex;

		private GridMenuDelegate _grid;

		private void OnItemSelected(int index)
		{
			if (index < 0 || index >= _postfixes.Length) return;
			ApplyLanguage(_postfixes[index]);
			Destroy();
		}

		private void OnCancelled(object sender, EventArgs args)
		{
			Destroy();
		}

		private void ApplyLanguage(string postfix)
		{
			if (string.IsNullOrEmpty(postfix))
			{
				Settings.LanguagePostfix = string.Empty;
				TranslationServiceFactory.UseIdentity();
				NotifyIfInGame(Translate("Original (English)"));
				return;
			}

			if (!TranslationServiceFactory.TryUseLanguage(Runtime.StorageDirectory, postfix, out string error, message => Log(message)))
			{
				Log("Could not activate language '{0}': {1}", postfix, error);
				if (Game.Started)
				{
					GameTask.Insert(Message.Error(
						Translate("Language"),
						TranslateFormatted("Could not load language '{0}'.", postfix),
						error));
				}
				return;
			}

			Settings.LanguagePostfix = postfix;
			NotifyIfInGame(Translate(postfix));
		}

		private void NotifyIfInGame(string languageName)
		{
			if (!Game.Started) return;
			GameTask.Insert(Message.General(
				TranslateFormattedArray("Language switched to {0}.\nRestart the game or load a save game\nto apply all UI text changes.", languageName)
			));
		}

		private GridMenuDelegate CreateGrid()
		{
			string active = TranslationServiceFactory.ActiveLanguagePostfix ?? string.Empty;
			bool isChecked(int i) =>
				string.Equals(_postfixes[i], active, StringComparison.Ordinal);

			return new GridMenuDelegate(
				_labels,
				GridMenuDelegate.SelectionMode.Select,
				isChecked: isChecked,
				fontId: 0,
				defaultSelectedIndex: _defaultIndex);
		}

		protected override bool HasUpdate(uint gameTick)
		{
			if (!RefreshNeeded()) return false;

			if (_grid == null)
			{
				_grid = CreateGrid();
				_grid.ItemSelected += OnItemSelected;
				_grid.Cancelled += OnCancelled;
			}

			_grid.Draw(this, Translate("Change language..."), CanvasHeight);
			return true;
		}

		public override bool KeyDown(KeyboardEventArgs args)
		{
			if (_grid == null) return false;
			bool handled = _grid.KeyDown(args);
			if (handled) Refresh();
			return handled;
		}

		public override bool MouseDown(ScreenEventArgs args)
		{
			if (_grid == null) return false;
			bool handled = _grid.MouseDown(args.X, args.Y);
			if (handled) Refresh();
			return handled;
		}

		private static (string[] postfixes, string[] labels, int defaultIndex) BuildLanguageData(
			string storageDirectory, Func<string, string> translate, Action<string> log)
		{
			IReadOnlyList<TranslationLanguageInfo> languages =
				TranslationServiceFactory.GetAvailableLanguages(storageDirectory, log);

			string active = TranslationServiceFactory.ActiveLanguagePostfix ?? string.Empty;

			var postfixes = new List<string> { string.Empty };
			var labels = new List<string> { translate("Original (default)") };

			foreach (TranslationLanguageInfo language in languages)
			{
				postfixes.Add(language.Postfix);
				labels.Add(TranslationServiceFactory.GetLanguageDisplayName(language, translate));
			}

			int defaultIndex = postfixes.FindIndex(p =>
				string.Equals(p, active, StringComparison.Ordinal));
			if (defaultIndex < 0) defaultIndex = 0;

			return ([.. postfixes], [.. labels], defaultIndex);
		}

		/// <summary>Initialises the language selection screen.</summary>
		public LanguageScreen() : base(MouseCursor.Pointer)
		{
			using var defaultPalette = Common.DefaultPalette;
			Palette = defaultPalette;

			(_postfixes, _labels, _defaultIndex) =
				BuildLanguageData(Runtime.StorageDirectory, Translate, message => Log(message));
		}
	}
}
