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
using System.Globalization;
using System.Linq;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.Graphics.Sprites;

namespace CivOne.Screens.Debug
{
	/// <summary>
	/// Debug screen listing every player slot with the civilization currently occupying it.
	///
	/// Makes the results of civilization assignment and respawn checkable in a running game: which
	/// civilization holds which slot, how the repeat names are numbered, how often a slot respawned, and which
	/// player colours are in use. The regular reports only fit about eight players, so they cannot answer this
	/// for a game with more.
	/// </summary>
	[ScreenResizeable]
	internal class PlayerSlotsScreen : BaseScreen
	{
		private const int FontId = 0;
		private const int FooterHeight = 10;
		private const int SwatchSize = 6;
		private const int TitleY = 2;

		/// <summary>Colour of the text in a row that still has units or cities.</summary>
		private const byte ColourAlive = 15;

		/// <summary>Colour of the text in a row without units and cities, and of the column headers.</summary>
		private const byte ColourDimmed = 5;

		private int _pageIndex;
		private bool _hasUpdate = true;

		private int OffsetX => Math.Max(0, (Width - 320) / 2);

		private static int RowHeight => Resources.GetFontHeight(FontId) + 1;

		// The title, the column headers and the list each get their own band, so nothing overlaps at any font
		// height. A taller header simply means fewer rows per page.
		private static int ColumnHeaderY => TitleY + RowHeight + 1;
		private static int ListTop => ColumnHeaderY + RowHeight + 2;

		private int ListHeight => Math.Max(RowHeight, Height - ListTop - FooterHeight - 2);
		private int RowsPerPage => Math.Max(1, ListHeight / RowHeight);

		/// <summary>
		/// One line of the list. Built once per redraw so the drawing code stays free of game lookups.
		/// </summary>
		private sealed record SlotRow(int Slot, string Leader, string Tribe, int CivilizationId, int Units, int Cities, int Respawns, bool IsHuman, bool IsBarbarian);

		private static List<SlotRow> BuildRows()
		{
			int[] respawnsBySlot = new int[Game.MaxPlayers];
			foreach (ReplayData.CivilizationRespawned respawn in Game.GetReplayData<ReplayData.CivilizationRespawned>())
			{
				if (respawn.PlayerId >= 0 && respawn.PlayerId < respawnsBySlot.Length)
				{
					respawnsBySlot[respawn.PlayerId]++;
				}
			}

			List<SlotRow> rows = [];
			foreach (Player player in Game.Players)
			{
				byte slot = Game.PlayerNumber(player);

				// Deliberately no Player.IsDestroyed here: that property runs HandleExtinction, which disbands
				// units and marks players destroyed. A debug view must not change the game it displays.
				int units = Game.GetUnits().Count(unit => unit.Owner == slot);
				int cities = Game.Cities.Count(city => city.CityOwnerPlayerIndex == slot);

				rows.Add(new SlotRow(
					slot,
					player.LeaderName,
					player.TribeNamePlural,
					player.Civilization.Id,
					units,
					cities,
					slot < respawnsBySlot.Length ? respawnsBySlot[slot] : 0,
					player.IsHuman,
					player.Civilization.PreferredPlayerNumber == 0));
			}

			return rows;
		}

		// Column positions inside the 320 pixel wide layout, measured from OffsetX.
		private const int ColumnSwatch = 4;
		private const int ColumnSlot = 14;
		private const int ColumnName = 34;
		private const int ColumnCivilization = 210;
		private const int ColumnUnits = 232;
		private const int ColumnCities = 252;
		private const int ColumnRespawns = 272;
		private const int ColumnFlag = 296;

