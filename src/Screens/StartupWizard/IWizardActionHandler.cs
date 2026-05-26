namespace CivOne.Screens.StartupWizard
{
	internal interface IWizardActionHandler
	{
		WizardActionResult Execute(WizardEntry entry, WizardEngine engine);

		WizardActionResult OpenUrl(string url, WizardEngine engine);
	}
}