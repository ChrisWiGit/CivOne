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
using CivOne.Graphics;
using CivOne.Wonders;

namespace CivOne.Screens.Reports
{
	[Modal, ScreenResizeable]
	internal class TopCities : BaseScreen
	{
		private readonly City[] _cities;
		
		protected override bool HasUpdate(uint gameTick)
		{
			if (!RefreshNeeded())
			{
				return false;
			}

			DrawBackground();

			int offsetX = Math.Max(0, (Width - 320) / 2);
			int offsetY = Math.Max(0, (Height - 200) / 2);

			for (int i = 0; i < _cities.Length; i++)
			{
				City city = _cities[i];

				if (city == null || city.Size == 0) continue;
				byte colour = Common.ColourLight[city.Owner];

				int xx = offsetX + 8;
				int yy = offsetY + 32 + (32 * i);
				int ww = 304;
				int hh = 26;

				Player owner = Game.GetPlayer(city.Owner);

				this.FillRectangle(xx, yy, ww, hh, colour)
					.FillRectangle(xx + 1, yy + 1, ww - 2, hh - 2, 3);

				int rowInnerLeft = xx + 4;
				int rowInnerRight = xx + ww - 4;
				int rowInnerWidth = rowInnerRight - rowInnerLeft;

				ICityCitizenLayoutService layoutService = ICityCitizenLayoutService.Create(city);
				CitizenDrawInfo[] citizenInfos = [.. layoutService.EnumerateCitizens()];
				int citizensMinX = citizenInfos.Length > 0 ? citizenInfos.Min(c => c.X) : 0;
				int citizensMaxX = citizenInfos.Length > 0 ? citizenInfos.Max(c => c.X) : 0;
				const int citizenIconWidth = 16;
				int citizensWidth = citizenInfos.Length > 0 ? (citizensMaxX - citizensMinX + citizenIconWidth) : 0;

				IBitmap[] wonderIcons = [.. city.Wonders.Select(w => w.SmallIcon)];
				int wondersWidth = 0;
				for (int w = 0; w < wonderIcons.Length; w++)
				{
					if (w > 0) wondersWidth += 2;
					wondersWidth += wonderIcons[w].Bitmap.Width;
				}

				int spacing = (citizensWidth > 0 && wondersWidth > 0) ? 8 : 0;
				int targetCitizensWidth = citizensWidth;
				if (citizensWidth + spacing + wondersWidth > rowInnerWidth)
				{
					targetCitizensWidth = Math.Max(16, rowInnerWidth - wondersWidth - spacing);
				}

				int contentWidth = targetCitizensWidth + spacing + wondersWidth;
				double citizenScale = 1.0;
				if (citizensWidth > citizenIconWidth && targetCitizensWidth < citizensWidth)
				{
					citizenScale = (double)(targetCitizensWidth - citizenIconWidth) / (citizensWidth - citizenIconWidth);
				}

				int dx = Math.Max(rowInnerLeft, rowInnerLeft + ((rowInnerWidth - contentWidth) / 2));

				foreach (CitizenDrawInfo info in citizenInfos)
				{
					int normalizedX = info.X - citizensMinX;
					int compressedX = (int)Math.Round(normalizedX * citizenScale);
					this.AddLayer(Icons.Citizen(info.Citizen), dx + compressedX, yy + 10);
				}
				dx += targetCitizensWidth;

				dx += spacing;
				int rowRight = rowInnerRight;
				for (int w = 0; w < wonderIcons.Length; w++)
				{
					if (dx >= rowRight)
					{
						break;
					}

					IBitmap wonderIcon = wonderIcons[w];
					int drawWidth = Math.Min(wonderIcon.Bitmap.Width, rowRight - dx);
					if (drawWidth <= 0)
					{
						break;
					}

					if (drawWidth < wonderIcon.Bitmap.Width)
					{
						this.AddLayer(wonderIcon.Bitmap[0, 0, drawWidth, wonderIcon.Bitmap.Height], dx, yy + 11, dispose: true);
					}
					else
					{
						this.AddLayer(wonderIcon, dx, yy + 11);
					}

					dx += drawWidth + 2;
				}

				this.DrawText(TranslateFormatted("{0}. {1} ({2})", i + 1, city.Name, owner.Civilization.Name), 0, 15, offsetX + 160, yy + 3, TextAlign.Center);
			}

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

		protected override void Resize(int width, int height)
		{
			base.Resize(width, height);
			Refresh();
		}
		
		public TopCities()
		{
			using var defaultPalette = Common.DefaultPalette;
			Palette = defaultPalette;

			// I'm not sure about the order of top 5 cities, but this is pretty close
			_cities = [.. Game.GetCities()
				.Where(c => c.Size > 0)
				.Select(c => new
				{
					City = c,
					Citizens = c.GetCitizenTypes()
				})
				.OrderByDescending(x => x.City.Wonders.Length)
				.ThenByDescending(x => x.City.Size)
				.ThenByDescending(x => x.Citizens.happy)
				.ThenByDescending(x => x.Citizens.content)
				.ThenBy(x => x.Citizens.redShirt)
				.ThenBy(x => x.Citizens.unhappy)
				.Take(5)
				.Select(x => x.City)
			];

			Refresh();
		}

		private void DrawBackground()
		{
			int offsetX = Math.Max(0, (Width - 320) / 2);
			int offsetY = Math.Max(0, (Height - 200) / 2);

			this.Clear(3)
				.DrawText(Translate("The Top Five Cities in the World"), 0, 5, offsetX + 160, offsetY + 13, TextAlign.Center)
				.DrawText(Translate("The Top Five Cities in the World"), 0, 15, offsetX + 160, offsetY + 12, TextAlign.Center);
		}
	}
}