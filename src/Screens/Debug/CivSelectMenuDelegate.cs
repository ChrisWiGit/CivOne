using System;
using System.Linq;
using CivOne.Graphics;

namespace CivOne.Screens.Debug
{
	/// <summary>
	/// Player selection dialog for the debug screens.
	/// </summary>
	/// <remarks>
	/// Built on <see cref="GridMenuDelegate"/>, so the players are laid out in up to four columns and
	/// paged when they still do not fit.
	/// A single-column menu would grow past the bottom of the canvas once a game has more than about
	/// eight players, hiding the remaining ones.
	///
	/// The host screen owns the input: forward <see cref="GridMenuDelegate.KeyDown"/> and
	/// <see cref="GridMenuDelegate.MouseDown"/>, and call <see cref="Draw(IBitmap, int)"/> while drawing.
	/// </remarks>
	internal class CivSelectMenuDelegate : GridMenuDelegate
	{
		private const int MinDialogWidth = 136;

		private readonly Player[] _players;

		/// <summary>
		/// Raised when the user confirms a player.
		/// </summary>
		public event Action<Player>? PlayerSelected;

		/// <summary>
		/// Title drawn in the header row of the dialog.
		/// </summary>
		public string Title { get; }

		private static string[] GetLabels(Player[] players, Func<Player, string>? labelSelector)
		{
			ArgumentNullException.ThrowIfNull(players);
			Func<Player, string> selector = labelSelector ?? (player => player.TribeNamePlural);
			return [.. players.Select(selector)];
		}

		private static int GetDefaultIndex(Player[] players) => Array.FindIndex(players, player => player.IsHuman);

		/// <summary>
		/// Creates a selection dialog for the given players.
		/// </summary>
		/// <param name="players">Players to offer, in display order.</param>
		/// <param name="title">Dialog title, already translated by the caller.</param>
		/// <param name="labelSelector">Optional label builder; defaults to the plural tribe name.</param>
		public CivSelectMenuDelegate(Player[] players, string title, Func<Player, string>? labelSelector = null)
			: base(
				GetLabels(players, labelSelector),
				SelectionMode.Select,
				fontId: 0,
				defaultSelectedIndex: GetDefaultIndex(players),
				enableHotkeys: true,
				minDialogWidth: MinDialogWidth)
		{
			_players = players;
			Title = title;
			ItemSelected += OnItemSelected;
		}

		/// <summary>
		/// Creates a selection dialog for every player of the running game.
		/// </summary>
		/// <param name="title">Dialog title, already translated by the caller.</param>
		public CivSelectMenuDelegate(string title)
			: this([.. Game.Players], title)
		{
		}

		/// <summary>
		/// Draws the dialog using <see cref="Title"/>.
		/// </summary>
		/// <param name="target">Drawing target, typically the host screen.</param>
		/// <param name="canvasHeight">Runtime canvas height, used to decide how many rows fit.</param>
		public void Draw(IBitmap target, int canvasHeight) => Draw(target, Title, canvasHeight);

		private void OnItemSelected(int index)
		{
			if (index < 0 || index >= _players.Length)
			{
				return;
			}

			PlayerSelected?.Invoke(_players[index]);
		}
	}
}
