// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Drawing;
using System.IO;
using CivOne.Enums;
using CivOne.Events;
using CivOne.IO;
using CivOne.Graphics;

namespace CivOne
{
	internal class Runtime : IRuntime, IDisposable
	{
		public Profile Profile { get; }
		
		internal static Size CanvasSize { get; set; }

		internal bool SignalQuit { get; private set; }

		internal void InvokeInitialize() => Initialize?.Invoke(this, EventArgs.Empty);
		internal void InvokeDraw() => Draw?.Invoke(this, EventArgs.Empty);
		internal void InvokeUpdate(ref UpdateEventArgs args) => Update?.Invoke(this, args);
		internal void InvokeKeyboardUp(KeyboardEventArgs args) => KeyboardUp?.Invoke(this, args);
		internal void InvokeKeyboardDown(KeyboardEventArgs args) => KeyboardDown?.Invoke(this, args);
		internal void InvokeMouseUp(ScreenEventArgs args) => MouseUp?.Invoke(this, args);
		internal void InvokeMouseDown(ScreenEventArgs args) => MouseDown?.Invoke(this, args);
		internal void InvokeMouseMove(ScreenEventArgs args) => MouseMove?.Invoke(this, args);

		public event EventHandler Initialize, Draw;
		public event UpdateEventHandler Update;
		public event KeyboardEventHandler KeyboardUp, KeyboardDown;
		public event ScreenEventHandler MouseUp, MouseDown, MouseMove;
		internal event EventHandler CursorChanged;
		internal event Action<string> PlaySound;
		internal event Action StopSound;
		internal event Action<string> SetWindowTitle;
		
		public RuntimeSettings Settings { get; private set; }
		public MouseCursor CurrentCursor { internal get; set; }
		private Bytemap[] _layers = [];
		public Bytemap[] Layers
		{
			get => _layers;
			set
			{
				// Snapshot incoming array and normalize null to empty, so render loop
				// doesn't race against external callers mutating the same instance.
				_layers = value is null ? [] : [..value];
			}
		}
		private Palette _palette;
		public Palette Palette
		{
			get => _palette;
			set
			{
				if (_palette == value) return;
				// Refactor note: dispose the previous palette when swapping instances so unmanaged
				// buffers are released immediately instead of waiting for GC/finalizer.
				(_palette as IDisposable)?.Dispose();
				_palette = value;
			}
		}
		private IBitmap _cursor;
		public IBitmap Cursor
		{
			internal get => _cursor;
			set
			{
				_cursor = value;
				CursorChanged?.Invoke(this, EventArgs.Empty);
			}
		}


		private readonly object _sync = new();

		private StreamWriter TryOpenWrite(string path)
		{
			for (int i = 0; i < 3; i++)
			{
				try				
				{
					return new StreamWriter(new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite));
				}
				catch (IOException ex) when (IsSharingViolation(ex))
				{
					// Retry only when the file is temporarily locked by another process/handle.
					Console.Error.WriteLine($"Failed to open log file for writing (attempt {i + 1}/3, sharing violation): {path}");
					System.Threading.Thread.Sleep(TimeSpan.FromMilliseconds(250));
				}
				catch (IOException ex)
				{
					Console.Error.WriteLine($"Failed to open log file for writing: {path} ({ex.Message})");
					return null;
				}
			}
			return null;
		}

		private static bool IsSharingViolation(IOException ex)
		{
			// HRESULT 0x80070020 = ERROR_SHARING_VIOLATION, 0x80070021 = ERROR_LOCK_VIOLATION
			int code = ex.HResult & 0xFFFF;
			return code == 32 || code == 33;
		}

        public void Log(string text, params object[] parameters)
        {
			// civone local folder verwenden
			var storage = ((IRuntime)this).StorageDirectory;
            var path = Path.Combine(storage,"Civ.log");

			lock (_sync)
			{
				using StreamWriter tw = TryOpenWrite(path);
				if (tw != null)
				{
					tw.WriteLine(text, parameters);
					tw.Flush();
					tw.Close();
				}
			}
			// Re-read once to avoid TOCTOU NPE if Settings is disposed between checks (shutdown race).
			RuntimeSettings settings = Settings;
			if (settings?.ConsoleLogging != true)
			{
				return;
			}

			if (settings.McpEnabled)
			{
				Console.Error.WriteLine(text, parameters);
				Console.Error.Flush();
			}
			else
			{
				Console.WriteLine(text, parameters);
			}
        }

		Platform IRuntime.CurrentPlatform => Platform.Windows;
		string IRuntime.StorageDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CivOne");
		string IRuntime.GetSetting(string key) => Profile.GetSetting(key);
		void IRuntime.SetSetting(string key, string value) => Profile.SetSetting(key, value);
		void IRuntime.SetCurrentCursor(MouseCursor cursor) => CurrentCursor = cursor;
		void IRuntime.SetCursor(IBitmap cursor) => Cursor = cursor;
		int IRuntime.CanvasWidth => CanvasSize.Width;
		int IRuntime.CanvasHeight => CanvasSize.Height;
		
		string? IRuntime.BrowseFolder(string caption) => Native.FolderBrowser(caption);
		string IRuntime.FileChooser(bool save, string title, string initialFileName, string filter) => Native.FileChooser(save, title, initialFileName, filter);
		void IRuntime.SetWindowTitle(string title) => SetWindowTitle?.Invoke(title);
		void IRuntime.PlaySound(string file) => PlaySound?.Invoke(file);
		void IRuntime.StopSound() => StopSound?.Invoke();
		void IRuntime.Quit() => SignalQuit = true;

		public Runtime(RuntimeSettings runtimeSettings)
		{	
			Settings = runtimeSettings;
			Profile = Profile.Get(this, runtimeSettings.Get<string>("profile-name"));
			RuntimeHandler.Register(this);
		}

		public void Dispose()
		{
			if (_disposed) return;
			_disposed = true;
			RuntimeHandler.Shutdown();
		}

		private bool _disposed;
	}
}