using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.IO;

namespace CivOne.UnitTests
{
    public class MockRuntime : IRuntime, IDisposable
    {
        public event EventHandler Initialize;
        public event EventHandler Draw;
        public event UpdateEventHandler Update;
        public event KeyboardEventHandler KeyboardUp;
        public event KeyboardEventHandler KeyboardDown;
        public event ScreenEventHandler MouseUp;
        public event ScreenEventHandler MouseDown;
        public event ScreenEventHandler MouseMove;
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
            // TODO tests apparently keep stepping on each other trying to access the log file; using mutex lock doesn't seem to help
#if false
            var path = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Civ.log");
            //_mutex.WaitOne();
            using (TextWriter tw = new StreamWriter(path, append: true))
            {
                tw.WriteLine(text, parameters);
                tw.Flush();
                tw.Close();
            }
            //_mutex.ReleaseMutex();
#endif

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
