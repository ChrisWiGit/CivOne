// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Threading;
using CivOne.Buildings;
using CivOne.Enums;
using CivOne.Graphics.Sprites;
using CivOne.Governments;
using CivOne.Screens.Services;

namespace CivOne.Graphics
{
	internal class Icons
	{
		private static Resources Resources => Resources.Instance;

		private const string Filename = "SP257";
		private static IBitmap? _food;
		public static IBitmap Food => LazyInitializer.EnsureInitialized(ref _food, () =>
		{
			if (RuntimeHandler.Runtime.Settings.Free || !Resources.Exists(Filename))
				return new Picture(Free.Food, Common.GetPalette256);
			return Resources[Filename][128, 32, 8, 8]
				.ColourReplace(3, 0)
				.FillRectangle(0, 0, 1, 8, 0);
		});

		private static IBitmap? _foodLoss;
		public static IBitmap FoodLoss => LazyInitializer.EnsureInitialized(ref _foodLoss, () =>
			Resources[Filename][128, 32, 8, 8]
				.ColourReplace((3, 0), (15, 5))
				.FillRectangle(0, 0, 1, 8, 0));
		
		private static IBitmap? _shield;
		public static IBitmap Shield => LazyInitializer.EnsureInitialized(ref _shield, () =>
		{
			if (RuntimeHandler.Runtime.Settings.Free || !Resources.Exists(Filename))
				return new Picture(Free.Shield, Common.GetPalette256);
			return Resources[Filename][136, 32, 8, 8].ColourReplace(3, 0);
		});

		private static IBitmap? _smokeStack;
		public static IBitmap SmokeStack => LazyInitializer.EnsureInitialized(ref _smokeStack, () =>
		{
			if (RuntimeHandler.Runtime.Settings.Free || !Resources.Exists(Filename))
				return new Picture(Free.Shield, Common.GetPalette256);
			return Resources[Filename][50, 32, 62-50, 46-32].ColourReplace(3, 0);
		});
		
		private static IBitmap? _shieldLoss;
		public static IBitmap ShieldLoss => LazyInitializer.EnsureInitialized(ref _shieldLoss, () =>
			Resources[Filename][136, 32, 8, 8].ColourReplace((3, 0), (15, 5)));
		
		private static IBitmap? _trade;
		public static IBitmap Trade => LazyInitializer.EnsureInitialized(ref _trade, () =>
		{
			if (RuntimeHandler.Runtime.Settings.Free || !Resources.Exists(Filename))
				return new Picture(Free.Trade, Common.GetPalette256);
			return Resources[Filename][144, 32, 8, 8].ColourReplace(3, 0);
		});

		private static IBitmap? _corruption;
		public static IBitmap Corruption => LazyInitializer.EnsureInitialized(ref _corruption, () =>
			Resources[Filename][144, 32, 8, 8].ColourReplace((3, 0), (15, 5)));
		
		private static IBitmap? _unhappy;
		public static IBitmap Unhappy => LazyInitializer.EnsureInitialized(ref _unhappy, () =>
			Resources[Filename][136, 40, 8, 8].ColourReplace(3, 0));
		
		private static IBitmap? _luxuries;
		public static IBitmap Luxuries => LazyInitializer.EnsureInitialized(ref _luxuries, () =>
		{
			if (RuntimeHandler.Runtime.Settings.Free || !Resources.Exists(Filename))
				return new Picture(Free.Luxuries, Common.GetPalette256);
			return Resources[Filename][144, 40, 8, 8].ColourReplace(3, 0);
		});
		
		private static IBitmap? _taxes;
		public static IBitmap Taxes => LazyInitializer.EnsureInitialized(ref _taxes, () =>
		{
			if (RuntimeHandler.Runtime.Settings.Free || !Resources.Exists(Filename))
				return new Picture(Free.Taxes, Common.GetPalette256);
			return Resources[Filename][152, 32, 8, 8].ColourReplace(3, 0);
		});
		
		private static IBitmap? _science;
		public static IBitmap Science => LazyInitializer.EnsureInitialized(ref _science, () =>
		{
			if (RuntimeHandler.Runtime.Settings.Free || !Resources.Exists(Filename))
				return new Picture(Free.Science, Common.GetPalette256);
			return Resources[Filename][128, 40, 8, 8].ColourReplace(3, 0);
		});
		
