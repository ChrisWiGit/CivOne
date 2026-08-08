using System;
using System.Collections.Generic;
using System.Linq;

namespace CivOne.Services.Screen
{
	/// <summary>
	/// Default <see cref="IPowerGraphSelectionService"/>, keeping the selection in memory for the running game.
	/// </summary>
	/// <remarks>
	/// The selection is rebuilt from the defaults as soon as the candidate list changes, which happens when
	/// another game is started or loaded.
	/// </remarks>
	internal sealed class PowerGraphSelectionService : IPowerGraphSelectionService
	{
		private const int MaxVisible = 12;
		private const int DefaultVisible = 7;

		private readonly HashSet<int> _selected = [];
		private int[] _initializedFor = [];

		/// <inheritdoc />
		public int MaxVisiblePlayers => MaxVisible;

		/// <inheritdoc />
		public int DefaultVisiblePlayers => DefaultVisible;

		/// <inheritdoc />
		public int SelectedCount => _selected.Count;

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

	/// <summary>
	/// Provides the <see cref="IPowerGraphSelectionService"/> used by the power graph.
	/// </summary>
	/// <remarks>
	/// The service outlives the power graph screen, so the selection survives closing and reopening the graph.
	/// </remarks>
	internal static class PowerGraphSelectionServiceFactory
	{
		private static IPowerGraphSelectionService? _current;

		/// <summary>
		/// The service instance shared by every power graph screen.
		/// </summary>
		public static IPowerGraphSelectionService Current => _current ??= new PowerGraphSelectionService();

		/// <summary>
		/// Replaces the shared instance, used by tests.
		/// </summary>
		/// <param name="service">The service to use from now on.</param>
		public static void SetCurrent(IPowerGraphSelectionService service) => _current = service;
	}
}
