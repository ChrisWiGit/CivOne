using System;
using System.Collections.Generic;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.Screens.SpaceShipAssets;
using CivOne.Services.EndGame;

namespace CivOne.Screens
{
	/// <summary>
	/// The space-victory screen, in two phases like the original game.
	/// </summary>
	/// <remarks>
	/// <list type="number">
	/// <item>Flight (320 frames): the star picture slides in from the right by one pixel per frame, a rotating planet
	/// sits in it, and the ship, a single white pixel, flies from the left towards the planet. Every
	/// <see cref="FramesPerLine"/> frames the next line of the flight text appears in the text strip.
	/// Like the original, the line already advances on the first frame, so the empty opening line is never shown
	/// and the last flight frames show the first landing line, which appears again after the landing.</item>
	/// <item>Landing: for the human player the flight picture fades to black, then the space station is shown with
	/// its palette animation. For a rival the human player's palace is shown at once. The remaining lines appear
	/// one after another at the top, each with a blinking dot. A key or click closes the screen after the last line.</item>
	/// </list>
	/// <para>
	/// Deliberate deviation: the original runs the flight at a fixed 8 timer ticks per frame and does not wait for
	/// the music. Here the flight is paced so that the landing falls on the end of the flight music, when its length
	/// is known. Otherwise the flight takes <see cref="DefaultFlightSeconds"/>, about the length of the win music.
	/// </para>
	/// <para>
	/// The other timings follow the original timer, assumed to run at 70 Hz, converted to the screen tick.
	/// Screens get the game tick divided by 4, so the screen tick runs at 15 Hz.
	/// The rate of the original timer is not confirmed.
	/// </para>
	/// </remarks>
	[ScreenResizeable]
	internal sealed class SpaceVictory : BaseScreen
	{
		private const double ScreenTicksPerSecond = 15.0;
		private const double ScreenTicksPerOriginalTick = ScreenTicksPerSecond / 70.09;
		private const int FlightFrames = 320;
		private const int FramesPerLine = 22;
		private const int OriginalTicksPerBlink = 80;
		private const int OriginalTicksPerFade = 50;
		private const double DefaultFlightSeconds = 47.0;
		private const int PlanetFrameCount = 23;
		private const int PlanetFrameSize = 49;
		private const int PlanetFrameStep = 50;
		private const int PlanetFramesPerRow = 6;
		private const int PlanetX = 135;
		private const int PlanetY = 63;
		private const int PictureTop = 12;
		private const int PictureSourceTop = 24;
		private const int PictureHeight = 176;
		private const int ShipY = 92;
		private const int FlightTextY = 39;
		private const byte FlightBackgroundColour = 236;
		private const byte FlightTextColour = 60;
		private const byte FlightShadowColour = 56;
		private const byte LandingTextColour = 15;
		private const byte LandingShadowColour = 11;
		private const byte WhiteColour = 15;
		private const byte BlackColour = 0;
		private const int FontId = 5;

		private readonly Picture _flightPicture;
		private readonly byte _landingBlack;
		private readonly Picture[] _planetFrames;
		private readonly Picture _landingBackground;
		private readonly Palette _landingPalette;
		private readonly PaletteCycleDelegate? _paletteCycle;
		private readonly IReadOnlyList<string> _flightLines;
		private readonly IReadOnlyList<string> _landingLines;
		private readonly Action? _onLanding;
		private readonly bool _fadeBeforeLanding;
		private readonly double _screenTicksPerFrame;

		private int _frame;
		private int _flightLine = -1;
		private int _landingLine = -1;
		private bool _landed;
		private bool _blinkOn;
		private bool _finished;
		private uint? _startTick;
		private uint? _fadeTick;
		private uint _landingTick;
		private int _blinkSteps;
		private uint _lastTick;

		private int OffsetX => Math.Max(0, (Width - 320) / 2);
		private int OffsetY => Math.Max(0, (Height - 200) / 2);

		protected override bool HasUpdate(uint gameTick)
		{
			_startTick ??= gameTick;

			_lastTick = gameTick;
			if (_fadeTick.HasValue && !_landed)
			{
				return Fade(gameTick);
			}

			if (!_landed)
			{
				// Catch up when updates come late, so the flight keeps its pace.
				int dueFrame = (int)((gameTick - _startTick.Value) / _screenTicksPerFrame);
				if (dueFrame < _frame && _frame > 0)
				{
					return false;
				}

				_frame = Math.Max(_frame, dueFrame);
				NextFlightFrame(gameTick);
				return true;
			}

			bool changed = _paletteCycle?.Update(gameTick - _landingTick) == true;
			if (changed)
			{
				Palette = _landingPalette;
			}

			if (_finished)
			{
				return changed;
			}

			int dueBlinks = (int)((gameTick - _landingTick) / (OriginalTicksPerBlink * ScreenTicksPerOriginalTick));
			if (dueBlinks < _blinkSteps)
			{
				return changed;
			}

			_blinkSteps++;
			NextLandingStep();
			return true;
		}

