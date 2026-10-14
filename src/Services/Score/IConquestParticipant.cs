namespace CivOne.Services.Score
{
	/// <summary>
	/// Survival state of one civilization slot, as read by the <see cref="WorldConquestDelegate"/>.
	/// </summary>
	public interface IConquestParticipant
	{
		/// <summary>
		/// Gets whether this slot is controlled by the human player.
		/// </summary>
		bool IsHuman { get; }

		/// <summary>
		/// Gets whether this slot holds the barbarians.
		/// Barbarians never count as a rival.
		/// </summary>
		bool IsBarbarian { get; }

		/// <summary>
		/// Gets whether this civilization has been destroyed.
		/// </summary>
		bool IsDestroyed { get; }
	}
}
