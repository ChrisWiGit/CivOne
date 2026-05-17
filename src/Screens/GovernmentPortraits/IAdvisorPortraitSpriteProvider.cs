// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using CivOne.Graphics;

namespace CivOne.Screens.GovernmentPortraits
{
	enum AdvisorEra
	{
		Ancient,
		Modern
	}

	enum AdvisorGovernment
	{
		Despotism,
		Monarchy,
		Democracy,
		Communism
	}

	internal enum AdvisorPortraitSize
	{
		Small,
		Large
	}

	/// <summary>
	/// Provides portraits for the various advisors, based on their type, face, government and era.
	/// This is an uncoupled version of <see cref="CivOne.Enums.Advisor"/>, which is used by the various screens to request portraits without needing to know about the underlying logic of how portraits are determined.
	/// </summary>
	internal enum AdvisorType
	{
		MilitaryAdvisor,
		TradeAdvisor,
		ForeignAdvisor,
		ScienceAdvisor
	}

	internal enum AdvisorFace
	{
		Neutral,
		Grim,
		Happy
	}


	internal interface IAdvisorPortraitSpriteProvider
	{
		IBitmap GetPortrait(
			AdvisorType portraitType,
			AdvisorFace face = AdvisorFace.Neutral,
			AdvisorGovernment government = AdvisorGovernment.Democracy,
			AdvisorEra era = AdvisorEra.Modern,
			AdvisorPortraitSize size = AdvisorPortraitSize.Small);

		Palette Palette { get; }
	}
}