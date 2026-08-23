using System;

namespace CivOne.Services
{
	/// <summary>
	/// Service for saving games in YAML format. This is used for both manual saves and autosaves when the YAML save format is selected.
	/// The implementation should handle the specifics of how to serialize the game state into YAML and write it to the specified file path. 
	/// It should also ensure that the save operation is atomic, meaning that the file is either fully written or not written at all in case of an error.
	/// </summary>
	public interface IYamlSaveGameService
	{
		/// <summary>
		/// Saves the current game state to a .cos file in YAML format at the specified file path. 
		/// `cos` is short for "CivOne Savegame".
		/// </summary>
		/// <param name="filePath"></param>
		void SaveCos(string filePath);
	}
}
