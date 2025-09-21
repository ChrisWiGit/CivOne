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
using CivOne.Buildings;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.Graphics.Sprites;
using CivOne.Units;

namespace CivOne.Screens.CityManagerPanels
{
    internal class CityInfo : BaseScreen
	{
		private readonly City _city;
		private readonly IUnit[] _units;

		private CityInfoChoice _choice = CityInfoChoice.Info;
		private bool _update = true;

		private Picture InfoFrame
		{
			get
			{
				Picture output = new Picture(144, 83);
				for (int i = 0; i < _units.Length; i++)
				{
					int xx = 4 + ((i % 6) * 18);
					int yy = 0 + (((i - (i % 6)) / 6) * 16);

					output.AddLayer(_units[i].ToBitmap(), xx, yy);
					string homeCity = "NON.";
					if (_units[i].Home != null)
					{
						homeCity = _units[i].Home.Name;
						if (homeCity.Length >= 3)
							homeCity = $"{homeCity.Substring(0, 3)}.";
					}
					output.DrawText(homeCity, 1, 5, xx, yy + 16);
				}
				return output;
			}
		}

        private void DrawHappyRow(Picture output, int yy, int happy, int content, int unhappy, int ent, int sci, int tax)
        {
            int dex = 0;
            for (int x = 0; x < happy; x++)
                output.AddLayer(Icons.Citizen((x % 2 == 0) ? Citizen.HappyMale : Citizen.HappyFemale), 7 + (8 * dex++), yy);
            for (int x = 0; x < content; x++)
                output.AddLayer(Icons.Citizen((x % 2 == 0) ? Citizen.ContentMale : Citizen.ContentFemale), 7 + (8 * dex++), yy);
            for (int x = 0; x < unhappy; x++)
                output.AddLayer(Icons.Citizen((x % 2 == 0) ? Citizen.UnhappyMale : Citizen.UnhappyFemale), 7 + (8 * dex++), yy);
            for (int x = 0; x < ent; x++)
                output.AddLayer(Icons.Citizen(Citizen.Entertainer), 7 + (8 * dex++), yy);
            for (int x = 0; x < sci; x++)
                output.AddLayer(Icons.Citizen(Citizen.Scientist), 7 + (8 * dex++), yy);
            for (int x = 0; x < tax; x++)
                output.AddLayer(Icons.Citizen(Citizen.Taxman), 7 + (8 * dex++), yy);

        }

        private void DrawHappyRow(Picture output, int yy, City.CitizenTypes group)
        {
            DrawHappyRow(output, yy, group.happy,group.content,group.unhappy, group.elvis, group.einstein, group.taxman);
        }

        private Picture HappyFrame
		{
			get
			{
				Picture output = new Picture(144, 83)
					.FillRectangle(5, 15, 122, 1, 1)
					.FillRectangle(5, 31, 122, 1, 1)
					.As<Picture>();

                using (var residents = _city.Residents.GetEnumerator())
                {
                    // initial state
                    residents.MoveNext();
                    var group = residents.Current;
                    int yy = 1;
                    DrawHappyRow(output, yy, group);

                    // luxury [row drawn only if there is a change]
                    yy += 16;
                    residents.MoveNext();
                    group = residents.Current;
                    if (group.happy != 0)
                    {
                        DrawHappyRow(output, yy, group);
                        output.AddLayer(Icons.Luxuries, output.Width - 25, 19);
                        yy += 16;
                    }

                    // buildings [row drawn only if there is a change]
                    // TODO fire-eggs should this be drawn if there are buildings but no change?
                    if (residents.MoveNext())
                    {
                        var group2 = residents.Current;
                        if (!group2.Equals(group))
                        {
                            DrawHappyRow(output, yy, group2);
                            IBuilding temple = _city.Buildings.FirstOrDefault(b => b is Temple);
                            if (temple != null)
                                output.AddLayer(temple.SmallIcon, output.Width - temple.SmallIcon.Width() - 15, yy);
                            // TODO fire-eggs colosseum, cathedral
                            yy += 16;
                            group = group2;
                        }
                    }

                    // martial law [row always drawn]
                    if (residents.MoveNext())
                    {
                        var group2 = residents.Current;
                        if (!group2.Equals(group))
                        {
                            DrawHappyRow(output, yy, group2);
                            yy += 16;
                            group = group2;
                        }
                    }

                    // wonders [row only drawn if change]
                    if (residents.MoveNext())
                    {
                        var group2 = residents.Current;
                        if (!group2.Equals(group))
                        {
                            DrawHappyRow(output, yy, group2);
                        }
                    }
                }

                return output;
			}
		}
		
