using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using CivOne.Sound.Engine;

namespace CivOne
{
	#pragma warning disable S101 // Types should be named in PascalCase - but these are named to match SDL as a name.
	internal static partial class SDL
	{
		/// <summary>
		/// The one audio output the sound system plays through, opened in pull mode.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This is the only class that knows SDL exists. Everything above it works against
		/// <see cref="IAudioDevice"/>, which is what lets the mixer be tested without a sound card.
		/// </para>
		/// <para>
		/// It is nested in <see cref="SDL"/> only to reach the audio imports, which are private to
		/// that class. It is otherwise unrelated to <see cref="Window"/> and shares no state with it,
		/// so the old one-file-at-a-time path keeps working untouched alongside this one.
		/// </para>
		/// </remarks>
		/// <param name="sampleRate">Rate to ask the device for, in Hz.</param>
		/// <param name="bufferSamples">
		/// How many samples the device asks for at a time. 1024 samples at 44100 Hz is about 23 ms,
		/// which is far below anything a turn-based game needs and leaves ample room against dropouts.
		/// </param>
		internal sealed class AudioDevice(int sampleRate = 44100, ushort bufferSamples = 1024) : IAudioDevice
		{
			/// <summary>
			/// Little-endian 32-bit float samples.
			/// </summary>
			/// <remarks>
			/// Spelled out rather than taken from <c>SDL_AudioFormat</c>: that enum carries plain
			/// sequential values instead of SDL's own bit-packed ones, so it cannot be used here.
			/// </remarks>
			private const ushort AudioF32Lsb = 0x8120;

			private const int BytesPerSample = 4;
			private const int Channels = 1;

			private readonly object _lock = new();

			private uint _deviceId;
			private SDL_AudioCallback? _callback;
			private AudioRenderCallback? _render;
			private float[] _buffer = [];
			private bool _disposed;

			/// <summary>Raised with anything worth writing to the log.</summary>
			public event Action<string>? OnLog;

			/// <inheritdoc />
			public int SampleRate { get; private set; } = sampleRate;

			/// <inheritdoc />
			public void Start(AudioRenderCallback render)
			{
				ArgumentNullException.ThrowIfNull(render);

				lock (_lock)
				{
					if (_disposed || _deviceId != 0) return;

					_render = render;

					// The delegate has to be held in a field. SDL keeps only the raw function
					// pointer, so a delegate that exists nowhere else is collected and the next
					// callback tears the process down.
					_callback = OnAudioRequested;

					var desired = new SDL_AudioSpec
					{
						Frequency = SampleRate,
						Format = AudioF32Lsb,
						Channels = Channels,
						Samples = bufferSamples,
						Callback = _callback
					};

					_deviceId = SDL_OpenAudioDevice(IntPtr.Zero, 0, ref desired, out SDL_AudioSpec obtained, 0);
					if (_deviceId == 0)
					{
						_callback = null;
						_render = null;
						OnLog?.Invoke($"Could not open the audio device: {GetSdlErrorMessage()}");
						return;
					}

					SampleRate = obtained.Frequency > 0 ? obtained.Frequency : SampleRate;
					_buffer = new float[Math.Max(obtained.Samples, bufferSamples) * Channels];

					SDL_PauseAudioDevice(_deviceId, 0);
				}
			}

			/// <inheritdoc />
			public void Stop()
			{
				lock (_lock)
				{
					if (_deviceId == 0) return;

					// Closing the device waits for a callback that is currently running and
					// guarantees it will not be entered again, so the sample buffers the mixer reads
					// from are safe to release afterwards.
					SDL_PauseAudioDevice(_deviceId, 1);
					SDL_CloseAudioDevice(_deviceId);

					_deviceId = 0;
					_callback = null;
					_render = null;
				}
			}

			/// <inheritdoc />
			public void Dispose()
			{
				Stop();

				lock (_lock)
				{
					_disposed = true;
					OnLog = null;
				}
			}

			/// <summary>
			/// Fills the buffer SDL handed us.
			/// </summary>
			/// <remarks>
			/// Runs on SDL's own audio thread. It must not block, must not allocate on the normal
			/// path and must not let an exception reach native code, which would end the process.
			/// </remarks>
			/// <summary>
			/// Writes silence into the buffer SDL handed us, without allocating anything.
			/// </summary>
			/// <param name="stream">Buffer SDL handed us.</param>
			/// <param name="count">How many samples it holds.</param>
			private static void WriteSilence(IntPtr stream, int count)
			{
				for (int index = 0; index < count; index++)
				{
					Marshal.WriteInt32(stream, index * BytesPerSample, 0);
				}
			}

			[SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "This method is called from native code. Any exception that escaped it would end the process, so every one of them has to be swallowed here.")]
			private void OnAudioRequested(IntPtr userData, IntPtr stream, int length)
			{
				int count = length / BytesPerSample;
				if (count <= 0) return;

				try
				{
					// Only ever on a buffer size change, never per callback.
					if (_buffer.Length < count) _buffer = new float[count];

					AudioRenderCallback? render = _render;
					if (render == null)
					{
						Array.Clear(_buffer, 0, count);
					}
					else
					{
						render(_buffer.AsSpan(0, count));
					}

					Marshal.Copy(_buffer, 0, stream, count);
				}
				catch (Exception)
				{
					// Silence is the only safe answer here: there is no caller to report to, and
					// letting this travel into SDL would take the process down. Written straight into
					// the stream, so the whole buffer is covered even when the managed one is too
					// small or allocating it has just failed - a partly filled buffer would be heard
					// as a click rather than as silence.
					WriteSilence(stream, count);
				}
			}
		}
	}
}
