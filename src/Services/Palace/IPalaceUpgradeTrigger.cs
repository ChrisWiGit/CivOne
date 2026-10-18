namespace CivOne.Services.Palace
{
	internal interface IPalaceUpgradeTrigger
	{
		bool ShouldTrigger(IPlayerGameState player);
	}
}