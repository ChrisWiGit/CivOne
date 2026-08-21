// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
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
		private const int DetailsMinWidth = 320;
		private const int DetailsMaxWidth = 420;
		private const int DetailsLabelX = 16;
		private const int DetailsValueMinX = 62;
		private const int DetailsValueGap = 8;
		private const int DetailsMinValueWidth = 150;

		private const int RowHeight = 24;
		private const int FirstRowY = 30;
		private const int MaxOpponentsPerPage = 6;
		private const int RowsPerPage = MaxOpponentsPerPage + 1;

		private const int HintY = 192;
		private const int HintGap = 6;
		private const int HintRightEdge = 276;

		private readonly Dictionary<Player, Rectangle> _infoButtons = new Dictionary<Player, Rectangle>();
		private readonly Dictionary<int, Player> _infoRows = [];
		private readonly PageNavigationDelegate _pages = new();
		private bool _showDetails;
		private bool _knownFirst;
		private bool _allEmbassies;
		private Player? _selectedPlayer;

		private static bool DebugMenuEnabled => Settings.DebugMenu || RuntimeHandler.Runtime.Settings.Get<bool>("debug") == true;

		/// <remarks>
		/// With <see cref="_knownFirst"/> the civilizations the player knows are moved to the front, so they are
		/// not buried behind pages of empty rows in a game with many civilizations.
		/// The order inside both groups stays the same, so a civilization does not jump around between rows.
		/// </remarks>
		private Player[] ReportedPlayers
		{
			get
			{
				IEnumerable<Player> players = Game.Players.Where(p => p != 0 && !p.IsDestroyed);
				if (_knownFirst)
				{
					players = players.OrderBy(player => IsKnown(player) ? 0 : 1);
				}
				return [.. players];
			}
		}

		private bool IsKnown(Player player) => _allEmbassies || player.IsHuman || Human.HasEmbassy(player);

		private static bool RequiresPaging(Player[] players) => players.Count(player => !player.IsHuman) > MaxOpponentsPerPage;

		private void RenderOverview()
		{
			this.Clear(1);
			DrawReportHeader();

			Player[] players = ReportedPlayers;
			_pages.SetItems(players.Length, RowsPerPage);

			int yy = OffsetY + FirstRowY;
			int row = 0;
			foreach (Player player in players.Skip(_pages.FirstItemIndex).Take(_pages.PageSize))
			{
				DrawPlayerRow(player, yy, ++row);
				yy += RowHeight;
			}

			DrawHints(players);
		}

		/// <remarks>
		/// The hints are placed by their measured width so they never overlap, no matter how long a translation is.
		/// The line stops at <see cref="HintRightEdge"/>, because the last row of a full page keeps its info button
		/// to the right of it.
		/// </remarks>
		private void DrawHints(Player[] players)
		{
			int x = OffsetX + 8;
			x += DrawHint(Translate("F1: Known first"), x) + HintGap;

			if (DebugMenuEnabled)
			{
				x += DrawHint(Translate("F2: Embassies"), x) + HintGap;
			}

			if (!RequiresPaging(players))
			{
				return;
			}

			string pageHint = TranslateFormatted("PgUp/PgDn: {0}/{1}", _pages.CurrentPage + 1, _pages.PageCount);
			int rightAligned = OffsetX + HintRightEdge - Resources.GetTextSize(0, pageHint).Width;
			DrawHint(pageHint, Math.Max(x, rightAligned));
		}

		private int DrawHint(string text, int x)
		{
			this.DrawText(text, 0, 15, x, OffsetY + HintY);
			return Resources.GetTextSize(0, text).Width;
		}

		/// <remarks>
		/// The info buttons are numbered by their position on the page, so the numbers stay between 1 and
		/// <see cref="RowsPerPage"/> and can be typed to open the leader details of that row.
		/// </remarks>
		private void DrawPlayerRow(Player player, int yy, int row)
		{
			this.FillRectangle(OffsetX + 4, yy, 313, 1, 9);

			byte id = Game.PlayerNumber(player);
			byte colour = Common.PlayerColourLight(id);
			if (IsKnown(player))
			{
				int unitCount = Game.GetUnits().Count(u => u.Owner == id && u.Home != null);
				string leaderLine = TranslateFormatted("{0}: {1}", player.TribeNamePlural, player.LeaderName);

				this.DrawText(leaderLine, 0, 5, OffsetX + 8, yy + 3)
					.DrawText(leaderLine, 0, 15, OffsetX + 8, yy + 2)
					.DrawText(TranslateFormatted("{0}, {1}$, {2} Units.", player.Government.TranslatedName, player.Gold, unitCount), 0, colour, OffsetX + 160, yy + 2);

				if (!player.IsHuman)
				{
					this.DrawButton($"INFO{row}", 0, colour, Common.PlayerColourDark(id), OffsetX + 281, yy + 14, 38, Resources.GetFontHeight(0) + 2);
					_infoButtons.Add(player, new Rectangle(OffsetX + 281, yy + 14, 38, Resources.GetFontHeight(0) + 2));
					_infoRows.Add(row, player);
				}
			}
			else
			{
				this.DrawText(Translate("No embassy established."), 0, colour, OffsetX + 160, yy + 2, TextAlign.Center);
			}
		}

		private void RenderDetails(Player player)
		{
			int detailsWidth = Math.Min(Math.Max(DetailsMinWidth, Width), DetailsMaxWidth);
			int detailsLeft = Math.Max(0, (Width - detailsWidth) / 2);
			int y = OffsetY + 32;
			int fontHeight = Resources.GetFontHeight(0);
			int labelX = detailsLeft + DetailsLabelX;

			string leaderLabel = Translate("Leader:");
			string capitalLabel = Translate("Capital:");
			string governmentLabel = Translate("Government:");
			string treasuryLabel = Translate("Treasury:");
			string militaryLabel = Translate("Military:");
			string foreignAffairsLabel = Translate("Foreign Affairs:");
			string technologiesLabel = Translate("Technologies:");

			int widestLabel = new[]
			{
				leaderLabel,
				capitalLabel,
				governmentLabel,
				treasuryLabel,
				militaryLabel,
				foreignAffairsLabel,
				technologiesLabel
			}.Max(label => Resources.GetTextSize(0, label).Width);
			int valueX = Math.Max(detailsLeft + DetailsValueMinX, labelX + widestLabel + DetailsValueGap);
			int maxValueX = detailsLeft + detailsWidth - DetailsMinValueWidth;
			valueX = Math.Min(valueX, maxValueX);

			// Covers the whole list area down to the bottom edge, including the hint line of the overview.
			this.FillRectangle(detailsLeft, OffsetY + 25, detailsWidth, 200 - 25, BackgroundColour)
				.DrawText(TranslateFormatted("Subject: the {0}", player.TribeNamePlural), 0, 5, detailsLeft + 16, y + 1)
				.DrawText(TranslateFormatted("Subject: the {0}", player.TribeNamePlural), 0, 15, detailsLeft + 16, y)
				.DrawText(leaderLabel, 0, 9, labelX, (y += fontHeight + 4))
				.DrawText(TranslateFormatted("Emperor {0}", player.LeaderName), 0, 15, valueX, y);

			foreach (string line in player.Civilization.Leader.Traits())
				this.DrawText(line, 0, 7, detailsLeft + 24, (y += fontHeight));

			this.DrawText(capitalLabel, 0, 9, labelX, (y += fontHeight + 4))
				.DrawText(player.GetCapitalName(), 0, 15, valueX, y)
				.DrawText(governmentLabel, 0, 9, labelX, (y += fontHeight))
				.DrawText(player.Government.TranslatedName, 0, 15, valueX, y)
				.DrawText(treasuryLabel, 0, 9, labelX, (y += fontHeight))
				.DrawText(TranslateFormatted("{0}$", player.Gold), 0, 15, valueX, y)
				.DrawText(militaryLabel, 0, 9, labelX, (y += fontHeight))
				.DrawText(TranslateFormatted("{0} Units", Game.GetUnits().Count(x => player == x.Owner)), 0, 15, valueX, y)
				.DrawText(foreignAffairsLabel, 0, 9, labelX, (y += fontHeight + 4))
				.DrawText(technologiesLabel, 0, 9, labelX, (y += fontHeight + 4))
				.DrawText(Translate("Any key: Back"), 0, 15, detailsLeft + 8, OffsetY + HintY);
		}

		private void MouseDown(object? _, ScreenEventArgs args)
		{
			ArgumentNullException.ThrowIfNull(args);

			if (_showDetails)
			{
				CloseDetails();
				args.Handled = true;
				return;
			}

			if (_infoButtons.Count == 0) return;

			foreach (KeyValuePair<Player, Rectangle> infoButton in _infoButtons)
			{
				if (!infoButton.Value.Contains(args.X, args.Y)) continue;

				OpenDetails(infoButton.Key);
				args.Handled = true;
			}

			if (args.Handled) _infoButtons.Clear();
		}

		private void OpenDetails(Player player)
		{
			_selectedPlayer = player;
			_showDetails = true;
			SetUpdate();
		}

		/// <remarks>
		/// Only the numbers of the info buttons on the current page open a leader, every other character falls
		/// through and closes the report.
		/// </remarks>
		private bool OpenDetails(char keyChar)
		{
			if (!_infoRows.TryGetValue(keyChar - '0', out Player? player))
			{
				return false;
			}

			OpenDetails(player);
			return true;
		}

		public override bool KeyDown(KeyboardEventArgs args)
		{
			ArgumentNullException.ThrowIfNull(args);

			// The details of a single civilization are a step into the report, so any key returns to the list
			// instead of closing the whole report.
			if (_showDetails)
			{
				CloseDetails();
				return true;
			}

			if (HandleOverviewKey(args))
			{
				return true;
			}

			return base.KeyDown(args);
		}

		private void CloseDetails()
		{
			_showDetails = false;
			_selectedPlayer = null;
			SetUpdate();
		}

		private bool HandleOverviewKey(KeyboardEventArgs args)
		{
			switch (args.Key)
			{
				case Key.PageUp:
				case Key.PageDown:
					return TurnPage(args.Key);
				case Key.Character:
					return OpenDetails(args.KeyChar);
				case Key.F1:
					_knownFirst = !_knownFirst;
					break;
				case Key.F2:
					if (!DebugMenuEnabled) return false;
					_allEmbassies = !_allEmbassies;
					break;
				default:
					return false;
			}

			// Both keys change which civilizations are at the front of the list, so the list starts over.
			_pages.First();
			SetUpdate();
			return true;
		}

		private bool TurnPage(Key key)
		{
			Player[] players = ReportedPlayers;
			if (!RequiresPaging(players))
			{
				return false;
			}

			_pages.SetItems(players.Length, RowsPerPage);
			if (key == Key.PageUp)
			{
				_pages.Previous();
			}
			else
			{
				_pages.Next();
			}

			SetUpdate();
			return true;
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
			_infoRows.Clear();
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

		public override string Title() => Translate("INTELLIGENCE REPORT");

		public IntelligenceReport() : base(1, MouseCursor.Pointer)
		{
			OnMouseDown += MouseDown;
			SetUpdate();
		}
	}
}