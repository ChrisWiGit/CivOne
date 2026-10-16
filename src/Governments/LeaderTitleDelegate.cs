using System;
using CivOne.Services;

namespace CivOne.Governments
{
	/// <summary>
	/// The leader's title per government, like the original game:
	/// Anarchy "Mr.", Despotism "Emperor", Monarchy "King", Communism "Comrade", Republic and Democracy "President".
	/// </summary>
	/// <remarks>
	/// Used by the "We Love the ... day" celebration and the retirement warning.
	/// </remarks>
	/// <param name="translation">Translates the titles.</param>
	internal sealed class LeaderTitleDelegate(ITranslationService translation)
	{
		private readonly ITranslationService _translation = translation ?? throw new ArgumentNullException(nameof(translation));

		/// <summary>
		/// Gets the translated title of a leader under <paramref name="government"/>.
		/// </summary>
		/// <param name="government">The government; <c>null</c> gives the default title "President".</param>
		/// <returns>The translated title.</returns>
		public string Title(IGovernment? government) => government switch
		{
			Anarchy => _translation.Translate("Mr."),
			Despotism => _translation.Translate("Emperor"),
			Monarchy => _translation.Translate("King"),
			Communism => _translation.Translate("Comrade"),
			_ => _translation.Translate("President"),
		};
	}
}