		private Picture MapFrame
		{
			get
			{
				Picture output = new Picture(144, 83)
					.FillRectangle(5, 2, 122, 1, 9)
					.FillRectangle(5, 3, 1, 74, 9)
					.FillRectangle(126, 3, 1, 74, 9)
					.FillRectangle(5, 77, 122, 1, 9)
					.FillRectangle(6, 3, 120, 74, 5)
					.As<Picture>();

                Map local = Map.Instance;
                var unis = Game.GetUnits().Where(u => u.Home == _city).ToArray(); // city-owned units

                // Using image scaling, take the 80x50 map and scale up to 120x75.
                // http://tech-algorithm.com/articles/nearest-neighbor-image-scaling/
                // Then draw the city and city-owned units as 2x2 squares. Draw the
                // city after the units so it takes visible precedence.

                int xRatio = (Map.WIDTH << 16) / 120 + 1;
                int yRatio = ((Map.HEIGHT-1) << 16) / 75 + 1; // Note: trying to use the full height runs off the array

                for (int i=0; i < 120; i++)
                for (int j = 0; j < 75; j++)
                {
                    int x2 = (i * xRatio) >> 16;
                    int y2 = (j * yRatio) >> 16;

                    if (Human.Visible(x2, y2) || Settings.RevealWorld)
                    {
                        output[i + 6, j + 3] = (byte)(local[x2, y2].IsOcean ? 1 : 2);
                    }
                }

                foreach (var unit in unis)
                {
                    Draw2by2(unit.X, unit.Y, Common.ColourLight[_city.Owner]);
                }
                Draw2by2(_city.X, _city.Y, 15);

                return output;

                void Draw2by2(int mapX, int mapY, byte color)
                {
                    int x3 = ((mapX << 16) / xRatio) + 1;
                    int y3 = ((mapY << 16) / yRatio) + 1;
                    output[x3 + 6, y3 + 3] = color;
                    output[x3 + 6, y3 + 4] = color;
                    output[x3 + 7, y3 + 3] = color;
                    output[x3 + 7, y3 + 4] = color;
                }
            }
		}

		protected override bool HasUpdate(uint gameTick)
		{
			if (_update)
			{
				this.Tile(Pattern.PanelBlue)
					.DrawRectangle(colour: 1);
				
				DrawButton("Info", (byte)((_choice == CityInfoChoice.Info) ? 15 : 9), 1, 0, 0, 34);
				DrawButton("Happy", (byte)((_choice == CityInfoChoice.Happy) ? 15 : 9), 1, 34, 0, 32);
				DrawButton("Map", (byte)((_choice == CityInfoChoice.Map) ? 15 : 9), 1, 66, 0, 33);
				DrawButton("View", 9, 1, 99, 0, 33);

				switch (_choice)
				{
					case CityInfoChoice.Info:
						this.AddLayer(_cityInfoUnits.Bitmap, 0, 9);
						break;
					case CityInfoChoice.Happy:
						this.AddLayer(HappyFrame, 0, 9);
						break;
					case CityInfoChoice.Map:
						this.AddLayer(MapFrame, 0, 9);
						break;
				}

				_update = false;
			}
			if (IsUnitsInfoActive && _cityInfoUnits.Update(gameTick)) _update = true;

			return true;
		}

		public override bool Update(uint gameTick)
		{
			return base.Update(gameTick) ||
				(IsUnitsInfoActive && _cityInfoUnits.Update(gameTick));
		}

		private bool GotoInfo()
		{
			_choice = CityInfoChoice.Info;
			_update = true;
			return true;
		}

		private bool GotoHappy()
		{
			_choice = CityInfoChoice.Happy;
			_update = true;
			return true;
		}

		private bool GotoMap()
		{
			_choice = CityInfoChoice.Map;
			_update = true;
			return true;
		}

		private bool GotoView()
		{
			_choice = CityInfoChoice.Info;
			_update = true;
			Common.AddScreen(new CityView(_city));
			return true;
		}
		
		public override bool KeyDown(KeyboardEventArgs args)
		{
			if (IsUnitsInfoActive && _cityInfoUnits.KeyDown(args)) return true;

			switch (args.KeyChar)
			{
				case 'I':
					return GotoInfo();
				case 'H':
					return GotoHappy();
				case 'M':
					return GotoMap();
				case 'V':
					return GotoView();
			}
			return false;
		}

		private bool InfoClick(ScreenEventArgs args)
		{
			for (int i = 0; i < _units.Length; i++)
			{
				int xx = 4 + ((i % 6) * 18);
				int yy = 0 + (((i - (i % 6)) / 6) * 16);

				if (new Rectangle(xx, yy, 16, 16).Contains(args.Location))
				{
					_units[i].Busy = false;
					_update = true;
					break;
				}
			}
			return true;
		}
		
		public override bool MouseDown(ScreenEventArgs args)
		{
			if (IsUnitsInfoActive && _cityInfoUnits.MouseDown(args)) return true;

			if (args.Y < 10)
			{
				if (args.X < 34) return GotoInfo();
				else if (args.X < 66) return GotoHappy();
				else if (args.X < 99) return GotoMap();
				else if (args.X < 132) return GotoView();
			}
			
			switch (_choice)
			{
				case CityInfoChoice.Info:
					MouseArgsOffset(ref args, 0, 9);
					return InfoClick(args);
				case CityInfoChoice.Happy:
				case CityInfoChoice.Map:
					break;
			}
			return true;
		}

		protected bool IsUnitsInfoActive => _choice == CityInfoChoice.Info;

		public CityInfo(City city, ICityManager cityManager) : base(133, 92)
		{
			_city = city;
			_units = [.. Game.GetUnits().Where(u => u.X == city.X && u.Y == city.Y)];

			_cityInfoUnits = new CityInfoUnits(city, cityManager, _units);
			
			GotoInfo();
		}
		
		private readonly CityInfoUnits _cityInfoUnits;

		public void Update()
		{
			_update = true;
			_cityInfoUnits.Update();
		}
    }
}