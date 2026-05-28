// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

namespace CivOne
{
	#pragma warning disable S101 // Types should be named in PascalCase - but these are named to match SDL as a name.
	internal static partial class SDL
	{
		internal abstract partial class Window
		{
			private Wave _currentSound = null;

			private void HandleSound()
			{
				if (_currentSound == null || _currentSound.IsPlaying()) return;

				StopSound();
			}

			protected void PlaySound(string filename)
			{
				if (_currentSound != null) StopSound();
				_currentSound = new Wave(filename);
				_currentSound.OnLog += Log;
				_currentSound.Play();
			}

			protected void StopSound()
			{
				// Atomic swap: protects against concurrent StopSound/PlaySound and against
				// callers invoking StopSound when no sound is active (NullReferenceException).
				Wave sound = System.Threading.Interlocked.Exchange(ref _currentSound, null);
				sound?.Dispose();
			}
		}
	}
}