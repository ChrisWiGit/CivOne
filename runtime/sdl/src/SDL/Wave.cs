// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace CivOne
{
	internal static partial class SDL
	{
		internal unsafe class Wave : IDisposable
		{
			private uint deviceId = uint.MaxValue;

			private SDL_AudioSpec _waveSpec;
			private uint _length;
			private IntPtr _buffer = IntPtr.Zero;

			private void Log(string message) => OnLog?.Invoke(message);

			public event Action<string> OnLog;

			public string Filename { get; }
			public bool Playing { get; private set; }

			public void Play()
			{
				if (Playing)
				{
					throw new InvalidOperationException("This Wave class was already used to play a sound, and cannot be reused.");
				}

				Playing = true;

				Log($"Sound start: {Path.GetFileName(Filename)}");

				const int FREE_SOURCE = 1;
				if (SDL_LoadWAV_RW(Filename, FREE_SOURCE, ref _waveSpec, out _buffer, out _length) == IntPtr.Zero)
				{
					_buffer = IntPtr.Zero;
					Log($"Could not load sound: {Path.GetFileName(Filename)}: {GetSdlErrorMessage()}");
					return;
				}

				deviceId = SDL_OpenAudioDevice(null, 0, ref _waveSpec, out _, 0);
				if (deviceId == 0 && SDL_GetError() != 0)
				{
					deviceId = uint.MaxValue;
					Log($"Could not open audio device {GetSdlErrorMessage()} error: {SDL_GetError()}");
					return;
				}

				if (SDL_QueueAudio(deviceId, _buffer, _length) < 0)
				{
					Log("Could not queue audio");
					return;
				}

				SDL_PauseAudioDevice(deviceId, 0);
			}

			public Wave(string filename)
			{
				Filename = filename;
			}

			// Safety net: if a Wave reference is lost (e.g. on a Window crash) without explicit Dispose,
			// the finalizer still releases the SDL audio device + WAV buffer. Without this, SDL can
			// exhaust its limited number of simultaneous audio devices over the program lifetime.
			~Wave() => Dispose();

			public bool IsPlaying()
			{
				return deviceId != uint.MaxValue && SDL_GetQueuedAudioSize(deviceId) > 0;
			}

			public void Dispose()
			{
				// Idempotent: avoid double Log/double-free when called twice.
				if (deviceId == uint.MaxValue && _buffer == IntPtr.Zero) return;

				Log($"Sound stop: {Path.GetFileName(Filename)}");

				if (deviceId != uint.MaxValue)
				{
					SDL_PauseAudioDevice(deviceId, 1);
					SDL_CloseAudioDevice(deviceId);
					deviceId = uint.MaxValue;
				}

				if (_buffer != IntPtr.Zero)
				{
					SDL_FreeWAV(_buffer);
					_buffer = IntPtr.Zero;
				}

				GC.SuppressFinalize(this);
			}
		}
	}
}