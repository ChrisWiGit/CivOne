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
using CivOne.IO.Text;
using CivOne.Persistence.Factories;
using CivOne.Services;
using CivOne.Services.Random;
using CivOne.UserInterface;

namespace CivOne
{
	#pragma warning disable CA1822 // Mark members as static
	public abstract class BaseInstance
	{
		protected static Game Game => Game.Instance;
		protected static Map Map => Map.Instance;
		protected static Player Human => Game.Instance.HumanPlayer;
		protected static Resources Resources => Resources.Instance;
		protected static IRuntime Runtime => RuntimeHandler.Runtime;
		protected static Settings Settings => Settings.Instance;
		protected static MenuCollection GlobalMenus => MenuCollection.Instance;

		protected static ITranslationService Translation => TranslationServiceFactory.GetCurrent();
		protected static IRandomService RandomService => RandomServiceFactory.Create();

		protected static ILogger Logger => new RuntimeLogger();

		protected internal static void Log(string text, params object[] parameters) => Runtime.Log(text, parameters);
		protected static void PlaySound(string? filename)
		{
			if (string.IsNullOrEmpty(filename)) return;
			if (!Game.Started || !Game.Sound) return;
			if (Settings.Sound == GameOption.Off) return;
			if (!File.Exists(filename = filename.GetSoundFile())) return;

			Runtime.PlaySound(filename);
		}

		protected bool GFX256 => Settings.GraphicsMode == GraphicsMode.Graphics256;


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