		private static IBitmap? _spy;
		public static IBitmap Spy => LazyInitializer.EnsureInitialized(ref _spy, () =>
		{
			if (RuntimeHandler.Runtime.Settings.Free || !Resources.Exists("SP299"))
				return new Picture(Free.Instance.PanelGrey, Common.GetPalette256);
			return Resources["SP299"][160, 142, 40, 52].ColourReplace(3, 0);
		});
		
		private static IBitmap? _newspaper;
		public static IBitmap Newspaper => LazyInitializer.EnsureInitialized(ref _newspaper, () =>
			Resources[Filename][176, 128, 32, 16]);

		private static IBitmap? _sellButton;
		public static IBitmap SellButton => LazyInitializer.EnsureInitialized(ref _sellButton, () =>
		{
			byte[] bytemap = [
				0,  0,  5,  5,  5,  0,  0,  0,
				0,  5, 15, 15, 15,  5,  0,  0,
				5, 15, 12, 12, 12, 15,  5,  0,
				5, 15, 12, 12, 12, 15,  5,  0,
				5, 15, 12, 12, 12, 15,  5,  0,
				0,  5, 15, 15, 15,  5,  0,  0,
				0,  0,  5,  5,  5,  0,  0,  0
			];
			return new Picture(8, 7, bytemap, Food.Palette);
		});

