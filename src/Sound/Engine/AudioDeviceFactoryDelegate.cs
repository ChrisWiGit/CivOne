namespace CivOne.Sound.Engine;

/// <summary>
/// Creates the audio output the sound system plays through.
/// </summary>
/// <returns>The device, or <c>null</c> when this platform has no audio output.</returns>
/// <remarks>
/// The runtime supplies this rather than a finished device, because the device can only be opened
/// once the platform's audio has been initialised - which happens while the window is coming up,
/// long after the runtime itself is registered.
/// </remarks>
internal delegate IAudioDevice? AudioDeviceFactoryDelegate();
