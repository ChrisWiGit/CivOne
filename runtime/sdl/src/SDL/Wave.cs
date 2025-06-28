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
			private SDL_AudioSpec _waveSpec;
			private uint _length;
			private IntPtr _buffer;

			private void Log(string message) => OnLog(message);

			public event Action<string> OnLog;

			public string Filename { get; }
			public bool Playing { get; private set; }

			public void Play()
			{
				if (Playing) return;

				Playing = true;

				Log($"Sound start: {Path.GetFileName(Filename)}");

				if (SDL_LoadWAV_RW(Filename, 1, ref _waveSpec, out _buffer, out _length) == IntPtr.Zero)
				{
					Log($"Could not load sound: {Path.GetFileName(Filename)}: {GetSdlErrorMessage()}");
					return;
				}

				var deviceId = SDL_OpenAudioDevice(null, 0, ref _waveSpec, out _, 0);
				if (deviceId == 0 && SDL_GetError() != 0)
				{
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

			public void Dispose()
			{
				Log($"Sound stop: {Path.GetFileName(Filename)}");

				SDL_PauseAudio(1);
				SDL_CloseAudio();
				SDL_FreeWAV(_buffer);
			}
		}
	}
}