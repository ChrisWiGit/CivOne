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
				// it is a best practice to make a copy of the current sound
				// before disposing it, as HandleSound() may be called in another thread
				// in the future.
				Wave sound = _currentSound;
				_currentSound = null;
				sound.Dispose();
			}
		}
	}
}