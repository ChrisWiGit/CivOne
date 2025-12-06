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
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.Graphics.Sprites;
using CivOne.IO;
using CivOne.Tasks;

namespace CivOne.Screens.CityManagerPanels
{
	/// <summary>
	/// City Header Panel showing city name, population and citizens
	/// </summary>
	internal class CityHeader : BaseScreen
	{
		private readonly City _city;

		private readonly ICityCitizenLayoutService _citizenLayoutService;

		public event EventHandler HeaderUpdate;

		protected static int MinCitizenSize => 5;

		protected override bool HasUpdate(uint gameTick)
		{
			if (!RefreshNeeded())
			{
				return false;
			}

			string population = Common.NumberSeperator(_city.Population);

			this.Tile(Pattern.PanelBlue)
				.DrawRectangle(colour: 1)
				.DrawText($"{_city.Name} (Pop:{population})", 1, 17, (int)Math.Ceiling((float)Width / 2), 1, TextAlign.Center);

			foreach (var info in _citizenLayoutService.EnumerateCitizens())
			{
				this.AddLayer(Icons.Citizen(info.Citizen), info.X, 7);
			}

			return true;
		}

		public override bool MouseDown(ScreenEventArgs args)
		{
			if (args.Y <= 6 || args.Y >= 20)
			{
				return false;
			}

			if (_city.Size < MinCitizenSize)
			{
				CitySizeToSmall(this, null);
				return true;
			}

			foreach (var info in _citizenLayoutService.EnumerateCitizens())
			{
				bool isLastCitizenForFullClickArea = info.CitizenIndex == _city.Size - 1;
				int citizenOffset = isLastCitizenForFullClickArea ? 8 : _citizenLayoutService.CitizenOffset - 1;

				if (args.X < info.X || args.X > info.X + citizenOffset || info.SpecialistIndex < 0)
					continue;

				_city.ChangeSpecialist(info.SpecialistIndex);
				Update();
				return true;
			}

			return false;
		}

		protected int GetSpecialistIndex(KeyboardEventArgs args)
		{
			int offset = 0;
			switch (args.Modifier)
			{
				case KeyModifier.Shift:
					offset += 10;
					break;
				case KeyModifier.Control:
					offset += 20;
					break;
				case KeyModifier.Alt:
					offset += 30;
					break;
				case KeyModifier.Control | KeyModifier.Shift:
					offset += 40;
					// CW: Control + Shift + 0 does not work because KeyDown is not called.
					break;
				case KeyModifier.Alt | KeyModifier.Shift:
					offset += 50;
					break;
				case KeyModifier.Alt | KeyModifier.Control:
					offset += 60;
					break;
				case KeyModifier.Alt | KeyModifier.Control | KeyModifier.Shift:
					offset += 70;
					break;
			}

			if (args.KeyChar >= '0' && args.KeyChar <= '9')
			{
				return offset + (args.KeyChar == '0' ? 9 : args.KeyChar - '1');
			}

			return -1;
		}

		public override bool KeyDown(KeyboardEventArgs args)
		{
			int index = GetSpecialistIndex(args);
			if (index == -1)
			{
				return false;
			}

			if (_city.Size < MinCitizenSize)
			{
				CitySizeToSmall(this, null);
				return true;
			}

			_city.ChangeSpecialist(index);
			Update();
			return true;
		}


		private void CitySizeToSmall(object sender, EventArgs args)
		{
			// CW: dialog position is not as in original game, but easier to implement and results in same effect
			GameTask.Enqueue(Message.General(
				"A city must have at least five",
				"population units to support",
				"taxmen or scientists."));
		}

		public void Update()
		{
			HeaderUpdate?.Invoke(this, null);
			Refresh();
		}

		public void Close()
		{
			Destroy();
		}

		internal void Resize(int width)
		{
			Bitmap = new Bytemap(width, 21);
			Refresh();
		}

		public CityHeader(City city) : base(207, 21)
		{
			_city = city;
			_citizenLayoutService = ICityCitizenLayoutService.Create(city);
		}
	}
}