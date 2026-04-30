using System;
using System.IO;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.IO;
using CivOne.Services;
using Xunit;

namespace CivOne.UnitTests
{
	public sealed class QuickSaveLoadHotkeyServiceTests : IDisposable
	{
		private readonly string _storageDirectory;
		private readonly FakeRuntime _runtime;

		public QuickSaveLoadHotkeyServiceTests()
		{
			_storageDirectory = Path.Combine(Path.GetTempPath(), "CivOneTests", Guid.NewGuid().ToString("N"));
			_runtime = new FakeRuntime(_storageDirectory);
		}

		[Fact]
		public void TryHandle_ControlF1_SavesToSlotFile()
		{
			string savedPath = null;
			var service = CreateService(
				canQuickSave: () => true,
				save: path => savedPath = path,
				load: _ => true,
				rebuild: () => { },
				onError: _ => { });

			bool handled = service.TryHandle(new KeyboardEventArgs(Key.F1, KeyModifier.Control));

			Assert.True(handled);
			Assert.Equal(Path.Combine(_storageDirectory, "fastsave_f1.cos"), savedPath);
		}

		[Fact]
		public void TryHandle_AltF2_LoadsSlotAndRebuildsGameplay()
		{
			string slotPath = Path.Combine(_storageDirectory, "fastsave_f2.cos");
			Directory.CreateDirectory(_storageDirectory);
			File.WriteAllText(slotPath, "test");

			string loadedPath = null;
			int rebuildCalls = 0;
			var service = CreateService(
				canQuickSave: () => true,
				save: _ => { },
				load: path =>
				{
					loadedPath = path;
					return true;
				},
				rebuild: () => rebuildCalls++,
				onError: _ => { });

			bool handled = service.TryHandle(new KeyboardEventArgs(Key.F2, KeyModifier.Alt));

			Assert.True(handled);
			Assert.Equal(slotPath, loadedPath);
			Assert.Equal(1, rebuildCalls);
		}

		[Fact]
		public void TryHandle_AltSlotWithoutFile_DoesNotInvokeLoader()
		{
			int loadCalls = 0;
			var service = CreateService(
				canQuickSave: () => true,
				save: _ => { },
				load: _ =>
				{
					loadCalls++;
					return true;
				},
				rebuild: () => { },
				onError: _ => { });

			bool handled = service.TryHandle(new KeyboardEventArgs(Key.F3, KeyModifier.Alt));

			Assert.True(handled);
			Assert.Equal(0, loadCalls);
		}

		[Fact]
		public void TryHandle_UnmodifiedOrShiftFunctionKey_IsNotHandled()
		{
			var service = CreateService(
				canQuickSave: () => true,
				save: _ => { },
				load: _ => true,
				rebuild: () => { },
				onError: _ => { });

			bool unmodifiedHandled = service.TryHandle(new KeyboardEventArgs(Key.F4, KeyModifier.None));
			bool shiftHandled = service.TryHandle(new KeyboardEventArgs(Key.F4, KeyModifier.Shift));

			Assert.False(unmodifiedHandled);
			Assert.False(shiftHandled);
		}

		public void Dispose()
		{
			if (Directory.Exists(_storageDirectory))
			{
				Directory.Delete(_storageDirectory, true);
			}
		}

		private QuickSaveLoadHotkeyService CreateService(
			Func<bool> canQuickSave,
			Action<string> save,
			Func<string, bool> load,
			Action rebuild,
			Action<string> onError)
			=> new(
				_runtime,
				TranslationServiceFactory.CreateDefault(),
				null,
				(_, _) => { },
				save,
				load,
				rebuild,
				canQuickSave,
				onError);

		private sealed class FakeRuntime(string storageDirectory) : IRuntime
		{
			public event EventHandler Initialize { add { } remove { } }
			public event EventHandler Draw { add { } remove { } }
			public event UpdateEventHandler Update { add { } remove { } }
			public event KeyboardEventHandler KeyboardUp { add { } remove { } }
			public event KeyboardEventHandler KeyboardDown { add { } remove { } }
			public event ScreenEventHandler MouseUp { add { } remove { } }
			public event ScreenEventHandler MouseDown { add { } remove { } }
			public event ScreenEventHandler MouseMove { add { } remove { } }

			public Platform CurrentPlatform => Platform.Windows;
			public string StorageDirectory { get; } = storageDirectory;
			public RuntimeSettings Settings { get; } = new RuntimeSettings();
			public MouseCursor CurrentCursor { set { } }
			public Bytemap[] Layers { get; set; }
			public Palette Palette { get; set; }
			public IBitmap Cursor { set { } }
			public int CanvasWidth => 320;
			public int CanvasHeight => 200;
			public string WindowTitle { set { } }

			public string GetSetting(string key) => null;
			public void SetSetting(string key, string value) { }
			public void Log(string text, params object[] parameters) { }
			public string BrowseFolder(string caption = "") => string.Empty;
			public string FileChooser(bool save, string title, string initialFileName, string filter) => string.Empty;
			public void PlaySound(string file) { }
			public void StopSound() { }
			public void Quit() { }
		}
	}
}