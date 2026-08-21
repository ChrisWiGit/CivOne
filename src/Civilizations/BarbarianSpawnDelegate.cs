using System;
using CivOne.Enums;
using CivOne.Services.Random;

namespace CivOne.Civilizations
{
	/// <summary>
	/// Decides whether barbarians appear in the current turn, and which kind.
	///
	/// This is the single place that combines the two questions that used to sit apart: the spawn rhythm
	/// of the barbarians (<see cref="Barbarian.IsSpawnTurn"/>) and the barbarian sources a game allows
	/// (<see cref="BarbarianActivity"/>).
	/// The caller only reacts to the answer and places the units.
	/// </summary>
	internal class BarbarianSpawnDelegate
	{
		private readonly Func<BarbarianActivity> _activity;
		private readonly IRandomService? _randomService;
		private readonly Func<bool>? _isSpawnTurn;

		/// <summary>
		/// Creates the spawn delegate.
		/// </summary>
		/// <param name="activity">Reads the barbarian sources the running game allows.</param>
		/// <param name="randomService">Random source used to pick land or sea. Falls back to the shared service.</param>
		/// <param name="isSpawnTurn">Tells whether the current turn is a spawn turn. Falls back to the barbarian rules.</param>
		public BarbarianSpawnDelegate(Func<BarbarianActivity> activity, IRandomService? randomService = null, Func<bool>? isSpawnTurn = null)
		{
			ArgumentNullException.ThrowIfNull(activity);

			_activity = activity;
			_randomService = randomService;
			_isSpawnTurn = isSpawnTurn;
		}

		/// <summary>
		/// The barbarian sources the running game allows.
		/// </summary>
		protected virtual BarbarianActivity Activity => _activity();

		/// <summary>
		/// Random source used to pick land or sea.
		/// </summary>
		protected virtual IRandomService RandomService => _randomService ?? RandomServiceFactory.Create();

		/// <summary>
		/// Tells whether the current turn is a spawn turn at all.
		/// </summary>
		protected virtual bool IsSpawnTurn => _isSpawnTurn?.Invoke() ?? Barbarian.IsSpawnTurn;

		/// <summary>
		/// Returns what should appear this turn.
		/// </summary>
		/// <returns>
		/// The kind of raiding party to place, or <see cref="BarbarianSpawnKind.None"/> when nothing
		/// appears this turn.
		/// </returns>
		/// <remarks>
		/// A spawn turn always draws once to pick between land and sea, even when the drawn side is
		/// switched off. Two reasons: the random sequence of a game then does not depend on the barbarian
		/// setting, and the disabled side does not hand its turns to the other one, which would let a
		/// single enabled side appear twice as often as in the original game.
		/// </remarks>
		public virtual BarbarianSpawnKind GetSpawnKind()
		{
			if (!IsSpawnTurn)
			{
				return BarbarianSpawnKind.None;
			}

			// KBR 20200927 use cdonges land spawn code
			// https://github.com/cdonges/CivOne/commit/e54fe9377030de625c51b674c0ecf29a335e0556
			// TODO land spawning and sea spawning as separate timing / acts
			bool land = RandomService.NextInt(100) > 50;
			BarbarianActivity activity = Activity;

			if (land)
			{
				return activity.HasFlag(BarbarianActivity.LandRaids) ? BarbarianSpawnKind.Land : BarbarianSpawnKind.None;
			}

			return activity.HasFlag(BarbarianActivity.SeaRaids) ? BarbarianSpawnKind.Sea : BarbarianSpawnKind.None;
		}
	}
}
