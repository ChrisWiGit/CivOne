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
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.IO;
using CivOne.IO.Text;
using CivOne.Screens.Dialogs;
using CivOne.Sound;

namespace CivOne.Screens
{
	[ScreenResizeable]
	internal class Intro : BaseScreen
	{
		private const float FADE_STEP = 0.0625F;
		private const uint MAP_NOT_READY_MESSAGE_TICKS = 60;
		private const string INTRO_END_MARKER = "\0";
		private const string INTRO_ERROR_MESSAGE = "Error loading intro text.";

		/// <summary>Line of the intro text that swaps to the next picture instead of showing text.</summary>
		private const string PICTURE_CHANGE_MARKER = "_";

		/// <summary>
		/// How often <see cref="HasUpdate"/> runs per second: the 60 Hz raw tick divided by 4, which
		/// is the game tick RuntimeHandler passes to screens. One screen fade step costs one of these.
		/// </summary>
		private const double SCREEN_UPDATES_PER_SECOND = 15.0;

		/// <summary>
		/// How fast <see cref="_elapsedTicks"/> advances: half the screen update rate, because the
		/// text only advances on even game ticks (see <see cref="HasUpdate"/>).
		/// </summary>
		private const double INTRO_TICKS_PER_SECOND = SCREEN_UPDATES_PER_SECOND / 2;

		/// <summary>
		/// How long the music should keep playing after the last line of the text has appeared, so
		/// the text does not run into silence.
		/// </summary>
		private const double LEAD_OUT_SECONDS = 2.0;

		/// <summary>Standard intro length when the evolution music's duration is not known.</summary>
		private const double DEFAULT_DURATION_SECONDS = 150.0;

		/// <summary>Ticks per line when there is nothing to pace against, e.g. missing intro text.</summary>
		private const int DEFAULT_TICKS_PER_LINE = 30;

		private readonly string[] _introText;
		private readonly Picture[] _pictures;

		/// <summary>How many advance-steps (see <see cref="CountAdvanceSteps"/>) the text takes end to end.</summary>
		private readonly int _totalSteps;

		/// <summary>
		/// The steps the pacing is spread over: every step up to the last line of real text. The
		/// closing prompt follows one step later, at the same rate.
		/// </summary>
		private readonly int _pacedSteps;

		/// <summary>
		/// When the last line of real text is due, in <see cref="_elapsedTicks"/> units.
		/// </summary>
		private readonly int _targetTicks;

		private int _elapsedTicks;
		private int _stepIndex;
		private int _introLine = 1;
		
		private int _introPicture;
		private int _introPictureNext;
		private uint _mapNotReadyMessageUntil;
		private bool _errorDialogShown;

		private readonly WorldGenerationMusicDelegate _generationMusic = new();
        
		private int IntroPicture
		{
			get
			{
				return _introPicture;
			}
			set
			{
				if (value < 0)
				{
					_introPictureNext = 0;
					return;
				}

				int maxPictureIndex = _pictures.Length - 1;
				if (value > maxPictureIndex)
				{
					_introPictureNext = maxPictureIndex;
					return;
				}

				_introPictureNext = value;
			}
		}
        
		private void FadeColours()
		{
			if (!GFX256) return;
			
			using (Palette palette = _pictures[_introPicture].Palette.Copy())
			{
				for (int i = 1; i < 256; i++)
					palette[i] = FadeColour(new Colour(0, 0, 0), _pictures[_introPicture].OriginalColours[i]);
				this.SetPalette(palette);
			}
		}
		
		private bool HandleScreenFadeIn()
		{
			if (FadeStep >= 1.0F) return false;
			FadeStep += FADE_STEP;
			FadeColours();
			return true;
		}
		
		private bool HandleScreenFadeOut()
		{
			if (_introPicture == _introPictureNext) return false;
			if (FadeStep > 0.0F)
			{
				FadeStep -= FADE_STEP;
				FadeColours();
			}
			else
			{
				_introPicture = _introPictureNext;
				Palette = _pictures[_introPicture].Palette;
				FadeColours();
			}
			return true;
		}
		
		private bool HandleScreenFade()
		{
			if (_introPicture == _introPictureNext && HandleScreenFadeIn())
				return true;
			return HandleScreenFadeOut();
		}
		
		private void LogIntroText()
		{
			Log(@"Intro: ""{0}""", _introText[_introLine]);
		}
		
		/// <summary>
		/// When a step is due, in <see cref="_elapsedTicks"/> units.
		/// </summary>
		/// <param name="step">The step to look up; may run past <see cref="_pacedSteps"/>.</param>
		/// <returns>The tick the step is due at.</returns>
		/// <remarks>
		/// Every deadline is measured from the start rather than added up step by step, so rounding
		/// a single step never shifts the ones after it.
		/// </remarks>
		private int StepDeadline(int step) => (int)((long)step * _targetTicks / _pacedSteps);

