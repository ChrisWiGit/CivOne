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
using CivOne.Advances;
using CivOne.Enums;
using CivOne.Graphics;

namespace CivOne.Screens.Reports
{
	[ScreenResizeable]
	internal class ScienceReport : BaseReport
	{
		private bool _update = true;

		private void Render()
		{
			this.Clear(1)
				.FillRectangle(OffsetX, OffsetY, 320, 200, 1);
			DrawReportHeader();

			double width = 8;
			while ((width * Human.ScienceCost) > 200 || width <= 0.1)
			{
				width -= 0.1;
			}

			int barWidth = (int)Math.Ceiling(width * Human.ScienceCost);
			int barX = OffsetX + ((320 - barWidth) / 2);
			this.FillRectangle(barX, OffsetY + 25, barWidth, 16, 9);

			if (Human.CurrentResearch != null)
			{
				string researching = TranslateFormatted("Researching {0}", Human.CurrentResearch.TranslatedName);
				this.DrawText(researching, 0, 5, OffsetX + 160, OffsetY + 26, TextAlign.Center)
					.DrawText(researching, 0, 15, OffsetX + 159, OffsetY + 26, TextAlign.Center);

				int xx = -1;
				for (int i = 0; i < Human.Science; i++)
				{
					if (xx == (int)Math.Floor((width * i) + barX - 1)) continue;
					xx = (int)Math.Floor((width * i) + barX - 1);
					this.AddLayer(Icons.Science, xx, OffsetY + 32);
				}
			}

			int c = 0;
			foreach (IAdvance advance in Human.Advances.OrderBy(a => a.Id))
			{
				bool first = Game.GetAdvanceOrigin(advance, Human);
				int xx = OffsetX + 8 + ((c % 3) * 100);
				int yy = OffsetY + 42 + (((c - (c % 3)) / 3) * 7);
				this.DrawText(advance.TranslatedName, 0, (byte)(first ? 15 : 11), xx, yy);
				c++;
			}

			if (barWidth > 205)
			{
				return;
			}
			this.AddLayer(Portrait[(int)Advisor.Science], OffsetX + 278, OffsetY + 2);
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

		public override string Title() => Translate("SCIENCE REPORT");

		public ScienceReport() : base(1)
		{
			Render();
			_update = false;
		}
	}
}