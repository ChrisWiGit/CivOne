namespace CivOne.Sound.Cvl;

/// <summary>
/// One tune of a CVL driver: its number in the driver, the name the game plays it by, and how it
/// behaves.
/// </summary>
/// <param name="TuneId">Number the CVL dispatch table addresses this tune by.</param>
/// <param name="Name">Name from <see cref="SoundNames"/> that plays this tune.</param>
/// <param name="Title">English display title, shown in the sound test.</param>
/// <param name="IsMusic">Whether this is a music piece rather than a short sound effect.</param>
internal sealed record CvlTuneDefinition(int TuneId, string Name, string Title, bool IsMusic);
