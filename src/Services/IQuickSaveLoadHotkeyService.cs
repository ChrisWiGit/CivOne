using CivOne.Events;

namespace CivOne.Services
{
	internal interface IQuickSaveLoadHotkeyService
	{
		bool TryHandle(KeyboardEventArgs args);
	}
}