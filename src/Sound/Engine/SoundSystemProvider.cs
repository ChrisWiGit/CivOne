using System.Diagnostics.CodeAnalysis;
using CivOne.Enums;

namespace CivOne.Sound.Engine;

/// <summary>
/// Hands out the sound system and keeps it in step with the sound setting.
/// </summary>
/// <remarks>
/// <para>
/// The same shape as <c>SoundPlaybackStrategyProvider</c>, and for the same reason: screens reach
/// sound through a provider rather than through a constructor. New code that can take
/// <see cref="ISoundSystem"/> as a dependency should do that instead of reading
/// <see cref="Current"/>.
/// </para>
/// <para>
/// The device is created on first use, not on registration. It can only be opened once the
/// platform's audio is up, which happens while the window is coming up.
/// </para>
/// </remarks>
internal static class SoundSystemProvider
{
	private static AudioDeviceFactoryDelegate? _deviceFactory;
	[SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance", Justification = "The field holds what Current hands out, and that is deliberately the interface: it is the only seam through which another implementation can be put in place.")]
	private static ISoundSystem? _current;
	private static bool _deviceTried;

	/// <summary>
	/// Gets whether a runtime has offered an audio output at all.
	/// </summary>
	public static bool IsAvailable => _deviceFactory != null;

	/// <summary>
	/// Records how to create the audio output.
	/// </summary>
	/// <param name="deviceFactory">Creates the device, or <c>null</c> to forget the previous one.</param>
	/// <remarks>
	/// Called by the runtime while it starts up. Nothing is opened here.
	/// </remarks>
	public static void RegisterDevice(AudioDeviceFactoryDelegate? deviceFactory)
	{
		Shutdown();

		_deviceFactory = deviceFactory;
		_deviceTried = false;
	}

	/// <summary>
	/// Gets the sound system, opening the audio output on first use.
	/// </summary>
	/// <remarks>
	/// <c>null</c> when no runtime offered an output, or when opening it failed. A caller that only
	/// wants to play something should use <see cref="Play"/>, which handles that and the sound
	/// setting as well.
	/// </remarks>
	public static ISoundSystem? Current
	{
		get
		{
			if (_current != null) return _current;
			if (_deviceTried || _deviceFactory == null) return null;

			_deviceTried = true;

			IAudioDevice? device = _deviceFactory();
			if (device == null) return null;

			var system = new SoundSystem(device);

			// A device that would not open is of no use to anyone: it is let go here rather than
			// handed out, so nothing queues audio that is never heard.
			if (!system.IsRunning)
			{
				system.Dispose();
				return null;
			}

			_current = system;
			return _current;
		}
	}

	/// <summary>
	/// Plays a sound, unless sound is switched off.
	/// </summary>
	/// <param name="request">What to play and how.</param>
	/// <returns>A handle, or <c>null</c> when nothing was started.</returns>
	public static ISoundHandle? Play(SoundRequest request)
	{
		if (Settings.Instance.Sound == GameOption.Off) return null;

		return Current?.Play(request);
	}

	/// <summary>
	/// Stops everything this system is playing.
	/// </summary>
	/// <remarks>
	/// Deliberately does not create the system when there is none: nothing can be playing then, so
	/// there is nothing to stop. Silencing sound must never be the call that opens a device.
	/// </remarks>
	public static void Abort()
	{
		if (_current == null) return;

		_current.StopAll(SoundBus.Music);
		_current.StopAll(SoundBus.Effect);
	}

	/// <summary>
	/// Raises the events of sounds that have ended.
	/// </summary>
	/// <remarks>Called once per frame from the game thread.</remarks>
	public static void Process() => _current?.Process();

	/// <summary>
	/// Closes the audio output.
	/// </summary>
	/// <remarks>
	/// The registered factory is kept, and the next use opens the device again: a failed attempt
	/// must not be remembered across a shutdown, or sound would stay gone for the rest of the
	/// process.
	/// </remarks>
	public static void Shutdown()
	{
		_current?.Dispose();
		_current = null;
		_deviceTried = false;
	}
}
