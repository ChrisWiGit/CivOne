using System;
using System.IO;
using System.Linq;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Screens;
using CivOne.Tasks;

namespace CivOne.Services
{
	internal sealed class QuickSaveLoadHotkeyService : IQuickSaveLoadHotkeyService
	{
		private readonly IRuntime _runtime;
		private readonly ITranslationService _translation;
		private readonly Action<string, object[]> _log;
		private readonly Action<string> _saveCosAction;
		private readonly Func<string, bool> _loadCosAction;
		private readonly Action _rebuildGamePlayAction;
		private readonly Func<bool> _canQuickSave;
		private readonly Action<string> _showUserErrorAction;

		public QuickSaveLoadHotkeyService(
			IRuntime runtime,
			ITranslationService translationService,
			IYamlSaveGameServiceFactory yamlSaveGameServiceFactory = null,
			Action<string, object[]> log = null,
			Action<string> saveCosAction = null,
			Func<string, bool> loadCosAction = null,
			Action rebuildGamePlayAction = null,
			Func<bool> canQuickSave = null,
			Action<string> showUserErrorAction = null)
		{
			_runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
			_translation = translationService ?? throw new ArgumentNullException(nameof(translationService));
			if (saveCosAction == null && yamlSaveGameServiceFactory == null)
			{
				throw new ArgumentNullException(nameof(yamlSaveGameServiceFactory));
			}
			_log = log ?? ((text, parameters) => _runtime.Log(text, parameters));
			_saveCosAction = saveCosAction ?? (filePath => yamlSaveGameServiceFactory.Create(Game.Instance).SaveCos(filePath));
			_loadCosAction = loadCosAction ?? Game.LoadYamlGame;
			_rebuildGamePlayAction = rebuildGamePlayAction ?? RebuildGamePlay;
			_canQuickSave = canQuickSave ?? (() => Game.Started && Game.Instance != null);
			_showUserErrorAction = showUserErrorAction ?? ShowError;
		}

		public bool TryHandle(KeyboardEventArgs args)
		{
			if (!TryGetSlot(args.Key, out var slot))
			{
				return false;
			}

			if (args.Modifier == KeyModifier.Control)
			{
				TryQuickSave(slot);
				return true;
			}

			if (args.Modifier == KeyModifier.Alt)
			{
				TryQuickLoad(slot);
				return true;
			}

			return false;
		}

		private static bool TryGetSlot(Key key, out int slot)
		{
			slot = key switch
			{
				Key.F1 => 1,
				Key.F2 => 2,
				Key.F3 => 3,
				Key.F4 => 4,
				Key.F5 => 5,
				Key.F6 => 6,
				Key.F7 => 7,
				Key.F8 => 8,
				Key.F9 => 9,
				Key.F10 => 10,
				_ => 0
			};

			return slot > 0;
		}

		private string GetSlotFilePath(int slot)
		{
			Directory.CreateDirectory(_runtime.StorageDirectory);
			return Path.Combine(_runtime.StorageDirectory, $"fastsave_f{slot}.cos");
		}

		private void TryQuickSave(int slot)
		{
			if (!_canQuickSave())
			{
				_showUserErrorAction(_translation.Translate("Fast save is not available right now."));
				return;
			}

			var filePath = GetSlotFilePath(slot);
			try
			{
				_saveCosAction(filePath);
				_log("Fast save completed: slot F{0} -> {1}", [slot, filePath]);
			}
			catch (Exception ex)
			{
				_showUserErrorAction(_translation.Translate("Could not save fast save slot."));
				_log("Fast save failed for slot F{0} ({1}): {2}", [slot, filePath, ex]);
			}
		}

		private void TryQuickLoad(int slot)
		{
			var filePath = GetSlotFilePath(slot);
			if (!File.Exists(filePath))
			{
				_showUserErrorAction(_translation.Translate("Fast save slot is empty."));
				_log("Fast load failed for slot F{0}: file does not exist ({1})", [slot, filePath]);
				return;
			}

			try
			{
				if (!_loadCosAction(filePath))
				{
					_showUserErrorAction(_translation.Translate("Could not load fast save slot."));
					_log("Fast load failed for slot F{0}: loader returned false ({1})", [slot, filePath]);
					return;
				}

				_rebuildGamePlayAction();
				_log("Fast load completed: slot F{0} <- {1}", [slot, filePath]);
			}
			catch (Exception ex)
			{
				_showUserErrorAction(_translation.Translate("Could not load fast save slot."));
				_log("Fast load failed for slot F{0} ({1}): {2}", [slot, filePath, ex]);
			}
		}

		private void RebuildGamePlay()
		{
			GameTask.ClearAll();

			foreach (var screen in Common.Screens.ToArray())
			{
				Common.DestroyScreen(screen);
			}

			Common.AddScreen(new GamePlay());
		}

		private void ShowError(string message)
		{
			GameTask.Enqueue(Message.Error(_translation.Translate("Quick Save/Load"), message));
		}
	}
}