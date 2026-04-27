// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.src;

namespace CivOne.Screens.Reports
{
	[ScreenResizeable]
	internal class IntelligenceReport : BaseReport
	{
		private readonly Dictionary<Player, Rectangle> _infoButtons = new Dictionary<Player, Rectangle>();
		private bool _showDetails;
		private Player _selectedPlayer;

		private void RenderOverview()
		{
			this.Clear(1);
			DrawReportHeader();

			int yy = OffsetY + 30;
			foreach (Player player in Game.Players.Where(p => p != 0 && !p.IsDestroyed))
			{
				this.FillRectangle(OffsetX + 4, yy, 313, 1, 9);

				byte id = Game.PlayerNumber(player);
				byte colour = Common.ColourLight[id];
				if (player.IsHuman || Human.HasEmbassy(player))
				{
					int unitCount = Game.GetUnits().Count(u => u.Owner == id && u.Home != null);

					this.DrawText($"{player.TribeNamePlural}: {player.LeaderName}", 0, 5, OffsetX + 8, yy + 3)
						.DrawText($"{player.TribeNamePlural}: {player.LeaderName}", 0, 15, OffsetX + 8, yy + 2)
						.DrawText($"{player.Government.Name}, {player.Gold}$, {unitCount} Units.", 0, colour, OffsetX + 160, yy + 2);

					if (!player.IsHuman)
					{
						this.DrawButton($"INFO{id}", 0, colour, Common.ColourDark[id], OffsetX + 281, yy + 14, 38, Resources.GetFontHeight(0) + 2);
						_infoButtons.Add(player, new Rectangle(OffsetX + 281, yy + 14, 38, Resources.GetFontHeight(0) + 2));
					}
				}
				else
				{
					this.DrawText("No embassy established.", 0, colour, OffsetX + 160, yy + 2, TextAlign.Center);
				}

				yy += 24;
			}
		}

		private void RenderDetails(Player player)
		{
			int y = OffsetY + 32;
			int fontHeight = Resources.GetFontHeight(0);

			this.FillRectangle(OffsetX, OffsetY + 25, 320, 172, BackgroundColour)
				.DrawText($"Subject: the {player.TribeNamePlural}", 0, 5, OffsetX + 16, y + 1)
				.DrawText($"Subject: the {player.TribeNamePlural}", 0, 15, OffsetX + 16, y)
				.DrawText("Leader:", 0, 9, OffsetX + 16, (y += fontHeight + 4))
				.DrawText($"Emperor {player.LeaderName}", 0, 15, OffsetX + 62, y);

			foreach (string line in player.Civilization.Leader.Traits())
				this.DrawText(line, 0, 7, OffsetX + 24, (y += fontHeight));

			this.DrawText("Capital:", 0, 9, OffsetX + 16, (y += fontHeight + 4))
				.DrawText(player.GetCapitalName(), 0, 15, OffsetX + 63, y)
				.DrawText("Government:", 0, 9, OffsetX + 16, (y += fontHeight))
				.DrawText(player.Government.Name, 0, 15, OffsetX + 83, y)
				.DrawText("Treasury:", 0, 9, OffsetX + 16, (y += fontHeight))
				.DrawText($"{player.Gold}$", 0, 15, OffsetX + 73, y)
				.DrawText("Military:", 0, 9, OffsetX + 16, (y += fontHeight))
				.DrawText($"{Game.GetUnits().Count(x => player == x.Owner)} Units", 0, 15, OffsetX + 67, y)
				.DrawText("Foreign Affairs:", 0, 9, OffsetX + 16, (y += fontHeight + 4))
				.DrawText("Technologies:", 0, 9, OffsetX + 16, (y += fontHeight + 4));
		}

		private void MouseDown(object sender, ScreenEventArgs args)
		{
			if (_infoButtons.Count == 0) return;

			foreach (KeyValuePair<Player, Rectangle> infoButton in _infoButtons)
			{
				if (!infoButton.Value.Contains(args.X, args.Y)) continue;

				_selectedPlayer = infoButton.Key;
				_showDetails = true;
				args.Handled = true;
				SetUpdate();
			}

			if (args.Handled) _infoButtons.Clear();
		}

		protected override void Resize(int width, int height)
		{
			base.Resize(width, height);
			SetUpdate();
		}

		protected override bool HasUpdate(uint gameTick)
		{
			if (!base.HasUpdate(gameTick)) return false;

			_infoButtons.Clear();
			if (_showDetails && _selectedPlayer != null)
			{
				RenderDetails(_selectedPlayer);
			}
			else
			{
				RenderOverview();
			}
			return true;
		}

		public IntelligenceReport() : base("INTELLIGENCE REPORT", 1, MouseCursor.Pointer)
		{
			OnMouseDown += MouseDown;
			SetUpdate();
		}
	}
}