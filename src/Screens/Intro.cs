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

namespace CivOne.Screens
{
	[ScreenResizeable]
	internal class Intro : BaseScreen
	{
		private const float FADE_STEP = 0.0625F;
		private const uint MAP_NOT_READY_MESSAGE_TICKS = 60;
		private const string INTRO_END_MARKER = "\0";
		
		private readonly string[] _introText;
		private readonly Picture[] _pictures;

		private int _introTicks;
		private int _introLine = 1;
		
		private int _introPicture;
		private int _introPictureNext;
		private uint _mapNotReadyMessageUntil;
		private bool _errorDialogShown;
        
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
		
		private byte TextColour
		{
			get
			{
				bool mapReady = Map.Ready;
				if (_introTicks % 30 > 1 && _introTicks % 30 < 29 || ((_introLine + 1) < _introText.Length && _introText[_introLine + 1] == string.Empty)) return mapReady ? (byte)10 : (byte)11;
				if (_introTicks % 30 == 1 || _introTicks % 30 == 29) return mapReady ? (byte)2 : (byte)3;
				return 0;
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
			int stageTotal = Math.Max(1, Map.GenerationStageTotal);
			int stageCode = Map.GenerationStageCode;
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
				_introTicks++;
				if (_introTicks % 30 == 0)
				{
					_introLine++;
					if (_introLine >= _introText.Length)
					{
						if (TryOpenNewGame())
						{
							return true;
						}
						_introLine = _introText.Length - 1;
						_introTicks = 1;
					}
					if (_introText[_introLine] == "_")
					{
						IntroPicture++;
						_introLine++;
					}
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
			string introLine = _introText[_introLine];
			while (introLine == string.Empty)
				introLine = _introText[_introLine - (++previousText)];
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

			if (_introTicks % 30 == 1) LogIntroText();
			return true;
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
			_introTicks = 0;
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
						_introTicks = 0;
						IntroPicture--;
					}
					else
					{
						LogIntroText();
					}
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
						_introTicks = 0;
						IntroPicture++;
					}
					else
					{
						LogIntroText();	
					}
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

		public void Resize(object sender, ResizeEventArgs args)
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
		}
	}
}