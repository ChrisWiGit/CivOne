using System;
using System.Collections.Generic;
using System.Linq;

namespace CivOne.Services.Screen
{
	/// <summary>
	/// Default <see cref="IPowerGraphSelectionService"/>, keeping the selection in memory for the running game.
	/// </summary>
	/// <remarks>
	/// The selection is rebuilt from the defaults when <see cref="UseGame"/> reports a different game, and
	/// also when the candidate list of the current game changes.
	/// Comparing candidate lists alone is not enough: two different games can produce the same list of player
	/// numbers, and the selection of the previous one would then be applied to other civilizations.
	/// </remarks>
	internal sealed class PowerGraphSelectionService : IPowerGraphSelectionService
	{
		private const int MaxVisible = 12;
		private const int DefaultVisible = 7;

		private readonly HashSet<int> _selected = [];
		private int[] _initializedFor = [];
		private WeakReference? _gameIdentity;

		/// <inheritdoc />
		public int MaxVisiblePlayers => MaxVisible;

		/// <inheritdoc />
		public int DefaultVisiblePlayers => DefaultVisible;

		/// <inheritdoc />
		public int SelectedCount => _selected.Count;

		/// <inheritdoc />
		public void UseGame(object gameIdentity)
		{
			ArgumentNullException.ThrowIfNull(gameIdentity);

			if (_gameIdentity != null && 
				_gameIdentity.Target is object current && 
				ReferenceEquals(current, gameIdentity))
			{
				return;
			}

			_gameIdentity = new WeakReference(gameIdentity);
			_initializedFor = [];
			_selected.Clear();
		}

		/// <inheritdoc />
		public bool RequiresSelection(IReadOnlyList<int> candidates)
		{
			ArgumentNullException.ThrowIfNull(candidates);
			return candidates.Count > MaxVisible;
		}

		/// <inheritdoc />
		public IReadOnlyList<int> GetVisiblePlayers(IReadOnlyList<int> candidates, int humanPlayerNumber)
		{
			ArgumentNullException.ThrowIfNull(candidates);

			if (!RequiresSelection(candidates))
			{
				return candidates;
			}

			EnsureInitialized(candidates, humanPlayerNumber);
			return [.. candidates.Where(IsSelected).Take(MaxVisible)];
		}

		/// <inheritdoc />
		public bool IsSelected(int playerNumber) => _selected.Contains(playerNumber);

		/// <inheritdoc />
		public bool Toggle(int playerNumber)
		{
			if (_selected.Remove(playerNumber))
			{
				return true;
			}

			if (_selected.Count >= MaxVisible)
			{
				return false;
			}

			_selected.Add(playerNumber);
			return true;
		}

		private void EnsureInitialized(IReadOnlyList<int> candidates, int humanPlayerNumber)
		{
			if (_initializedFor.SequenceEqual(candidates))
			{
				return;
			}

			_initializedFor = [.. candidates];
			_selected.Clear();

			// The human player is always interesting, the rest of the default selection is filled up with the
			// first civilizations, which are the ones that still carry their own name without a Roman numeral.
			if (candidates.Contains(humanPlayerNumber))
			{
				_selected.Add(humanPlayerNumber);
			}

			foreach (int candidate in candidates)
			{
				if (_selected.Count >= DefaultVisible)
				{
					return;
				}
				_selected.Add(candidate);
			}
		}
	}
}
