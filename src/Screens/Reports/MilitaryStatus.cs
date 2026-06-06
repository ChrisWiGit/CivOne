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
using CivOne.Graphics;
using CivOne.Units;

namespace CivOne.Screens.Reports
{
	[ScreenResizeable]
	internal class MilitaryStatus : BaseReport
	{
		private bool _update = true;

		private void Render()
		{
			this.Clear(1);
			DrawReportHeader();

			byte player = Game.PlayerNumber(Human);
			IUnit[] units = Game.GetUnits().Where(u => u.Owner == player && u.Home != null).ToArray();
			IUnit[] production = Game.GetCities().Where(c => c.CityOwnerPlayerIndex == player).Where(c => (c.CurrentProduction is IUnit)).Select(c => (c.CurrentProduction as IUnit)).ToArray();

			int i = 0;
			foreach (IUnit unit in Reflect.GetUnits())
			{
				if (!units.Any(u => u.Type == unit.Type) && !production.Any(u => u.Type == unit.Type)) continue;

				int active = units.Count(u => u.Type == unit.Type);
				int inProduction = production.Count(u => u.Type == unit.Type);

				int rowY = OffsetY + 30 + (i * 9);
				this.AddLayer(unit.ToBitmap(player, false), OffsetX + ((i % 2 == 0) ? 1 : 18), OffsetY + 27 + (9 * i))
					.FillRectangle(OffsetX + 36, rowY, 284, 1, 9)
					.DrawText(unit.TranslatedName, 0, 15, OffsetX + 36, OffsetY + 32 + (i * 9))
					.DrawText($"({unit.Attack}/{unit.Defense}/{unit.Move})", 0, 11, OffsetX + 112, OffsetY + 32 + (i * 9));
				if (active > 0)
					this.DrawText(TranslateFormatted("{0} active", active), 0, 15, OffsetX + 168, OffsetY + 32 + (i * 9));
				if (inProduction > 0)
					this.DrawText(TranslateFormatted("{0} in production", inProduction), 0, 11, OffsetX + 232, OffsetY + 32 + (i * 9));

				i++;
			}

			this.AddLayer(Portrait[(int)Advisor.Defense], OffsetX + 278, OffsetY + 2);
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

		public override string Title() => Translate("MILITARY STATUS");

		public MilitaryStatus() : base(1)
		{
			Render();
		}
	}
}