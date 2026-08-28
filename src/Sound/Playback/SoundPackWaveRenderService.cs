using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using CivOne.Sound.Cvl;

namespace CivOne.Sound.Playback;



/// <summary>
/// Turns a tune of a sound pack into a wave file the runtime can play, and keeps the result.
/// </summary>
/// <remarks>
/// <para>
/// Rendering an emulated sound chip is far too slow to do while the game waits for a sound, so
/// every tune is rendered once into <c>wav-cache/</c> next to the pack. The cache is rebuilt when
/// the score changes or when this renderer changes.
/// </para>
/// <para>
/// <see cref="Render"/> may be called from several threads at once, as long as no two calls render
/// the same tune and arrangement - two of those would write the same file. Keeping them apart is
/// <see cref="SoundPackRenderQueue"/>'s job.
/// </para>
/// </remarks>
internal sealed class SoundPackWaveRenderService
{
    /// <summary>Folder inside a pack that holds the rendered wave files.</summary>
    public const string CacheFolderName = "wav-cache";

    /// <summary>
    /// Bumped whenever a change here would make an already cached file sound wrong. Old files are
    /// then simply not found again and get rendered anew.
    /// </summary>
    private const int RendererVersion = 3;

    private readonly TuneRendererFactory _renderers;
    private readonly WaveFileWriter _writer = new();
    private readonly PcmMixerDelegate _mixer = new();

    /// <summary>
    /// Creates the service.
    /// </summary>
    /// <param name="renderers">
    /// Factory that supplies the per-device renderers, or <c>null</c> for the built-in ones.
    /// </param>
    public SoundPackWaveRenderService(TuneRendererFactory? renderers = null)
        => _renderers = renderers ?? new TuneRendererFactory();

    /// <summary>
    /// Renders a tune, or returns the cached file when one is already up to date.
    /// </summary>
    /// <param name="packFolder">Folder of the sound pack.</param>
    /// <param name="fileName">File name of the tune inside that folder.</param>
    /// <param name="arrangement">Which arrangement to render; ignored by packs that have only one.</param>
    /// <returns>Path of the wave file, or <c>null</c> when the tune could not be rendered.</returns>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "A single unreadable or unrenderable tune must not take the game's sound down; it is reported and skipped.")]
    public string? Render(string packFolder, string fileName, int arrangement = 0)
    {
        string sourcePath = Path.Combine(packFolder, fileName);
        if (!File.Exists(sourcePath)) return null;

        SoundPackIndex? index = ReadIndex(packFolder);
        ITuneRenderer? renderer = _renderers.Create(index?.Device);
        if (index == null || renderer == null) return null;

        string targetPath = Path.Combine(packFolder, CacheFolderName, CacheFileName(fileName, arrangement));
        if (IsUpToDate(targetPath, sourcePath, packFolder)) return targetPath;

        try
        {
            RenderedTune? rendered = renderer.Render(index, packFolder, fileName, arrangement);
            if (rendered == null) return null;

            _writer.Write(targetPath, _mixer.ToPcm16(rendered.Value.Samples, _renderers.Gain(index.Device)),
                rendered.Value.SampleRate);

            return targetPath;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Returns a tune's cached wave file when one is already up to date, without rendering anything.
    /// </summary>
    /// <remarks>
    /// This is the cheap path the game thread takes: it only looks at file names and timestamps, so
    /// it may be called while a sound is due.
    /// </remarks>
    /// <param name="packFolder">Folder of the sound pack.</param>
    /// <param name="fileName">File name of the tune inside that folder.</param>
    /// <param name="arrangement">Which arrangement is wanted.</param>
    /// <returns>Path of the wave file, or <c>null</c> when the tune still has to be rendered.</returns>
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This is part of the service's instance API, so a test can substitute the whole service.")]
    public string? TryGetCached(string packFolder, string fileName, int arrangement = 0)
    {
        string sourcePath = Path.Combine(packFolder, fileName);
        if (!File.Exists(sourcePath)) return null;

        string targetPath = Path.Combine(packFolder, CacheFolderName, CacheFileName(fileName, arrangement));
        return IsUpToDate(targetPath, sourcePath, packFolder) ? targetPath : null;
    }

    /// <summary>
    /// Builds the cache file name. The renderer version is part of it, so a changed renderer simply
    /// stops finding the old files instead of silently reusing them.
    /// </summary>
    private static string CacheFileName(string fileName, int arrangement)
    {
        string name = Path.GetFileNameWithoutExtension(fileName);
        if (name.EndsWith(".sound", StringComparison.OrdinalIgnoreCase)) name = name[..^".sound".Length];

        string suffix = arrangement > 0
            ? "-" + arrangement.ToString(CultureInfo.InvariantCulture)
            : string.Empty;

        return $"{name}{suffix}.v{RendererVersion.ToString(CultureInfo.InvariantCulture)}.wav";
    }

    /// <summary>
    /// A cached file counts as current when it is newer than the tune and than anything the pack
    /// shares, such as the AdLib instrument bank.
    /// </summary>
    private static bool IsUpToDate(string targetPath, string sourcePath, string packFolder)
    {
        if (!File.Exists(targetPath)) return false;

        DateTime rendered = File.GetLastWriteTimeUtc(targetPath);
        if (rendered < File.GetLastWriteTimeUtc(sourcePath)) return false;

        string indexPath = Path.Combine(packFolder, SoundPackIndex.FileName);
        return !File.Exists(indexPath) || rendered >= File.GetLastWriteTimeUtc(indexPath);
    }

    /// <summary>
    /// Reads the pack's manifest, which carries both the device and the clock rates the renderer
    /// needs. A pack written by an older build fails to load here and is simply not rendered.
    /// </summary>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "A corrupted or outdated index must not take the game's sound down; the pack is simply not rendered.")]
    private static SoundPackIndex? ReadIndex(string packFolder)
    {
        string indexPath = Path.Combine(packFolder, SoundPackIndex.FileName);
        if (!File.Exists(indexPath)) return null;

        try
        {
            return SoundPackIndexJson.Load(indexPath);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
