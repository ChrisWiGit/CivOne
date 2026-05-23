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
	/// Observer for translation language changes.
	/// </summary>
	public interface ITranslationLanguageObserver
	{
		/// <summary>
		/// Called whenever the active language changes.
		/// </summary>
		/// <param name="activeLanguagePostfix">
		/// The active language postfix, or <see langword="null"/> when identity translation is active.
		/// </param>
		void OnLanguageChanged(string activeLanguagePostfix);
	}
}
