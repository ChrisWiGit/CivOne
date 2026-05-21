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
using System.Linq;
using CivOne.Civilizations;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;

namespace CivOne.Screens.Reports
{
	internal class Demographics : BaseScreen
	{
		private struct TableRow
		{
			public string Title;
			public string Value;
			public int Place;

			public string BetterCiv;

			public TableRow(string title, string value, int place, string betterCiv)
			{
				Title = title;
				Value = value;
				Place = place;
				BetterCiv = betterCiv;
			}

			public TableRow(string title, string value, int place) : this(title, value, place, null)
			{
			}
		}

		private readonly TextSettings _shadowText, _normalText, _betterCivText;
		private bool _update = true;

		private TableRow Population()
		{
			string value = "00,000";
			if (Human.Population > 0) value = Common.NumberSeperator(Human.Population);
			
			int rank = 1;
			Player[] players = Game.Players.Where(x => !(x.Civilization is Barbarian)).OrderByDescending(x => x.Population).ThenBy(x => Game.PlayerNumber(x)).ToArray();
			for (int i = 0; i < players.Length; i++)
			{
				if (Human != players[i]) continue;
				rank = i + 1;
				break;
			}

			return new TableRow(Translate("Population"), value, rank);
		}

		private TableRow Pollution()
		{
			string value = Human.Pollution > 0 ? TranslateFormatted("{0} tons/year", Human.Pollution) : Translate("00 tons/year");

			var players = Game.Players
				.Where(x => x.Civilization is not Barbarian)
				.ToArray();

			int rank = players
				.Count(p => p.Pollution < Human.Pollution) + 1;

			// Player outRanked = Human.Embassies.Length > 0
			Player outRanked = Game.Players.Count() > 0
				? players
					.Where(p => p != Human)
					.Where(p => p.Pollution < Human.Pollution)
					.OrderByDescending(p => p.Pollution)
					.ThenBy(p => Game.PlayerNumber(p))
					.FirstOrDefault()
				: null;
			string betterCiv = null;
			if (outRanked != null)
			{
				betterCiv = TranslateFormatted("({0}: {1} tons)", outRanked.TribeName, outRanked.Pollution == 0 ? "00" : outRanked.Pollution.ToString());
			}

			return new TableRow(Translate("Pollution"), value, rank, betterCiv);
		}


		private IEnumerable<TableRow> GetTable()
		{
			yield return new TableRow(Translate("Approval Rating"), Translate("(todo)%"), 999);
			yield return Population();
			yield return new TableRow(Translate("GNP"), Translate("(todo) million $"), 999);
			yield return new TableRow(Translate("Mfg. Goods"), Translate("(todo) Mtons"), 999);
			yield return new TableRow(Translate("Land Area"), Translate("(todo) sq.miles"), 999);
			yield return new TableRow(Translate("Literacy"), Translate("(todo)%"), 999);
			yield return new TableRow(Translate("Disease"), Translate("(todo)%"), 999);
			yield return Pollution();
			yield return new TableRow(Translate("Life expectancy"), Translate("(todo) years"), 999);
			yield return new TableRow(Translate("Family Size"), Translate("(todo) children"), 999);
			yield return new TableRow(Translate("Military Service"), Translate("(todo) years"), 999);
			yield return new TableRow(Translate("Annual Income"), Translate("(todo)$ per capita"), 999);
			yield return new TableRow(Translate("Productivity"), Translate("(todo)"), 999);
		}

		private string Ordinal(int number)
		{
			switch (number)
			{
				case 1: return TranslateFormatted("{0}st", number);
				case 2: return TranslateFormatted("{0}nd", number);
				case 3: return TranslateFormatted("{0}rd", number);
				default: return TranslateFormatted("{0}th", number);
			}
		}

		protected override bool HasUpdate(uint gameTick)
		{
			if (!_update) return false;

			int yy = 21;
			foreach (TableRow tableEntry in GetTable())
			{
				this.DrawRectangle(4, yy, 312, 1, 9)
					.DrawText($"{tableEntry.Title}:", 8, yy + 3, _shadowText)
					.DrawText(tableEntry.Value, 104, yy + 3, _normalText)
					.DrawText(Ordinal(tableEntry.Place), 192, yy + 3, _shadowText);
				if (tableEntry.BetterCiv != null)
				{
					this.DrawText(tableEntry.BetterCiv, 320 - 5, yy + 3, _betterCivText);
				}
				yy += 12;
			}

			_update = false;
			return true;
		}

		public override bool KeyDown(KeyboardEventArgs args)
		{
			Destroy();
			return true;
		}
		
		public override bool MouseDown(ScreenEventArgs args)
		{
			Destroy();
			return true;
		}

		public Demographics()
		{
			using var defaultPalette = Common.DefaultPalette;
			Palette = defaultPalette;

			_normalText = new TextSettings() { Colour = 15 };
			_shadowText = TextSettings.ShadowText(15, 5);
			_betterCivText = new TextSettings()
			{
				Colour = 11,
				Alignment = TextAlign.Right
			};
			this.Clear(1)
				.DrawText(TranslateFormatted("{0} Demographics", Human.TribeName), 0, 15, 160, 4, TextAlign.Center);
		}
	}
}