		private void DrawScreen()
		{
			List<SlotRow> rows = BuildRows();
			int pageCount = Math.Max(1, (rows.Count + RowsPerPage - 1) / RowsPerPage);
			_pageIndex = Math.Clamp(_pageIndex, 0, pageCount - 1);

			int ox = OffsetX;

			this.Clear().Tile(Pattern.PanelGrey);
			this.DrawText(TranslateFormatted("Player Slots: {0} of {1}", rows.Count, Game.MaxPlayers), FontId, ColourAlive, ox + ColumnSwatch, TitleY);
			this.DrawText(TranslateFormatted("Page {0}/{1}", _pageIndex + 1, pageCount), FontId, ColourAlive, ox + ColumnCivilization, TitleY);

			int headerY = ColumnHeaderY;
			this.DrawText(Translate("Slot"), FontId, ColourDimmed, ox + ColumnSlot, headerY);
			this.DrawText(Translate("Leader / Tribe"), FontId, ColourDimmed, ox + ColumnName, headerY);
			this.DrawText(Translate("Civ"), FontId, ColourDimmed, ox + ColumnCivilization, headerY);
			this.DrawText(Translate("Uni"), FontId, ColourDimmed, ox + ColumnUnits, headerY);
			this.DrawText(Translate("Cit"), FontId, ColourDimmed, ox + ColumnCities, headerY);
			this.DrawText(Translate("Res"), FontId, ColourDimmed, ox + ColumnRespawns, headerY);

			int y = ListTop;
			foreach (SlotRow row in rows.Skip(_pageIndex * RowsPerPage).Take(RowsPerPage))
			{
				DrawRow(row, ox, y);
				y += RowHeight;
			}

			this.DrawText(Translate("PgUp/PgDn: page, ESC: close"), FontId, 5, ox + ColumnSwatch, Height - FooterHeight);
		}

		private void DrawRow(SlotRow row, int ox, int y)
		{
			// The swatch carries the player colour, not the text: a player colour may be light grey (slots 7, 15,
			// 23, 31), which is unreadable on the grey panel. The fill shows the light colour, which is the one
			// the map uses for the player; the border shows the dark colour, because only the (light, dark) pair
			// is unique once the light colours repeat every eight slots.
			this.FillRectangle(ox + ColumnSwatch, y, SwatchSize, SwatchSize, Common.PlayerColourLight(row.Slot));
			this.DrawRectangle(ox + ColumnSwatch, y, SwatchSize, SwatchSize, Common.PlayerColourDark(row.Slot));

			// Players without units and cities are greyed out, so a slot waiting for its respawn is easy to spot.
			bool alive = row.Units > 0 || row.Cities > 0;
			byte colour = alive ? ColourAlive : ColourDimmed;

			this.DrawText(Number(row.Slot), FontId, colour, ox + ColumnSlot, y);
			this.DrawText($"{row.Leader} / {row.Tribe}", FontId, colour, ox + ColumnName, y);
			this.DrawText(Number(row.CivilizationId), FontId, colour, ox + ColumnCivilization, y);
			this.DrawText(Number(row.Units), FontId, colour, ox + ColumnUnits, y);
			this.DrawText(Number(row.Cities), FontId, colour, ox + ColumnCities, y);
			this.DrawText(Number(row.Respawns), FontId, colour, ox + ColumnRespawns, y);

			// Single-letter markers stay untranslated on purpose: "H"/"B" would be ambiguous translation
			// keys, and this is a debug-only view.
			if (row.IsHuman)
			{
				this.DrawText("H", FontId, ColourAlive, ox + ColumnFlag, y);
			}
			else if (row.IsBarbarian)
			{
				this.DrawText("B", FontId, ColourAlive, ox + ColumnFlag, y);
			}
		}

		private static string Number(int value) => value.ToString(CultureInfo.CurrentCulture);

		private void ChangePage(int delta)
		{
			_pageIndex = Math.Max(0, _pageIndex + delta);
			_hasUpdate = true;
			Refresh();
		}

		protected override bool HasUpdate(uint gameTick)
		{
			if (!RefreshNeeded() && !_hasUpdate)
			{
				return false;
			}

			DrawScreen();
			_hasUpdate = false;
			return true;
		}

		public override bool KeyDown(KeyboardEventArgs args)
		{
			ArgumentNullException.ThrowIfNull(args);

			switch (args.Key)
			{
				case Key.Escape:
					Destroy();
					return true;
				case Key.PageDown:
				case Key.Down:
				case Key.Space:
					ChangePage(1);
					return true;
				case Key.PageUp:
				case Key.Up:
					ChangePage(-1);
					return true;
				default:
					return false;
			}
		}

		public override bool MouseDown(ScreenEventArgs args)
		{
			ChangePage(1);
			return true;
		}

		public PlayerSlotsScreen()
		{
			Palette = Common.DefaultPalette;
		}
	}
}
