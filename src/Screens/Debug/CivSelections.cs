// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Linq;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Tasks;

namespace CivOne.Screens.Debug
{
	[ScreenResizeable]
	internal sealed class CivSelections : BaseScreen
	{
		private readonly GridMenuDelegate _gridMenu;
		private readonly (string Label, Action Handler)[] _entries;

		public CivSelections() : base(MouseCursor.Pointer)
		{
			using var defaultPalette = Common.DefaultPalette;
			Palette = defaultPalette;

			_entries =
			[
				(Translate("Edit AI Selections"), OpenAiSelections),
				(Translate("Change Human Player"), OpenChangeHumanPlayer),
				(Translate("Back"), () => Destroy())
			];

			string[] labels = [.. _entries.Select(entry => entry.Label)];
			_gridMenu = new GridMenuDelegate(labels, GridMenuDelegate.SelectionMode.Select, fontId: 0, enableHotkeys: true);
			_gridMenu.ItemSelected += index => _entries[index].Handler();
			_gridMenu.Cancelled += (_, _) => Destroy();

			Refresh();
		}

		private void OpenAiSelections()
		{
			GameTask.Enqueue(Show.Screen(NewGameAiSelection.CreateInGameEditor()));
			Destroy();
		}

		private void OpenChangeHumanPlayer()
		{
			GameTask.Enqueue(Show.Screen<ChangeHumanPlayer>());
			Destroy();
		}

		public override bool KeyDown(KeyboardEventArgs args)
		{
			bool handled = _gridMenu.KeyDown(args);
			if (handled)
			{
				Refresh();
			}
			return handled;
		}

		public override bool MouseDown(ScreenEventArgs args)
		{
			bool handled = _gridMenu.MouseDown(args.X, args.Y);
			if (handled)
			{
				Refresh();
			}
			return handled;
		}

		protected override bool HasUpdate(uint gameTick)
		{
			if (!RefreshNeeded())
			{
				return false;
			}

			_gridMenu.Draw(this, Translate("Civ Selections"), CanvasHeight);
			return true;
		}
	}
}
