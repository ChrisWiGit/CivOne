// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.
using CivOne.Services;

namespace CivOne.IO.Text
{
	/// <summary>
	/// Entry point for game text access.
	/// Use this factory in game code instead of creating <see cref="TextFile"/> manually.
	/// </summary>
	/// <example>
	/// var lines = TextFileFactory.Get().GetGameText("HELP/HELP1");
	/// </example>
	internal static class TextFileFactory
	{
		private static ITextFileLoader _loader;
		private static TextFile _gameTexts;

		/// <summary>
		/// Returns the shared game text instance.
		/// </summary>
		/// <param name="reload">
		/// When true, forces a cache rebuild before returning.
		/// </param>
		/// <returns>
		/// Shared read-only game text accessor.
		/// </returns>
		public static IGameTexts Get(bool reload = false)
		{
			_loader ??= new TextFileLoader();

			if (_gameTexts == null)
			{
				_gameTexts = new(_loader);
				_gameTexts.Reset();
				TranslationServiceFactory.RegisterLanguageObserver(_gameTexts);
			}

			if (reload)
			{
				_gameTexts.Reset();
			}

			return _gameTexts;
		}

		/// <summary>
		/// Returns the shared loader instance used by the factory.
		/// </summary>
		/// <returns>
		/// Current loader instance.
		/// </returns>
		public static ITextFileLoader GetLoader()
		{
			_loader ??= new TextFileLoader();
			return _loader;
		}

		/// <summary>
		/// Convenience method for loading one raw text file.
		/// </summary>
		/// <param name="filename">
		/// Logical file name without extension.
		/// </param>
		/// <returns>
		/// Cleaned file lines.
		/// </returns>
		public static string[] LoadTextFile(string filename) => GetLoader().LoadArray(filename);

		/// <summary>
		/// Returns shared instance as concrete <see cref="TextFile"/> type.
		/// Intended for internal scenarios that need command behavior.
		/// </summary>
		/// <param name="reload">
		/// When true, forces a cache rebuild before returning.
		/// </param>
		/// <returns>
		/// Shared concrete instance.
		/// </returns>
		public static TextFile GetInstance(bool reload = false) => _ = Get(reload) as TextFile;

		/// <summary>
		/// Clears cached instances and unregisters language observer.
		/// Call before full resource reload.
		/// </summary>
		public static void ClearInstance()
		{
			TranslationServiceFactory.UnregisterLanguageObserver(_gameTexts);
			_gameTexts = null;
			_loader = null;
		}
	}
}