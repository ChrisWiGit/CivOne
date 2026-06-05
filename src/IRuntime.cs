// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.IO;

namespace CivOne
{
	public interface IRuntime
	{
		event EventHandler Initialize, Draw;
		event EventHandler<UpdateEventArgs> Update;
		event EventHandler<KeyboardEventArgs> KeyboardUp, KeyboardDown;
		event EventHandler<ScreenEventArgs> MouseUp, MouseDown, MouseMove, MouseWheel;
		Platform CurrentPlatform { get; }
		string StorageDirectory { get; }
		string? GetSetting(string key);
		void SetSetting(string key, string value);
		RuntimeSettings Settings { get; }
		void SetCurrentCursor(MouseCursor? cursor);
		Bytemap[]? Layers { get; set; }
		Palette? Palette { get; set; }
		void SetCursor(IBitmap? cursor);
		int CanvasWidth { get; }
		int CanvasHeight { get; }
		int WindowWidth { get; }
		int WindowHeight { get; }
		void Log(string text, params object[] parameters);
		string? BrowseFolder(string caption = "");
		string? FileChooser(bool save, string title, string initialFileName, string filter);
		void SetWindowTitle(string title);
		void PlaySound(string file);
		void StopSound();
		bool TryOpenUrl(string url, out string? errorMessage);
		bool TryCopyToClipboard(string text, out string? errorMessage);
		void Quit();
	}
}