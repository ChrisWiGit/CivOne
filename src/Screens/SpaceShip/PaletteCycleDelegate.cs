using System.Linq;
using CivOne.Graphics;

namespace CivOne.Screens.SpaceShipAssets
{
	/// <summary>
	/// Animates a picture by rotating colour ranges like the original game: each range moves towards the higher index,
	/// by one entry every <c>Rate</c> ticks of the original's 70 Hz timer.
	/// </summary>
	/// <param name="palette">The palette to animate; it is changed in place.</param>
	/// <param name="cycles">The rotating ranges, each with its rate in original ticks and its first and last index.</param>
	internal sealed class PaletteCycleDelegate(Palette palette, (int Rate, int From, int To)[] cycles)
	{
		// The original's timer runs at the VGA refresh rate. Screens get the game tick divided by 4, so 15 Hz.
		private const double OriginalTicksPerGameTick = 70.09 / 15.0;

		/// <summary>
		/// The ranges of the space station picture after the landing.
		/// </summary>
		public static readonly (int Rate, int From, int To)[] SpaceStationCycles =
		[
			(15, 16, 31),
			(100, 32, 34),
			(42, 35, 40),
			(300, 41, 47),
			(60, 48, 53),
			(37, 54, 63),
		];

		/// <summary>
		/// The ranges of the spaceship view: module lights, engine glow and the blinking lights.
		/// </summary>
		public static readonly (int Rate, int From, int To)[] SpaceShipCycles =
		[
			(30, 208, 223),
			(11, 192, 207),
			(20, 224, 239),
			(8, 144, 159),
			(150, 128, 129),
		];

		private readonly Palette _palette = palette;
		private readonly (int Rate, int From, int To)[] _cycles = cycles;
		private readonly long[] _steps = new long[cycles.Length];

		/// <summary>
		/// Tells whether a palette entry is part of a rotating range, so its colour changes over time.
		/// </summary>
		/// <param name="index">The palette index.</param>
		/// <returns><c>true</c> if the entry rotates.</returns>
		public bool IsAnimated(int index) => _cycles.Any(cycle => index >= cycle.From && index <= cycle.To);

		/// <summary>
		/// Rotates every range that is due.
		/// </summary>
		/// <param name="elapsedGameTicks">The screen ticks (15 Hz) since the animation started.</param>
		/// <returns><c>true</c> if a colour changed.</returns>
		public bool Update(uint elapsedGameTicks)
		{
			bool changed = false;
			for (int i = 0; i < _cycles.Length; i++)
			{
				(int rate, int from, int to) = _cycles[i];
				long steps = (long)(elapsedGameTicks * OriginalTicksPerGameTick / rate);
				while (_steps[i] < steps)
				{
					Rotate(from, to);
					_steps[i]++;
					changed = true;
				}
			}

			return changed;
		}

		private void Rotate(int from, int to)
		{
			Colour last = _palette[to];
			for (int i = to; i > from; i--)
			{
				_palette[i] = _palette[i - 1];
			}

			_palette[from] = last;
		}
	}
}
