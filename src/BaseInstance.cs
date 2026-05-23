// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.IO;
using CivOne.Enums;
using CivOne.Graphics;
using CivOne.IO;
using CivOne.IO.Text;
using CivOne.Services;
using CivOne.UserInterface;

namespace CivOne
{
	public abstract class BaseInstance
	{
		protected static Game Game => Game.Instance;
		protected static Map Map => Map.Instance;
		protected static Player Human => Game.Instance.HumanPlayer;
		protected static Resources Resources => Resources.Instance;
		protected static IRuntime Runtime => RuntimeHandler.Runtime;
		protected static Settings Settings => Settings.Instance;
		protected static MenuCollection Menus => MenuCollection.Instance;

		protected ITranslationService Translation => TranslationServiceFactory.GetCurrent();

		protected internal static void Log(string text, params object[] parameters) => Runtime.Log(text, parameters);
		protected static void PlaySound(string filename)
		{
			if (!(Game.Started && Game.Sound) || Settings.Sound == GameOption.Off || !File.Exists(filename = filename.GetSoundFile())) return;
			Runtime.PlaySound(filename);
		}

		protected bool GFX256 => (Settings.GraphicsMode == GraphicsMode.Graphics256);


		protected string Translate(string key)
		{
			return Translation.Translate(key);
		}

		protected string TranslateFormatted(string key, params object[] args)
		{
			return Translation.TranslateFormatted(key, args);
		}

		protected string[] TranslateArray(string key)
		{
			return Translation.TranslateArray(key);
		}

		protected string[] TranslateFormattedArray(string key, params object[] args)
		{
			return Translation.TranslateFormattedArray(key, args);
		}

		public string[] GetGameText(string key)
		{
			return TextFileFactory.Get().GetGameText(key);
		}
	}
}