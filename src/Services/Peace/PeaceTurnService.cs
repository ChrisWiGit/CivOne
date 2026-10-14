namespace CivOne.Services.Peace
{
	/// <summary>
	/// Default <see cref="IPeaceTurnService"/> following the original game, with the deviations listed below.
	/// </summary>
	/// <remarks>
	/// <para>
	/// As in the original:
	/// a fight in which one side is the barbarians (slot 0) does not break the streak,
	/// and the counter only grows while the year is AD (<c>Year &gt; 0</c>).
	/// A reset and the increment of the same turn wrap both apply, so the turn after a fight already reads 1.
	/// </para>
	/// <para>
	/// Deliberate deviations from the original:
	/// </para>
	/// <list type="bullet">
	/// <item>
	/// The original resets the counter only inside its combat routine.
	/// A nuclear strike and the capture of an undefended city never run through it, so they keep the streak.
	/// This service resets on both, because both are plainly hostile acts.
	/// Callers report them through <see cref="RegisterCombat"/> like any fight.
	/// </item>
	/// <item>
	/// The original resets immediately during the attack.
	/// This service defers the reset to the next turn wrap, so the score shown during the rest of that turn
	/// still uses the old value.
	/// </item>
	/// </list>
	/// <para>
	/// A replacement service can restore the original behaviour.
	/// See <c>docs/plan-civilizationScore.impl.md</c>, §14.4, §15.7 and deviations 8, 9 and 11 in §8.
	/// </para>
	/// </remarks>
	public sealed class PeaceTurnService : IPeaceTurnService
	{
		private const int BarbarianPlayer = 0;

		private ushort _peaceTurns;
		private bool _resetPending;

		/// <inheritdoc />
		public ushort PeaceTurns => _peaceTurns;

		/// <inheritdoc />
		public void RegisterCombat(int attackerPlayer, int defenderPlayer)
		{
			if (attackerPlayer == BarbarianPlayer || defenderPlayer == BarbarianPlayer)
			{
				return;
			}

			_resetPending = true;
		}

		/// <inheritdoc />
		public void AdvanceTurn(int year)
		{
			if (_resetPending)
			{
				_peaceTurns = 0;
				_resetPending = false;
			}

			if (year > 0 && _peaceTurns < ushort.MaxValue)
			{
				_peaceTurns++;
			}
		}

		/// <inheritdoc />
		public void Restore(ushort peaceTurns)
		{
			_peaceTurns = peaceTurns;
			_resetPending = false;
		}
	}
}
