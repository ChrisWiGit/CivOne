// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

namespace CivOne.Services
{
	/// <summary>
	/// Abstracts <see cref="System.IO.Directory"/> for dependency injection scenarios.
	/// </summary>
	public interface IDirectoryService
	{
		/// <summary>
		/// Creates all directories and subdirectories in <paramref name="path"/> unless they already exist.
		/// </summary>
		/// <param name="path">The directory path to create.</param>
		void CreateDirectory(string path);
	}
}
