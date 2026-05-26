namespace CivOne.Screens.StartupWizard
{
	internal readonly record struct WizardActionResult(bool ShouldRefresh, bool ShouldClose = false);
}