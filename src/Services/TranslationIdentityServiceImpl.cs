// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;

namespace CivOne.Services
{

	/// <summary>
	/// Default translation service implementation that performs no translation and simply returns the input key or formatted string.
	/// This is used as a fallback when no actual translation service is provided, ensuring that the game can still function without localization while allowing for future integration of a proper translation service.
	/// </summary>
	public class TranslationIdentityServiceImpl : ITranslationService
	{
		public string Translate(string key)
		{
			return key;
		}

		public string TranslateFormatted(string key, params object[] args)
		{
			return string.Format(key, args);
		}
	}

	// Factory method to create the default translation service instance, allowing for easy replacement with a real translation service in the future.
	public static class TranslationServiceFactory
	{
		public static ITranslationService CreateDefault()
		{
			return new TranslationIdentityServiceImpl();
		}
	}
}