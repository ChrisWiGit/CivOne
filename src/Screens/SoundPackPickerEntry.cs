namespace CivOne.Screens;

/// <summary>
/// One entry of the sound pack picker.
/// </summary>
/// <param name="PackId">Id written to the sound pack setting.</param>
/// <param name="Label">Text shown to the player.</param>
internal sealed record SoundPackPickerEntry(string PackId, string Label);