		private void NextFlightFrame(uint gameTick)
		{
			_flightLine = Math.Min((_frame / FramesPerLine) + 1, _flightLines.Count + _landingLines.Count - 1);

			DrawFlight();
			_frame++;
			if (_frame < FlightFrames)
			{
				return;
			}

			if (_fadeBeforeLanding)
			{
				_fadeTick = gameTick;
			}
			else
			{
				Land(gameTick);
			}
		}

		/// <summary>
		/// Fades the last flight frame to black, like the original before it shows the space station.
		/// </summary>
		private bool Fade(uint gameTick)
		{
			double progress = (gameTick - _fadeTick!.Value) / (OriginalTicksPerFade * ScreenTicksPerOriginalTick);
			if (progress >= 1.0)
			{
				Land(gameTick);
				return true;
			}

			// Index 0 is transparent and stays untouched.
			using Palette palette = OriginalColours.Copy();
			for (int i = 1; i < palette.Length; i++)
			{
				Colour colour = OriginalColours[i];
				palette[i] = new Colour((int)(colour.R * (1.0 - progress)), (int)(colour.G * (1.0 - progress)), (int)(colour.B * (1.0 - progress)));
			}

			Palette = palette;
			return true;
		}

		private string FlightLine(int index) => index < _flightLines.Count ? _flightLines[index] : _landingLines[index - _flightLines.Count];

		/// <summary>
		/// Screen ticks per flight frame: the length of the flight music spread over the frames, or
		/// <see cref="DefaultFlightSeconds"/> when the length is unknown.
		/// </summary>
		private static double ScreenTicksPerFrame(TimeSpan? flightDuration, bool fadeBeforeLanding)
		{
			TimeSpan duration = flightDuration is { } known && known > TimeSpan.Zero ? known : TimeSpan.FromSeconds(DefaultFlightSeconds);

			// The fade still belongs to the flight, so the space station appears when the music ends.
			double fadeTicks = fadeBeforeLanding ? OriginalTicksPerFade * ScreenTicksPerOriginalTick : 0.0;
			double flightTicks = (duration.TotalSeconds * ScreenTicksPerSecond) - fadeTicks;
			// Below one tick per frame the catch-up in HasUpdate skips frames, so even a short tune is met.
			return Math.Max(0.1, flightTicks / FlightFrames);
		}

		private void DrawFlight()
		{
			int ox = OffsetX;
			int oy = OffsetY;
			int revealed = Math.Min(_frame, 320);

			this.Clear(FlightBackgroundColour);
			if (revealed > 0)
			{
				// The rotating planet is part of the picture that slides in. Both are drawn at their sliding position
				// and the part right of the 320 pixel picture area is covered again.
				this.AddLayer(_flightPicture, ox + 320 - revealed, oy + PictureTop);
				this.AddLayer(_planetFrames[(_frame + 1) % PlanetFrameCount], ox + 320 - revealed + PlanetX, oy + PlanetY);
				this.FillRectangle(ox + 320, 0, Math.Max(0, Width - ox - 320), Height, FlightBackgroundColour);
			}

			if (_flightLine >= 0)
			{
				string line = FlightLine(_flightLine);
				this.DrawText(line, FontId, FlightShadowColour, ox + 160, oy + FlightTextY + 1, TextAlign.Center)
					.DrawText(line, FontId, FlightTextColour, ox + 160, oy + FlightTextY, TextAlign.Center);
			}

			// The ship is a single white pixel, half a pixel per frame, shown on every second frame.
			int shipX = -9 + ((_frame - 1) / 2);
			if (_frame % 2 == 1 && shipX >= 0)
			{
				this.FillRectangle(ox + shipX, oy + ShipY, 1, 1, WhiteColour);
			}
		}

		private void Land(uint gameTick)
		{
			_landed = true;
			_landingTick = gameTick;
			Palette = _landingPalette;
			_onLanding?.Invoke();
			DrawLanding();
		}

		private void NextLandingStep()
		{
			_blinkOn = !_blinkOn;
			if (_blinkOn)
			{
				if (_landingLine >= _landingLines.Count - 1)
				{
					_finished = true;
					DrawLanding();
					return;
				}

				_landingLine++;
			}

			DrawLanding();
		}

		private void DrawLanding()
		{
			int ox = OffsetX;
			int oy = OffsetY;

			this.Clear(_landingBlack).AddLayer(_landingBackground, ox, oy);
			if (_landingLine >= 0)
			{
				string line = _landingLines[_landingLine];
				this.DrawText(line, FontId, LandingShadowColour, ox + 160, oy + 5, TextAlign.Center)
					.DrawText(line, FontId, LandingTextColour, ox + 160, oy + 4, TextAlign.Center);
			}

			if (!_finished)
			{
				this.FillRectangle(ox + 280, oy + 32, 1, 1, _blinkOn ? WhiteColour : _landingBlack);
			}
		}

