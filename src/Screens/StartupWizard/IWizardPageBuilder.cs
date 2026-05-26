namespace CivOne.Screens.StartupWizard
{
	internal interface IWizardPageBuilder
	{
		WizardPage Build(WizardEngine engine);
	}
}