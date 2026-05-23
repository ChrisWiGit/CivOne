using System;
using System.IO;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.IO;

namespace CivOne.UnitTests
{
    public class MockRuntime : IRuntime, IDisposable
    {
        public event EventHandler Initialize { add { } remove { } }
        public event EventHandler Draw { add { } remove { } }
        public event UpdateEventHandler Update { add { } remove { } }
        public event KeyboardEventHandler KeyboardUp { add { } remove { } }
        public event KeyboardEventHandler KeyboardDown { add { } remove { } }
        public event ScreenEventHandler MouseUp { add { } remove { } }
        public event ScreenEventHandler MouseDown { add { } remove { } }
        public event ScreenEventHandler MouseMove { add { } remove { } }
        public Platform CurrentPlatform { get; }

        public string StorageDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CivOne");

        public string GetSetting(string key)
        {
            if (key == "GraphicsMode")
                return GraphicsMode.Graphics256.ToString();
            return null;
        }

        public void SetSetting(string key, string value)
        {
            throw new NotImplementedException();
        }

        public RuntimeSettings Settings { get; }
        public MouseCursor CurrentCursor { get; set; }
        public Bytemap[] Layers { get; set; }
        public Palette Palette { get; set; }
        public IBitmap Cursor { get; set; }
        public int CanvasWidth { get; }
        public int CanvasHeight { get; }

        //private static Mutex _mutex = new Mutex();

        public void Log(string text, params object[] parameters)
        {
            // Use dotnet test -p:SuppressConsoleLogs=true to silence console output here.
            Console.WriteLine(text, parameters);
        }

        public string BrowseFolder(string caption = "")
        {
            throw new NotImplementedException();
        }

        public string WindowTitle { get; set; }
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

        public void Dispose()
        {
        }

		public string FileChooser(bool save, string title, string initialFileName, string filter)
		{
			throw new NotImplementedException();
		}

		public MockRuntime(RuntimeSettings settings)
        {
            Settings = settings;
            // TODO fire-eggs this needs to be false if you want to use Earth! and must have a pointer to the Civ data files!
            settings.Free = false;
            RuntimeHandler.Wipe(); // Ensure any previous runtime is cleared out otherwise exceptions occur
            RuntimeHandler.RegisterForTest(this);
        }
    }
}
