using CivOne.Sound.Cvl;

namespace CivOne.Sound.Playback;



/// <summary>
/// Audio produced by a renderer, before it is written to a file.
/// </summary>
/// <param name="Samples">Mono samples. One operator or one square wave at full level reaches 1.</param>
/// <param name="SampleRate">Rate the samples were produced at, in Hz.</param>
internal readonly record struct RenderedTune(float[] Samples, int SampleRate);

/// <summary>
/// Turns one tune of a sound pack into audio.
/// </summary>
/// <remarks>
/// There is one implementation per sound device. The pack's <c>index.json</c> says which device it
/// holds, and <see cref="TuneRendererFactory"/> picks the matching renderer from that.
/// </remarks>
internal interface ITuneRenderer
{
    /// <summary>Gets the device this renderer serves, matching the pack's <c>device</c> field.</summary>
    string Device { get; }

    /// <summary>
    /// Renders one tune.
    /// </summary>
    /// <param name="index">
    /// The pack's manifest. It carries the identity of the pack and the clock rates the driver ran
    /// at, which the tune files themselves no longer repeat.
    /// </param>
    /// <param name="packFolder">Folder of the sound pack, which may hold files shared by all tunes.</param>
    /// <param name="scoreFileName">File name of the tune inside that folder.</param>
    /// <param name="arrangement">Which arrangement to render; ignored by packs that have only one.</param>
    /// <returns>The audio, or <c>null</c> when the tune has nothing to play.</returns>
    RenderedTune? Render(SoundPackIndex index, string packFolder, string scoreFileName, int arrangement);
}