		/// <summary>
		/// The colour the current line is drawn in: black just as it changes, dim for a tick on
		/// either side, and bright in between.
		/// </summary>
		/// <remarks>
		/// The phase is measured against the current step's own start and end, not against an
		/// average line length. A line lasts a whole number of ticks but the steps are spaced by a
		/// rounded fraction, so a fixed period would drift out of step with the line changes and
		/// blink somewhere in the middle of a line.
		/// </remarks>
		private byte TextColour
		{
			get
			{
				bool mapReady = Map.Ready;
				byte bright = mapReady ? (byte)10 : (byte)11;

				// Nothing follows the closing prompt, or the last line of text, that it could fade
				// out for, so it stays up instead of blinking.
				bool nextLineIsBlank = (_introLine + 1) < _introText.Length && string.IsNullOrEmpty(_introText[_introLine + 1]);
				if (_stepIndex >= _totalSteps || nextLineIsBlank) return bright;

				int lineStart = StepDeadline(_stepIndex);
				int lastPhase = StepDeadline(_stepIndex + 1) - lineStart - 1;
				int phase = _elapsedTicks - lineStart;

				if (phase <= 0 || phase > lastPhase) return 0;
				if (phase == 1 || phase == lastPhase) return mapReady ? (byte)2 : (byte)3;
				return bright;
			}
		}

		private void ShowMapNotReadyMessage()
		{
			_mapNotReadyMessageUntil = (RuntimeHandler.CurrentGameTick / 4) + MAP_NOT_READY_MESSAGE_TICKS;
		}

		private string GetGenerationStageLabel(int stageCode)
		{
			return stageCode switch
			{
				1 => Translate("Merging terrain and latitude"),
				2 => Translate("Applying climate adjustments"),
				3 => Translate("Applying age adjustments"),
				4 => Translate("Creating rivers"),
				5 => Translate("Calculating continent sizes"),
				6 => Translate("Creating poles"),
				7 => Translate("Placing goody huts"),
				8 => Translate("Calculating land value"),
				_ => Translate("Preparing map generation"),
			};
		}

		private string GetGenerationProgressText()
		{
			int stageCurrent = Math.Max(0, Map.GenerationStageCurrent);
			int stageTotal = Map.GenerationStageTotal;
			int stageCode = Map.GenerationStageCode;
			if (stageTotal <= 0)
			{
				if (Map.GenerationInProgress)
				{
					return Translate("Preparing map generation...");
				}

				return Translate("Waiting for map generation to start...");
			}

			string stageLabel = GetGenerationStageLabel(stageCode);
			int stageDisplay = Math.Clamp(stageCurrent, 1, stageTotal);
			return TranslateFormatted("{0} of {1}: {2}...", stageDisplay, stageTotal, stageLabel);
		}

		private bool TryOpenNewGame()
		{
			if (!Map.Ready)
			{
				ShowMapNotReadyMessage();
				return false;
			}

			_generationMusic.Stop();
			Destroy();
			Common.AddScreen(new NewGame());
			return true;
		}
		
		protected override bool HasUpdate(uint gameTick)
		{
			HandleMapGenerationError();
			HandleMapGenerationRetry();

			bool update = HandleScreenFade();
			if (!update && gameTick % 2 == 0)
			{
				_elapsedTicks++;

				// The intro holds on its last line (the "press a key to continue" prompt) instead
				// of moving on by itself; only an explicit key press (see KeyDown) starts the new
				// game, so no more steps are taken once the text is done.
				if (_stepIndex < _totalSteps && _elapsedTicks >= StepDeadline(_stepIndex + 1))
				{
					_stepIndex++;
					AdvanceLine();
				}

				switch (_introPicture)
				{
					case 0: this.Cycle(184, 176); break;
					case 1: this.Cycle(32, 47).Cycle(48, 63).Cycle(64, 79); break;
					case 2: this.Cycle(80, 95).Cycle(96, 111).Cycle(112, 127); break;
					case 3: this.Cycle(134, 139).Cycle(245, 250); break;
					case 4: this.Cycle(96, 102).Cycle(135, 140); break;
					case 5: this.Cycle(136, 138).Cycle(129, 130).Cycle(250, 254); break;
					case 6: this.Cycle(132, 134).Cycle(135, 138).Cycle(208, 210).Cycle(245, 249); break;
					case 7: this.Cycle(132, 134).Cycle(208, 210).Cycle(246, 249); break;
				}
			}
			else if (!update)
			{
				return false;
			}

			int x = (Width - 320) / 2;
			int y = (Height - 200) / 2;
			if (x != 0 || y != 0)
			{
				this.Clear(_pictures[_introPicture].Bitmap[0, 0])
					.FillRectangle(x, y, 320, 200, _pictures[_introPicture].Bitmap[10, 100])
					.AddLayer(_pictures[_introPicture], x, y);
			}
			else
			{
				this.AddLayer(_pictures[_introPicture]);
			}

			if (FadeStep < 1.0F) return true;

			int previousText = 0;
			string introLine = _introText[_introLine] ?? string.Empty;
			while (string.IsNullOrEmpty(introLine))
			{
				int previousTextIndex = _introLine - (++previousText);
				if (previousTextIndex < 0)
				{
					introLine = Translate(INTRO_ERROR_MESSAGE);
					break;
				}

				introLine = _introText[previousTextIndex] ?? string.Empty;
			}
			ShowHintText(x, y);
			if (_mapNotReadyMessageUntil > gameTick)
			{
				this.DrawText(Translate("Map generation is still running. Please wait..."), 1, 15, x + 160, y + 8, TextAlign.Center);
				this.DrawText(GetGenerationProgressText(), 1, 15, x + 160, y + 18, TextAlign.Center);
			}

			if (introLine == INTRO_END_MARKER)
			{
				introLine = Translate("Press Space, Enter, or Escape to continue...");
			}
			this.DrawText(introLine, 6, TextColour, x + 160, y + 160, TextAlign.Center);

			return true;
		}

