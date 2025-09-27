namespace CivOne.Screens;

public interface ICityManager
{
	/// <summary>
	/// Redirects all inputs only to the specified screen.
	/// </summary>
	void SetActiveScreen(IScreen screen);

	/// <summary>
	/// Stops redirecting inputs to the active screen.
	/// </summary>
	void CloseActiveScreen();
}