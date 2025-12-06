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
using CivOne.Screens.Services;
using CivOne.Units;

namespace CivOne.Screens.CityManagerPanels
{
	/// <summary>
	/// City Information Panel showing city units, happiness, local map 
	/// and option to view city screen.
	/// </summary>
    internal class CityInfo : BaseScreen
	{
		private readonly City _city;
		private readonly IUnit[] _units;

		private readonly ICityCitizenLayoutService _citizenLayoutService;

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
			// Reduce gaps between citizens for big cities
			// Icons on right side need may overlap citizen icons if city is big.
			int middleSizedCityXOffset = _city.Size >= 10 ? 5 : 8;
			int leftStartPackedForBigCities = _citizenLayoutService.IsBigCity ? _citizenLayoutService.CitizenOffset : middleSizedCityXOffset;
			int startX = 7;

            int deltaX = 0;
            for (int x = 0; x < happy; x++)
                output.AddLayer(Icons.Citizen((x % 2 == 0) ? Citizen.HappyMale : Citizen.HappyFemale), startX + (leftStartPackedForBigCities * deltaX++), yy);
            for (int x = 0; x < content; x++)
                output.AddLayer(Icons.Citizen((x % 2 == 0) ? Citizen.ContentMale : Citizen.ContentFemale), startX + (leftStartPackedForBigCities * deltaX++), yy);
            for (int x = 0; x < unhappy; x++)
                output.AddLayer(Icons.Citizen((x % 2 == 0) ? Citizen.UnhappyMale : Citizen.UnhappyFemale), startX + (leftStartPackedForBigCities * deltaX++), yy);
            for (int x = 0; x < ent; x++)
                output.AddLayer(Icons.Citizen(Citizen.Entertainer), startX + (leftStartPackedForBigCities * deltaX++), yy);
            for (int x = 0; x < sci; x++)
                output.AddLayer(Icons.Citizen(Citizen.Scientist), startX + (leftStartPackedForBigCities * deltaX++), yy);
            for (int x = 0; x < tax; x++)
                output.AddLayer(Icons.Citizen(Citizen.Taxman), startX + (leftStartPackedForBigCities * deltaX++), yy);

        }

        private void DrawHappyRow(Picture output, int yy, CitizenTypes group)
        {
            DrawHappyRow(output, yy, group.happy,group.content,group.unhappy, group.elvis, group.einstein, group.taxman);
        }

        private Picture HappyFrame
		{
			get
			{
				const int Width = 144;
				const int Height = 83;	

				Picture background = new Picture(Width, Height).As<Picture>();
				Picture citizens = new Picture(Width - 17, Height).As<Picture>();

                using (var residents = _city.Residents.GetEnumerator())
                {
					// CW: align icons to the middle of the row (not original Civ1 behaviour)
					const int heightOffset = 2;

                    //Stage 1: initial state
					residents.MoveNext();
                    var group = residents.Current;
                    int yy = 1;
                    DrawHappyRow(citizens, yy, group);

                    // Stage 2: luxury [row drawn only if there is a change]
                    yy += 16;
					background.FillRectangle(5, yy - heightOffset, 122, 1, 1);

                    residents.MoveNext();
                    group = residents.Current;
					if (group.happy != 0)
					{
						DrawHappyRow(citizens, yy, group);

						background.AddLayer(Icons.Luxuries, background.Width - 25, 19);
						yy += 16;
						background.FillRectangle(5, yy - heightOffset, 122, 1, 1);
                    }

                    // Stage 3: buildings
					if (residents.MoveNext())
					{
						var group2 = residents.Current;
						if (group2.Buildings.Count > 0)
						{
							DrawHappyRow(citizens, yy, group2);

							int deltaX = 0;
							foreach (var building in group.Buildings)
							{
								background.AddLayer(building.SmallIcon,
									left: background.Width - building.SmallIcon.Width() - 15 - (building.SmallIcon.Width() + 1) * deltaX++,
									top: yy + heightOffset);
							}

							yy += 16;
							group = group2;
							background.FillRectangle(5, yy - heightOffset, 122, 1, 1);
						}
					}

                    // Stage 4: martial law [row always drawn]
                    if (residents.MoveNext())
                    {
                        var group2 = residents.Current;
						if (!group2.Equals(group))
						{
							DrawHappyRow(citizens, yy, group2);

							int deltaX = 0;
							group2.MarshallLawUnits.Reverse();
							foreach (var unit in group2.MarshallLawUnits)
							{
								const int width = 16;
								background.AddLayer(unit.ToBitmap(false),
									left: background.Width - 2 * width - 5 * deltaX++,
									top: yy - heightOffset);
							}

							yy += 16;
							group = group2;
							background.FillRectangle(5, yy - heightOffset, 122, 1, 1);
                        }
                    }

                    // Stage 5: wonders [row only drawn if change]
                    if (residents.MoveNext())
                    {
                        var group2 = residents.Current;
						if (!group2.Equals(group))
						{
							DrawHappyRow(citizens, yy, group2);

							int deltaX = 0;
							foreach (var wonder in group.Wonders)
							{
								background.AddLayer(wonder.SmallIcon,
									left: background.Width - wonder.SmallIcon.Width() - 15 - (wonder.SmallIcon.Width() + 1) * deltaX++,
									top: yy + heightOffset);
							}
						}
                    }
                }
				Picture output = new Picture(Width, Height).As<Picture>();
				

				output.AddLayer(citizens, 0, 0);
				output.AddLayer(background, 0, 0);

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
			
			_citizenLayoutService = ICityCitizenLayoutService.Create(city);
		}
		
		private readonly CityInfoUnits _cityInfoUnits;

		public void Update()
		{
			_update = true;
			_cityInfoUnits.Update();
		}
    }
}