		/// <summary>
		/// Moves to the next line of real text - exactly what <see cref="CountAdvanceSteps"/> counts
		/// as one step.
		/// </summary>
		private void AdvanceLine()
		{
			_introLine++;
			while (_introLine < _introText.Length && IsSkippedLine(_introText[_introLine]))
			{
				if (_introText[_introLine] == PICTURE_CHANGE_MARKER)
				{
					IntroPicture++;
				}
				_introLine++;
			}

			if (_introLine >= _introText.Length)
			{
				_introLine = _introText.Length - 1;
			}
			LogIntroText();
		}

		/// <summary>
		/// Whether a line is passed over rather than shown: a picture-change marker draws no text,
		/// and an empty line has none to draw. The original text pads its end with empty lines, and
		/// giving each of those a step of its own would spend a sixth of the intro on a screen that
		/// never changes.
		/// </summary>
		private static bool IsSkippedLine(string line)
			=> string.IsNullOrEmpty(line) || line == PICTURE_CHANGE_MARKER;

		/// <summary>
		/// Realigns the pacing state to a step index set from outside the automatic ticker, e.g. by
		/// manual navigation, so the next automatic step waits a full step's worth of ticks instead
		/// of firing immediately because <see cref="_elapsedTicks"/> ran ahead of it.
		/// </summary>
		private void ResyncStepTiming(int stepIndex)
		{
			_stepIndex = Math.Clamp(stepIndex, 0, _totalSteps);
			_elapsedTicks = StepDeadline(_stepIndex);
		}

		private void HandleMapGenerationRetry()
		{
			// Check if error dialog was closed and retry generation
			if (!_errorDialogShown || Common.HasScreenType<MessageBox>())
			{
				return;
			}
			Map.ResetForGenerationRetry();
			Map.Generate();
			_generationMusic.Start();
			_elapsedTicks = 0;
			_stepIndex = 0;
			_introLine = 1;
			_introPicture = 0;
			_introPictureNext = 0;
			FadeStep = 0.0F;
			_errorDialogShown = false;
		}

		private void HandleMapGenerationError()
		{
			// Handle map generation error
			if (!Map.Error || _errorDialogShown)
			{
				return;
			}
			Common.AddScreen(new MessageBox(
				Translate("Error generating map"),
				Translate("See logs for more information."),
				Translate("Retrying...")));
			_errorDialogShown = true;
		}

		private void ShowHintText(int x, int y)
		{
			if (_introLine == 1)
			{
				this.DrawText(Translate("Shift+Left/Right Forward/Backward"), 1, 15, x + 160, y + 190, TextAlign.Center);
			}
		}

		private static string[] NormalizeIntroText(string[] lines)
		{
			List<string> normalized = [.. lines];

			if (normalized.Count > 0)
			{
				string lastLine = normalized[^1];
				if (string.Equals(lastLine?.Trim(), "\u001A", StringComparison.Ordinal))
				{
					normalized[^1] = INTRO_END_MARKER;
				}
			}

			if (normalized.Count == 0 || normalized[^1] != INTRO_END_MARKER)
			{
				normalized.Add(INTRO_END_MARKER);
			}

			return [.. normalized];
		}

