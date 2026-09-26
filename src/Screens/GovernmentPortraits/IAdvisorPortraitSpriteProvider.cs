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


	/// <summary>
	/// Provides portraits for the various advisors, based on their type, face, government and era.
	/// This keeps portrait lookup logic out of the consuming screens.
	/// </summary>
	internal interface IAdvisorPortraitSpriteProvider
	{
		IBitmap GetPortrait(
			AdvisorType portraitType,
			AdvisorFace face = AdvisorFace.Neutral,
			AdvisorGovernment government = AdvisorGovernment.Democracy,
			AdvisorEra era = AdvisorEra.Modern,
			AdvisorPortraitSize size = AdvisorPortraitSize.Small);
	}
}