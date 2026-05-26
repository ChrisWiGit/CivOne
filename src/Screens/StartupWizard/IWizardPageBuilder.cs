namespace CivOne.Screens.StartupWizard
{
	/// <summary>
	/// Builds the <see cref="WizardPage"/> that corresponds to the current state of a
	/// <see cref="WizardState"/>.
	/// </summary>
	/// <remarks>
	/// Each call to <see cref="Build"/> returns a freshly constructed, immutable page
	/// description (title, body lines, entries, links) for the page index stored in
	/// <see cref="WizardState.PageIndex"/>.
	/// The caller is responsible for re-invoking <see cref="Build"/> whenever the engine
	/// state changes so that the rendered page stays in sync.
	/// </remarks>
	internal interface IWizardPageBuilder
	{
		/// <summary>
		/// Builds and returns the <see cref="WizardPage"/> for the current wizard state.
		/// </summary>
		/// <param name="engine">
		/// The <see cref="WizardState"/> whose <see cref="WizardState.PageIndex"/> and
		/// other state properties determine which page is constructed.
		/// </param>
		/// <returns>
		/// A <see cref="WizardPage"/> populated with the title, body lines, selectable
		/// entries and optional footer links appropriate for the current page.
		/// </returns>
		WizardPage Build(WizardState engine);
	}
}