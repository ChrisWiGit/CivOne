namespace CivOne.Sound.Cvl;

/// <summary>A sound pack as listed for selection: its folder name and its display name.</summary>
/// <param name="PackId">Folder name and pack id, e.g. "pc-speaker".</param>
/// <param name="DisplayName">Name shown in the settings, e.g. "PC Speaker".</param>
internal readonly record struct SoundPackSummary(string PackId, string DisplayName);
