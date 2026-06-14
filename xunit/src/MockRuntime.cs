using System;
using System.Collections.Generic;
using System.IO;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.IO;

namespace CivOne.UnitTests
{
    #pragma warning disable CS0067 // Events are never used - but required to implement IRuntime
    public sealed class MockRuntime : IRuntime
    {
        private readonly Dictionary<string, string> _settings = new(StringComparer.OrdinalIgnoreCase);

        public event EventHandler Initialize;
        public event EventHandler Draw;
        public event EventHandler<UpdateEventArgs> Update;
        public event EventHandler<KeyboardEventArgs> KeyboardUp;
        public event EventHandler<KeyboardEventArgs> KeyboardDown;
        public event EventHandler<ScreenEventArgs> MouseUp;
        public event EventHandler<ScreenEventArgs> MouseDown;
        public event EventHandler<ScreenEventArgs> MouseMove;
        public event EventHandler<ScreenEventArgs> MouseWheel;
        public Platform CurrentPlatform { get; }

        public string StorageDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CivOne");

        public string? GetSetting(string key)
        {
            return _settings.TryGetValue(key, out string? value) ? value : null;
        }

        public void SetSetting(string key, string value)
        {
            _settings[key] = value;
        }

        public RuntimeSettings Settings { get; }
        public MouseCursor? CurrentCursor { get; private set; }
        public Bytemap[]? Layers { get; set; }
        public Palette? Palette { get; set; }
        public IBitmap? Cursor { get; private set; }
        public int CanvasWidth { get; }
        public int CanvasHeight { get; }
        public int WindowWidth { get; }
        public int WindowHeight { get; }

        public bool TryOpenUrl(string url, out string? errorMessage)
        {
            errorMessage = null;
            return false;
        }

        public bool TryCopyToClipboard(string text, out string? errorMessage)
        {
            errorMessage = null;
            return false;
        }

        public void SetCurrentCursor(MouseCursor? cursor) => CurrentCursor = cursor;
        public void SetCursor(IBitmap? cursor) => Cursor = cursor;

        public void Log(string text, params object[] parameters)
        {
            // Use dotnet test -p:SuppressConsoleLogs=true to silence console output here.
            Console.WriteLine(text, parameters);
        }

        public string? BrowseFolder(string caption = "")
        {
            throw new NotImplementedException();
        }

        public string WindowTitle { get; private set; }
        public void SetWindowTitle(string title) => WindowTitle = title;
        public void PlaySound(string file)
        {
            // ignore
        }

        public void StopSound()
        {
            // ignore
        }

        public void Quit()
        {
            // ignore
        }

    
        #pragma warning disable CA1822
        public void Dispose()
        {
			// No resources to release in this test double.
        }
        #pragma warning restore CA1822 // Mark members as static - but these are required to implement IRuntime

		public string FileChooser(bool save, string title, string initialFileName, string filter)
		{
			throw new NotImplementedException();
		}

		public MockRuntime(RuntimeSettings settings)
        {
            Settings = settings;
            _settings["GraphicsMode"] = GraphicsMode.Graphics256.ToString();
            settings.Free = false;
            RuntimeHandler.Wipe(); // Ensure any previous runtime is cleared out otherwise exceptions occur
            RuntimeHandler.RegisterForTest(this);

            CurrentPlatform = Platform.Windows;
            CanvasWidth = 320;
            CanvasHeight = 200;
            WindowWidth = 320;
            WindowHeight = 200;

            // This will hopefully cause exceptions in tests.
            Initialize = null!;
            Draw = null!;
            Update = null!;
            KeyboardUp = null!;
            KeyboardDown = null!;
            MouseUp = null!;
            MouseDown = null!;
            MouseMove = null!;
            MouseWheel = null!;
            WindowTitle = string.Empty;
        }
    }
}
