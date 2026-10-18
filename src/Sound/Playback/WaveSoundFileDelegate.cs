using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using CivOne.Sound.Cvl;

namespace CivOne.Sound.Playback;

/// <summary>
/// Finds the wave file that belongs to a sound name.
/// </summary>
/// <remarks>
/// <para>
/// A file named after the sound itself always wins, so anything the player or a plugin drops into
/// the profile's sounds folder takes precedence. Only when there is none are the older file names
/// tried - see <see cref="LegacyWaveNameDelegate"/>. A name with neither is simply silent.
/// </para>
/// <para>
/// The folder is listed once per call and the names are matched here rather than by a search
/// pattern, because the file names of the original game are upper case and a file system that
/// tells <c>OPENING.WAV</c> and <c>opening.wav</c> apart would otherwise never find them.
/// </para>
/// </remarks>
internal sealed class WaveSoundFileDelegate
{
    private readonly LegacyWaveNameDelegate _legacyNames = new();
    private readonly string? _soundsDirectory;

    private string SoundsDirectory => _soundsDirectory ?? Settings.Instance.SoundsDirectory;

    /// <summary>
    /// Creates the delegate.
    /// </summary>
    /// <param name="soundsDirectory">
    /// Folder to look in, or <c>null</c> for the profile's own sounds folder. Resolved on first use
    /// rather than here, so creating this never needs a running game.
    /// </param>
    public WaveSoundFileDelegate(string? soundsDirectory = null) => _soundsDirectory = soundsDirectory;

    /// <summary>
    /// Finds the wave file a sound name plays.
    /// </summary>
    /// <param name="soundName">The name the game logic asked for.</param>
    /// <param name="path">Full path of the wave file, when there is one.</param>
    /// <returns><c>true</c> when a file was found.</returns>
    public bool TryResolve(string soundName, [NotNullWhen(true)] out string? path)
    {
        path = null;
        if (string.IsNullOrEmpty(soundName)) return false;

        Dictionary<string, string>? files = ReadSoundsFolder();
        if (files == null) return false;

        return TryResolve(files, soundName, out path);
    }

    /// <summary>
    /// Lists the sounds the profile's wave files can actually play.
    /// </summary>
    /// <remarks>
    /// A collection of wave files rarely covers everything - the original's Windows release shipped
    /// no file for the short leader jingles at all - so this is what the sound test offers rather
    /// than the full catalog.
    /// </remarks>
    /// <returns>Name and file path per playable sound, in the catalog's order.</returns>
    public IReadOnlyList<(string Name, string Path)> Available()
    {
        Dictionary<string, string>? files = ReadSoundsFolder();
        if (files == null) return [];

        var available = new List<(string, string)>();

        foreach (CvlTuneDefinition tune in CvlTuneCatalog.Tunes)
        {
            if (TryResolve(files, tune.Name, out string? path)) available.Add((tune.Name, path));
        }

        return available;
    }

    private bool TryResolve(Dictionary<string, string> files, string soundName, [NotNullWhen(true)] out string? path)
    {
        if (files.TryGetValue($"{soundName}.wav", out path)) return true;

        foreach (string legacyName in _legacyNames.Candidates(soundName))
        {
            if (files.TryGetValue($"{legacyName}.wav", out path)) return true;
        }

        path = null;
        return false;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "A folder that cannot be listed - removed, permissions - must not take the game's sound down; there is simply no wave file then.")]
    private Dictionary<string, string>? ReadSoundsFolder()
    {
        string soundsDirectory = SoundsDirectory;
        if (!Directory.Exists(soundsDirectory)) return null;

        try
        {
            var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (string file in Directory.EnumerateFiles(soundsDirectory))
            {
                files.TryAdd(Path.GetFileName(file), file);
            }

            return files;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
