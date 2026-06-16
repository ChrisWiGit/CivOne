// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Drawing;
using System.Linq;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.Units;

namespace CivOne.Screens
{
	[ScreenResizeable]
	internal class UnitStack : BaseScreen
	{
		private const int WIDTH = 121;
		private const int UnitRowHeight = 16;
		private const int VerticalPadding = 3;
		private const int DialogHeightPadding = 6;

		private readonly IUnit[] _units;

		private bool _update = true;

		private int DialogHeight => (_units.Length * UnitRowHeight) + DialogHeightPadding;
		private int DialogLeft => (CanvasWidth - WIDTH) / 2;
		private int DialogTop => (CanvasHeight - DialogHeight) / 2;
		
		protected override bool HasUpdate(uint gameTick)
		{
			if (_update)
			{
				if (_units.Length == 0)
				{
					// No units, close the dialog
					Destroy();
					return true;
				}

				int left = DialogLeft;
				int top = DialogTop;
				int height = DialogHeight;

				IBitmap dialog = new Picture(WIDTH, height)
					.FillRectangle(1, 1, WIDTH - 2, height - 2, 3)
					.DrawRectangle3D();

				for (int i = 0; i < _units.Length; i++)
				{
					IUnit unit = _units[i];
					dialog.AddLayer(unit.ToBitmap(), 4, (i * 16) + 3)
						.DrawText(unit.TranslatedName + (unit.Veteran ? Translate(" (V)") : ""), 0, 15, 27, (i * 16) + 4)
						.DrawText(unit.Home == null ? Translate("NONE") : unit.Home.Name, 0, 14, 27, (i * 16) + 12);
				}

				this.FillRectangle(left - 1, top - 1, WIDTH + 2, height + 2, 5)
					.AddLayer(dialog, left, top);
				_update = false;
				
				return true;
			}
			return false;
		}

		protected override void Resize(int width, int height)
		{
			_update = true;
			base.Resize(width, height);
		}
		
		public override bool KeyDown(KeyboardEventArgs args)
		{
			Destroy();
			return true;
		}
		
		public bool MouseDown2(ScreenEventArgs args)
		{
			int left = DialogLeft;
			int top = DialogTop;
			int height = DialogHeight;

			if (args.X >= left && args.X < (left + WIDTH) && args.Y >= top && args.Y < (top + height))
			{
				int y = args.Y - top - VerticalPadding;
				int uid = (y - (y % UnitRowHeight)) / UnitRowHeight;
				if (uid < 0 || uid >= _units.Length)
				{
					_update = true;
					return true;
				}
				
				Game.ActiveUnit = _units[uid];
				_units[uid].Busy = false;
                    _units[uid].GotoDestination = Point.Empty; // fire-eggs 20190612 clear Goto
				_update = true;
				return true;
			}

			return true;
		}
		public override bool MouseDown(ScreenEventArgs args)
		{
			int dialogHeight = DialogHeight;
			int dialogLeft = DialogLeft;
			int dialogTop = DialogTop;

			var outOfBounds = args.X < dialogLeft || args.X >= dialogLeft + WIDTH || args.Y < dialogTop || args.Y >= dialogTop + dialogHeight;
			if (outOfBounds)
			{
				Destroy();
				return true;
			}

			// Ignore gaps between units for easier calculation.
			int relativeY = args.Y - dialogTop - VerticalPadding;
			int unitIndex = relativeY / UnitRowHeight;

			if (unitIndex < 0 || unitIndex >= _units.Length)
			{
				return true;
			}

			WakeAndSelectUnit(_units[unitIndex]);
			_update = true;
			return true;
		}

		private static void WakeAndSelectUnit(IUnit unit)
		{
			if (unit.Busy)
			{
				unit.Busy = false;
				// do not reset MovesLeft, otherwise a unit that has 
				// already moved would be able to move again after being selected from the stack
			}

			unit.GotoDestination = Point.Empty;
			Game.ActiveUnit = unit;
		}

		internal UnitStack(int x, int y) : base(MouseCursor.Pointer)
		{
			_units = Map[x, y].Units.Take(12).ToArray();

			Palette = Common.TopScreen!.Palette;
		}
	}
}