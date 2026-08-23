namespace CivTranslateInteractive;

public interface IInteractiveConsole
{
	void WriteLine(string message);
	string? ReadLine();
}

public sealed class SystemInteractiveConsole : IInteractiveConsole
{
	public void WriteLine(string message) => Console.WriteLine(message);

	public string? ReadLine() => Console.ReadLine();
}
