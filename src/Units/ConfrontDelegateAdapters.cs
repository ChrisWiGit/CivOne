using CivOne.Services;

namespace CivOne.Units;

/// <summary>
/// Adapter for game services used in confrontation logic.
/// Delegates to static Game instance for backward compatibility.
/// </summary>
internal sealed class ConfrontGameServicesAdapter : IConfrontGameServices
{
	public void RegisterHostileAction() => Game.Instance.RegisterHostileAction();
	public Player GetPlayer(byte owner) => Game.Instance.GetPlayer(owner);
	public bool IsAtWar(Player playerA, Player playerB) => playerA.IsAtWar(playerB);
	public int Difficulty => Game.Instance.Difficulty;
	public bool Started => Game.Started;
}