		protected override void Resize(int width, int height)
		{
			base.Resize(width, height);
			if (_landed)
			{
				DrawLanding();
			}
			else
			{
				DrawFlight();
			}
		}

		public override bool KeyDown(KeyboardEventArgs args)
		{
			if (args.Key == Key.Escape)
			{
				// Deviation from the original: ESC skips the flight and closes the screen after the landing.
				if (_landed)
				{
					Destroy();
				}
				else
				{
					Land(_lastTick);
				}

				return true;
			}

			if (_finished)
			{
				Destroy();
			}

			return true;
		}

		public override bool MouseDown(ScreenEventArgs args)
		{
			if (_finished)
			{
				Destroy();
			}

			return true;
		}

		/// <summary>
		/// Finds an opaque black in <paramref name="palette"/>; index 0 is transparent and would show the screen below.
		/// Entries the space station animation rotates are skipped, or the background would change colour.
		/// </summary>
		/// <remarks>
		/// When no such black exists, an entry the picture does not use is set to black in <paramref name="palette"/>.
		/// </remarks>
		private static byte OpaqueBlack(Picture picture, Palette palette, PaletteCycleDelegate? paletteCycle)
		{
			for (int i = 1; i < palette.Length; i++)
			{
				Colour colour = palette[i];
				if (paletteCycle?.IsAnimated(i) != true && colour.A > 0 && colour.R == 0 && colour.G == 0 && colour.B == 0)
				{
					return (byte)i;
				}
			}

			bool[] used = UsedColours(picture);
			for (int i = 1; i < palette.Length; i++)
			{
				if (!used[i] && paletteCycle?.IsAnimated(i) != true)
				{
					palette[i] = new Colour(0, 0, 0);
					return (byte)i;
				}
			}

			return BlackColour;
		}

		private static bool[] UsedColours(Picture picture)
		{
			bool[] used = new bool[256];
			for (int y = 0; y < picture.Height; y++)
			{
				for (int x = 0; x < picture.Width; x++)
				{
					used[picture[x, y]] = true;
				}
			}

			return used;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				foreach (Picture frame in _planetFrames)
				{
					frame.Dispose();
				}

				_landingPalette.Dispose();
				_flightPicture.Dispose();
				_landingBackground.Dispose();
			}

			base.Dispose(disposing);
		}

		/// <summary>
		/// Creates the space-victory screen.
		/// </summary>
		/// <param name="text">The flight and landing lines.</param>
		/// <param name="landingBackground">
		/// The picture after the landing: the space station or the human player's palace.
		/// The screen takes ownership and disposes it.
		/// </param>
		/// <param name="humanArrival">
		/// <c>true</c> for the human player's ship: the flight fades to black and the space station picture runs its
		/// palette animation.
		/// </param>
		/// <param name="onLanding">Called once when the landing picture appears, e.g. to change the music.</param>
		/// <param name="flightDuration">
		/// Length of the music played during the flight; the landing is timed to its end.
		/// <c>null</c> takes about the length of the win music.
		/// </param>
		public SpaceVictory(SpaceVictoryText text, Picture landingBackground, bool humanArrival, Action? onLanding = null, TimeSpan? flightDuration = null)
		{
			ArgumentNullException.ThrowIfNull(text);

			_flightLines = text.FlightLines;
			_landingLines = text.LandingLines;
			_landingBackground = landingBackground ?? throw new ArgumentNullException(nameof(landingBackground));
			_onLanding = onLanding;
			_landingPalette = landingBackground.Palette.Copy();
			_paletteCycle = humanArrival ? new PaletteCycleDelegate(_landingPalette, PaletteCycleDelegate.SpaceStationCycles) : null;
			_fadeBeforeLanding = humanArrival;
			_screenTicksPerFrame = ScreenTicksPerFrame(flightDuration, humanArrival);

			Picture flightBackground = Resources["PLANET1"];
			_flightPicture = flightBackground[0, PictureSourceTop, 320, PictureHeight];
			Picture planets = Resources["PLANET2"];
			_planetFrames = new Picture[PlanetFrameCount];
			for (int i = 0; i < PlanetFrameCount; i++)
			{
				_planetFrames[i] = planets[((i % PlanetFramesPerRow) * PlanetFrameStep) + 1, ((i / PlanetFramesPerRow) * PlanetFrameStep) + 1, PlanetFrameSize, PlanetFrameSize];
			}

			Palette = flightBackground.Palette;
			_landingBlack = OpaqueBlack(landingBackground, _landingPalette, _paletteCycle);
		}
	}
}
