// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.


using System.Linq;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.Persistence.Game;
using CivOne.Units;

namespace CivOne.Screens.Reports
{
	[ScreenResizeable]
	internal class MilitaryLosses : BaseReport
	{
		private bool _update = true;

		private void Render()
		{
			this.Clear(4);
			DrawReportHeader();

			byte player = Game.PlayerNumber(Human);
			IUnit[] units = Reflect.GetUnits().ToArray();
			ushort[] losses = ((IPlayer)Human).UnitsLost;

			int index = 0;
			int columns = 2;
			int columnWidth = 156;
			foreach (IUnit unit in units)
			{
				if (index >= losses.Length) break;
				ushort count = losses[index++];
				if (count == 0) continue;

				int column = (index - 1) / 14;
				int row = (index - 1) % 14;
				if (column >= columns) break;

				int x = OffsetX + 8 + (column * columnWidth);
				int y = OffsetY + 32 + (row * 11);

				this.AddLayer(unit.ToBitmap(player, false), x, y)
					.DrawText(unit.Name, 0, 15, x + 16, y + 1)
					.DrawText(count.ToString(), 0, 11, x + 138, y + 1, TextAlign.Right);
			}
		}

		protected override bool HasUpdate(uint gameTick)
		{
			if (!_update) return false;

			Render();
			_update = false;
			return true;
		}

		protected override void Resize(int width, int height)
		{
			base.Resize(width, height);
			_update = true;
		}

		public override string Title() => Translate("MILITARY LOSSES");

		public MilitaryLosses() : base(4)
		{
			Render();
		}
	}
}