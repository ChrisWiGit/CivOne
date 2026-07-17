// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using CivOne.Agents;

namespace CivOne.Screens
{
	/// <summary>
	/// Describes one selectable AI definition.
	/// </summary>
	/// <param name="id">The unique AI identifier.</param>
	/// <param name="name">The display name.</param>
	/// <param name="provider">The AI provider name.</param>
	/// <param name="difficulty">The default AI difficulty.</param>
	internal sealed class AiCatalogEntry(Guid id, string name, string provider, AiDifficulty difficulty)
	{
		/// <summary>
		/// Gets the unique AI identifier.
		/// </summary>
		public Guid Id { get; } = id;

		/// <summary>
		/// Gets the AI display name.
		/// </summary>
		public string Name { get; } = name ?? string.Empty;

		/// <summary>
		/// Gets the AI provider name.
		/// </summary>
		public string Provider { get; } = provider ?? string.Empty;

		/// <summary>
		/// Gets the default AI difficulty.
		/// </summary>
		public AiDifficulty Difficulty { get; } = difficulty;
	}
}