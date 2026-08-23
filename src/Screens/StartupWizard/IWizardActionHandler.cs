namespace CivOne.Screens.StartupWizard
{
	/// <summary>
	/// Handles user-initiated actions on a <see cref="WizardPage"/>.
	/// </summary>
	/// <remarks>
	/// Implementations process a <see cref="WizardEntry"/> that the user has activated
	/// (via keyboard hotkey or mouse click) and return a <see cref="WizardActionResult"/>
	/// that tells the wizard screen how to react (e.g. advance page, refresh, close).
	/// </remarks>
	internal interface IWizardActionHandler
	{
		/// <summary>
		/// Executes the action associated with the given <paramref name="entry"/>.
		/// </summary>
		/// <param name="entry">
		/// The activated <see cref="WizardEntry"/>. Must not be <see langword="null"/> and
		/// must have <see cref="WizardEntry.Enabled"/> set to <see langword="true"/>.
		/// </param>
		/// <param name="engine">
		/// The <see cref="WizardState"/> that holds the current wizard state
		/// (page index, selected language, data folder, sound setting, etc.).
		/// </param>
		/// <returns>
		/// A <see cref="WizardActionResult"/> describing what the wizard screen should do next.
		/// </returns>
		WizardActionResult Execute(WizardEntry entry, WizardState engine);

		/// <summary>
		/// Opens an external URL (e.g. a browser link from the wizard footer).
		/// </summary>
		/// <param name="url">The fully-qualified URL to open.</param>
		/// <param name="engine">
		/// The <see cref="WizardState"/> that holds the current wizard state.
		/// </param>
		/// <returns>
		/// A <see cref="WizardActionResult"/> describing what the wizard screen should do next.
		/// In most implementations this is a no-op result that keeps the current page.
		/// </returns>
		WizardActionResult OpenUrl(string url, WizardState engine);
	}
}