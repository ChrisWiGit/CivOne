using System;
using System.Collections.Generic;
using CivOne.Sound.Playback.Adlib;

namespace CivOne.Sound.Playback;

#nullable enable

/// <summary>
/// Picks the renderer that matches a sound pack's device.
/// </summary>
/// <remarks>
/// Renderers keep loaded data such as an instrument bank, so the factory hands out one shared
/// instance per device rather than creating a new one per tune.
/// </remarks>
internal sealed class TuneRendererFactory
{
    private readonly Dictionary<string, ITuneRenderer> _renderers;
    private readonly Dictionary<string, float> _gains;

    /// <summary>
    /// Creates a factory over the built-in renderers.
    /// </summary>
    public TuneRendererFactory()
    {
        _renderers = new Dictionary<string, ITuneRenderer>(StringComparer.OrdinalIgnoreCase);
        _gains = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        Add(new PcSpeakerTuneRenderer(), PcSpeakerTuneRenderer.Gain);
        Add(new AdlibTuneRenderer(), AdlibTuneRenderer.Gain);
    }

    private void Add(ITuneRenderer renderer, float gain)
    {
        _renderers[renderer.Device] = renderer;
        _gains[renderer.Device] = gain;
    }

    /// <summary>
    /// Gets the renderer for a device.
    /// </summary>
    /// <param name="device">Device name from the pack's index, e.g. <c>"adlib"</c>.</param>
    /// <returns>The renderer, or <c>null</c> when no renderer serves that device.</returns>
    public ITuneRenderer? Create(string? device)
        => device != null && _renderers.TryGetValue(device, out ITuneRenderer? renderer) ? renderer : null;

    /// <summary>
    /// Gets the level a device's audio is mixed at.
    /// </summary>
    /// <param name="device">Device name from the pack's index.</param>
    /// <returns>The gain, or <c>1</c> for an unknown device.</returns>
    public float Gain(string? device)
        => device != null && _gains.TryGetValue(device, out float gain) ? gain : 1f;
}
