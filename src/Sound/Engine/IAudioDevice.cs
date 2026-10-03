using System;

namespace CivOne.Sound.Engine;

/// <summary>
/// Fills a buffer with the next samples, each in the range <c>-1</c> to <c>1</c>.
/// </summary>
/// <param name="buffer">Buffer to fill completely. Anything left unwritten is heard as a gap.</param>
internal delegate void AudioRenderCallback(Span<float> buffer);

/// <summary>
/// An audio output that pulls samples from whoever fills its buffers.
/// </summary>
/// <remarks>
/// Pull, not push.
/// The device asks for samples when it needs them, which is what makes fades, cross fades and
/// pausing possible at all: a pushed buffer is out of reach once it has been handed over.
/// <para>
/// Keeping this behind an interface is what lets the mixer be tested without a sound card.
/// </para>
/// </remarks>
internal interface IAudioDevice : IDisposable
{
	/// <summary>Gets the rate the callback is served at, in Hz.</summary>
	int SampleRate { get; }

	/// <summary>
	/// Opens the device and begins asking <paramref name="render"/> for samples.
	/// </summary>
	/// <param name="render">Callback that produces the samples.</param>
	/// <returns><c>true</c> when the output is open and the callback is being served.</returns>
	/// <remarks>
	/// The callback runs on the device's own thread, not the game thread.
	/// It must not block, must not allocate and must not let an exception escape.
	/// <para>
	/// A platform that cannot open its output says so here rather than throwing: the caller has to
	/// know, because nothing it queues afterwards would ever be heard.
	/// </para>
	/// </remarks>
	bool Start(AudioRenderCallback render);

	/// <summary>
	/// Stops asking for samples.
	/// </summary>
	/// <remarks>
	/// Returns only once the callback is guaranteed not to run again, so buffers it reads from may
	/// be released afterwards.
	/// </remarks>
	void Stop();
}