		private static readonly Lock _helperArrowLock = new();
		private static IBitmap[]? _helperArrow;
		public static IBitmap? HelperArrow(Direction direction)
		{
			if (_helperArrow != null)
			{
				return direction switch
				{
					Direction.South => _helperArrow[0],
					Direction.North => _helperArrow[1],
					Direction.West => _helperArrow[2],
					Direction.East => _helperArrow[3],
					_ => null,
				};
			}
			
			lock (_helperArrowLock)
			{
				if (_helperArrow == null)
				{
					IBitmap[] arrows =
					[
						new Picture(16, 16, [
							0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
							0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
							0,  0,  0,  0,  0,  0,  5,  5,  5,  5,  0,  0,  0,  0,  0,  0,
							0,  0,  0,  0,  0,  0,  5, 15, 15,  5,  0,  0,  0,  0,  0,  0,
							0,  0,  0,  0,  0,  0,  5, 15, 15,  5,  0,  0,  0,  0,  0,  0,
							0,  0,  0,  0,  0,  0,  5, 15, 15,  5,  0,  0,  0,  0,  0,  0,
							0,  0,  0,  0,  0,  0,  5, 15, 15,  5,  0,  0,  0,  0,  0,  0,
							0,  0,  0,  0,  0,  0,  5, 15, 15,  5,  0,  0,  0,  0,  0,  0,
							0,  0,  5,  5,  5,  5,  5, 15, 15,  5,  5,  5,  5,  5,  0,  0,
							0,  0,  0,  5, 15, 15, 15, 15, 15, 15, 15, 15,  5,  0,  0,  0,
							0,  0,  0,  0,  5, 15, 15, 15, 15, 15, 15,  5,  0,  0,  0,  0,
							0,  0,  0,  0,  0,  5, 15, 15, 15, 15,  5,  0,  0,  0,  0,  0,
							0,  0,  0,  0,  0,  0,  5, 15, 15,  5,  0,  0,  0,  0,  0,  0,
							0,  0,  0,  0,  0,  0,  0,  5,  5,  0,  0,  0,  0,  0,  0,  0,
							0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
							0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
							], Food.Palette),
							new Picture(16, 16, [
							0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
							0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
							0,  0,  0,  0,  0,  0,  0,  5,  5,  0,  0,  0,  0,  0,  0,  0,
							0,  0,  0,  0,  0,  0,  5, 15, 15,  5,  0,  0,  0,  0,  0,  0,
							0,  0,  0,  0,  0,  5, 15, 15, 15, 15,  5,  0,  0,  0,  0,  0,
							0,  0,  0,  0,  5, 15, 15, 15, 15, 15, 15,  5,  0,  0,  0,  0,
							0,  0,  0,  5, 15, 15, 15, 15, 15, 15, 15, 15,  5,  0,  0,  0,
							0,  0,  5,  5,  5,  5,  5, 15, 15,  5,  5,  5,  5,  5,  0,  0,
							0,  0,  0,  0,  0,  0,  5, 15, 15,  5,  0,  0,  0,  0,  0,  0,
							0,  0,  0,  0,  0,  0,  5, 15, 15,  5,  0,  0,  0,  0,  0,  0,
							0,  0,  0,  0,  0,  0,  5, 15, 15,  5,  0,  0,  0,  0,  0,  0,
							0,  0,  0,  0,  0,  0,  5, 15, 15,  5,  0,  0,  0,  0,  0,  0,
							0,  0,  0,  0,  0,  0,  5, 15, 15,  5,  0,  0,  0,  0,  0,  0,
							0,  0,  0,  0,  0,  0,  5,  5,  5,  5,  0,  0,  0,  0,  0,  0,
							0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
							0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
							], Food.Palette),
							new Picture(16, 16, [
							0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
							0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
							0,  0,  0,  0,  0,  0,  0,  5,  0,  0,  0,  0,  0,  0,  0,  0,
							0,  0,  0,  0,  0,  0,  5,  5,  0,  0,  0,  0,  0,  0,  0,  0,
							0,  0,  0,  0,  0,  5, 15,  5,  0,  0,  0,  0,  0,  0,  0,  0,
							0,  0,  0,  0,  5, 15, 15,  5,  0,  0,  0,  0,  0,  0,  0,  0,
							0,  0,  0,  5, 15, 15, 15,  5,  5,  5,  5,  5,  5,  5,  0,  0,
							0,  0,  5, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15,  5,  0,  0,
							0,  0,  5, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15,  5,  0,  0,
							0,  0,  0,  5, 15, 15, 15,  5,  5,  5,  5,  5,  5,  5,  0,  0,
							0,  0,  0,  0,  5, 15, 15,  5,  0,  0,  0,  0,  0,  0,  0,  0,
							0,  0,  0,  0,  0,  5, 15,  5,  0,  0,  0,  0,  0,  0,  0,  0,
							0,  0,  0,  0,  0,  0,  5,  5,  0,  0,  0,  0,  0,  0,  0,  0,
							0,  0,  0,  0,  0,  0,  0,  5,  0,  0,  0,  0,  0,  0,  0,  0,
							0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
							0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
							], Food.Palette),
							new Picture(16, 16, [
							0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
							0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
							0,  0,  0,  0,  0,  0,  0,  0,  5,  0,  0,  0,  0,  0,  0,  0,
							0,  0,  0,  0,  0,  0,  0,  0,  5,  5,  0,  0,  0,  0,  0,  0,
							0,  0,  0,  0,  0,  0,  0,  0,  5, 15,  5,  0,  0,  0,  0,  0,
							0,  0,  0,  0,  0,  0,  0,  0,  5, 15, 15,  5,  0,  0,  0,  0,
							0,  0,  5,  5,  5,  5,  5,  5,  5, 15, 15, 15,  5,  0,  0,  0,
							0,  0,  5, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15,  5,  0,  0,
							0,  0,  5, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15,  5,  0,  0,
							0,  0,  5,  5,  5,  5,  5,  5,  5, 15, 15, 15,  5,  0,  0,  0,
							0,  0,  0,  0,  0,  0,  0,  0,  5, 15, 15,  5,  0,  0,  0,  0,
							0,  0,  0,  0,  0,  0,  0,  0,  5, 15,  5,  0,  0,  0,  0,  0,
							0,  0,  0,  0,  0,  0,  0,  0,  5,  5,  0,  0,  0,  0,  0,  0,
							0,  0,  0,  0,  0,  0,  0,  0,  5,  0,  0,  0,  0,  0,  0,  0,
							0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
							0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
							], Food.Palette),
						];
					_helperArrow = arrows;
				}
			}

			return direction switch
			{
				Direction.South => _helperArrow[0],
				Direction.North => _helperArrow[1],
				Direction.West => _helperArrow[2],
				Direction.East => _helperArrow[3],
				_ => null,
			};
		}