		public override bool KeyDown(KeyboardEventArgs args)
		{
			if (args.Shift)
			{
				if (FadeStep < 1.0F) return false;
				if (args.Key == Key.Left)
				{
					if (_introLine <= 1) return false;
					
					Log("Intro: <<");
					
					_introLine--;
					if (_introText[_introLine] == "_")
					{
						_introLine--;
						IntroPicture--;
					}
					else
					{
						LogIntroText();
					}
					ResyncStepTiming(_stepIndex - 1);
					return true;
				}
				if (args.Key == Key.Right)
				{
					if (_introLine >= _introText.Length - 1) return false;

					Log("Intro: >>");

					_introLine++;
					if (_introText[_introLine] == "_")
					{
						_introLine++;
						IntroPicture++;
					}
					else
					{
						LogIntroText();
					}
					ResyncStepTiming(_stepIndex + 1);
					return true;
				}
			}
			if (args.Key == Key.Space || args.Key == Key.Enter || args.Key == Key.Escape)
			{
				TryOpenNewGame();
				return true;
			}
			return false;
		}

		public void Resize(object? _, ResizeEventArgs __)
		{
			Bitmap.Clear();
			HasUpdate(0);
		}
		
		public Intro()
		{
			OnResize += Resize;
			FadeStep = 0.0F;
			
			_introText = NormalizeIntroText(TextFileFactory.LoadTextFile("STORY"));
			if (_introText.Length == 0)
			{
				_introText = new string[16];
				for (int i = 0; i < 16; i++)
				{
					_introText[i] = (i % 2) == 0 ? Translate("MISSING TEXT") : "_";
				}
			}
			_pictures = new Picture[8];
			for (int i = 0; i < _pictures.Length; i++)
				_pictures[i] = Resources[$"BIRTH{(i + 1)}"];

			Palette = _pictures[0].Palette;

			// The evolution music is not started here: it belongs to the world generation, which
			// starts before this screen is created and is where the music is started as well.

			(_totalSteps, _pacedSteps, _targetTicks) = CalculatePacing();
		}

		/// <summary>
		/// Sizes the text pacing so the last line of text appears <see cref="LEAD_OUT_SECONDS"/>
		/// before one pass of the evolution music ends, or before
		/// <see cref="DEFAULT_DURATION_SECONDS"/> have passed when that length is not known -
		/// regardless of how many lines the loaded text (or its translation) has.
		/// </summary>
		private (int totalSteps, int pacedSteps, int targetTicks) CalculatePacing()
		{
			(int totalSteps, int pictureChanges) = CountAdvanceSteps(_introText);

			// The closing prompt is the step after the last line of text, so it is not paced itself:
			// it follows one step later, at whatever rate the rest of the text runs at.
			int pacedSteps = totalSteps - 1;
			if (pacedSteps <= 0) return (totalSteps, 1, DEFAULT_TICKS_PER_LINE);

			double targetSeconds = _generationMusic.TryGetDuration(out TimeSpan duration) && duration > TimeSpan.Zero
				? duration.TotalSeconds
				: DEFAULT_DURATION_SECONDS;

			// Fading between the pictures freezes the text - HasUpdate only advances it while no
			// fade is running - so that time has to come out of the budget, or the text ends up
			// running well past the music.
			double budgetSeconds = targetSeconds - LEAD_OUT_SECONDS - FadeSeconds(pictureChanges);

			// Every line needs room for the black tick it changes on plus at least one lit tick.
			int targetTicks = Math.Max(pacedSteps * 2, (int)Math.Round(budgetSeconds * INTRO_TICKS_PER_SECOND));

			return (totalSteps, pacedSteps, targetTicks);
		}

		/// <summary>
		/// How long the screen spends fading, which is time the text does not advance in.
		/// </summary>
		/// <param name="pictureChanges">How many times the picture is swapped.</param>
		/// <returns>The total fading time in seconds.</returns>
		/// <remarks>
		/// One fade step happens per <see cref="HasUpdate"/> call. The screen fades in once at the
		/// start, and every picture change fades the old picture out, swaps it in a step of its own,
		/// then fades the new one in.
		/// </remarks>
		private static double FadeSeconds(int pictureChanges)
		{
			int stepsPerDirection = (int)Math.Ceiling(1.0F / FADE_STEP);
			int updates = stepsPerDirection + (pictureChanges * ((2 * stepsPerDirection) + 1));

			return updates / SCREEN_UPDATES_PER_SECOND;
		}

		/// <summary>
		/// Counts how many times <see cref="AdvanceLine"/> fires while walking from the first line
		/// to the last, and how many pictures are swapped on the way.
		/// </summary>
		private static (int steps, int pictureChanges) CountAdvanceSteps(string[] introText)
		{
			int steps = 0;
			int pictureChanges = 0;
			int line = 1;

			while (line < introText.Length - 1)
			{
				line++;
				while (line < introText.Length && IsSkippedLine(introText[line]))
				{
					if (introText[line] == PICTURE_CHANGE_MARKER)
					{
						pictureChanges++;
					}
					line++;
				}

				if (line >= introText.Length)
				{
					line = introText.Length - 1;
				}
				steps++;
			}

			return (steps, pictureChanges);
		}
	}
}