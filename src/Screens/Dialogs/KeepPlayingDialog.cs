using System;
using System.Linq;
using CivOne.Graphics;

namespace CivOne.Screens.Dialogs
{
	/// <summary>
	/// Asks after the end of the game whether the player wants to keep playing, with the text of the original game
	/// (<c>KING.TXT</c>, section <c>*FAME</c>). Lines starting with <c>_</c> are the two choices.
	/// </summary>
	/// <remarks>
	/// Like the original, ESC counts as "No, I'm done".
	/// </remarks>
	internal sealed class KeepPlayingDialog : BaseDialog, IKeepPlayingDialog
	{
		private const int DialogLeft = 64;
		private const int DialogTop = 72;
		private const int DialogWidth = 231;
		private const int LineHeight = 8;
		private const int TextTop = 5;

		private readonly string[] _choices;
		private readonly int _menuTop;

		/// <inheritdoc />
		public bool KeepPlaying { get; private set; }

		private void MenuKeepPlaying(object? sender, EventArgs args)
		{
			KeepPlaying = true;
			Cancel();
		}

		protected override IMenu? CreateManagedMenu()
		{
			Menu menu = new(Palette, Selection(3, _menuTop, 228, _choices.Length * LineHeight))
			{
				X = DialogLeft + 3,
				Y = DialogTop + _menuTop,
				CenterTo320Coordinates = true,
				MenuWidth = 227,
				ActiveColour = 11,
				TextColour = 5,
				FontId = 0,
				Indent = 2
			};

			int index = 0;
			foreach (string choice in _choices)
			{
				menu.Items.Add(choice, index++);
			}

			menu.Items[0].Selected += Cancel;
			if (menu.Items.Count > 1)
			{
				menu.Items[1].Selected += MenuKeepPlaying;
			}

			menu.Cancel += Cancel;
			return menu;
		}

		private KeepPlayingDialog(string[] text, string[] choices)
			: base(DialogLeft, DialogTop, DialogWidth, TextTop + ((text.Length + choices.Length) * LineHeight) + 2)
		{
			_choices = choices;
			_menuTop = TextTop + (text.Length * LineHeight) - 1;
			for (int i = 0; i < text.Length; i++)
			{
				DialogBox.DrawText(text[i], 0, 15, 5, TextTop + (i * LineHeight));
			}
		}

		/// <summary>
		/// Creates the dialog from the <c>*FAME</c> text.
		/// </summary>
		/// <param name="lines">The lines of the section; <c>^</c> line ends are removed.</param>
		/// <returns>A new dialog.</returns>
		public static KeepPlayingDialog FromGameText(string[] lines)
		{
			ArgumentNullException.ThrowIfNull(lines);

			string[] cleaned = [.. lines.Select(line => line.Replace("^", "", StringComparison.Ordinal)).Where(line => line.Length > 0)];
			string[] choices = [.. cleaned.Where(line => line.StartsWith('_'))];
			string[] text = [.. cleaned.Where(line => !line.StartsWith('_'))];
			return new KeepPlayingDialog(text, choices.Length > 0 ? choices : ["_No, I'm done.", "_Yes, keep playing."]);
		}
	}
}