		private static readonly Lock _citizenLock = new();
		private static readonly IBitmap[] _citizen = new Picture[9];
		public static IBitmap Citizen(Citizen citizen)
		{
			int idx = (int)citizen;
			if (_citizen[idx] != null) return _citizen[idx];
			lock (_citizenLock)
			{
				_citizen[idx] ??= Resources[Filename][(8 * idx), 128, 8, 16];
			}
			return _citizen[idx];
		}

		private static readonly Lock _lampLock = new();
		private static readonly IBitmap[] _lamp = new Picture[4];
		public static IBitmap? Lamp(int stage)
		{
			if (stage < 0 || stage > 3)
				return null;

			if (_lamp[stage] != null) return _lamp[stage];
			lock (_lampLock)
			{
				_lamp[stage] ??= Resources[Filename][128 + (8 * stage), 48, 8, 8];
			}
			return _lamp[stage];
		}

		private static readonly Lock _sunLock = new();
		private static readonly IBitmap[] _sun = new Picture[4];
		public static IBitmap? Sun(int stage)
		{
			if (stage < 0 || stage > 3)
				return null;

			if (_sun[stage] != null) return _sun[stage];
			lock (_sunLock)
			{
				_sun[stage] ??= Resources[Filename][130 + (8 * stage), 58, 6, 6];
			}
			return _sun[stage];
		}

		private static readonly Lock _governmentPortraitLock = new();
		private static readonly IBitmap[,] _governmentPortrait = new Picture[7, 4];
		public static IBitmap GovernmentPortrait(IGovernment government, Advisor advisor, bool modern)
		{
			string filename;
			int governmentId;
			if (government is Monarchy)
			{
				governmentId = (modern ? 3 : 2);
				filename = $"GOVT1" + (modern ? "M" : "A");
			}
			else if (government is Republic || government is Democracy)
			{
				governmentId = (modern ? 5 : 4);
				filename = $"GOVT2" + (modern ? "M" : "A");
			}
			else if (government is Communism)
			{
				governmentId = 6;
				filename = "GOVT3A";
			}
			else // Anarchy or Despotism
			{
				governmentId = (modern ? 1 : 0);
				filename = "GOVT0" + (modern ? "M" : "A");
			}
			int advisorId = (int)advisor;
			if (_governmentPortrait[governmentId, advisorId] != null)
				return _governmentPortrait[governmentId, advisorId];
			lock (_governmentPortraitLock)
			{
				_governmentPortrait[governmentId, advisorId] ??= Resources[filename][(40 * advisorId), 0, 40, 60];
			}
			return _governmentPortrait[governmentId, advisorId];
		}

		public static IBitmap City(City city, bool smallFont = false)
		{
			IBitmap output = new Picture(16, 16);
			TextSettings settings = new()
			{
				FontId = smallFont ? 1 : 0,
				Alignment = TextAlign.Center
			};
			
			if (city.Tile.Units.Length > 0)
				output.FillRectangle(0, 0, 16, 16, 5);
			output.FillRectangle(1, 1, 14, 14, 15)
				.FillRectangle(2, 1, 13, 13, Common.ColourDark[city.CityOwnerPlayerIndex])
				.FillRectangle(2, 2, 12, 12, Common.ColourLight[city.CityOwnerPlayerIndex]);
			
			Picture resource;
			if (Resources.Exists(Filename))
			{
				resource = Resources[Filename][192, 112, 16, 16];
			}
			else
			{
				resource = new Picture(Free.Instance.City, Common.GetPalette256);
			}
			resource
				.ColourReplace(3, 0)
				.ColourReplace(5, Common.ColourDark[city.CityOwnerPlayerIndex]);
				
			CitizenTypes citizenType = city.GetCitizenTypes();
			if (citizenType.InDisorder)
			{
				output.AddLayer(resource, 0, 0)
					.AddLayer(Icons.Citizen(Enums.Citizen.UnhappyMale), 5, 1);
			}
			else
			{
				output.AddLayer(resource, 0, 0)
					.DrawText($"{city.Size}", (smallFont ? 1 : 0), 5, 9, 5, TextAlign.Center);
			}

			resource?.Dispose();

			if (city.HasBuilding<CityWalls>())
			{
				output.AddLayer(Generic.Fortify, 0, 0);
			}
			
			return output;
		}
	}
}