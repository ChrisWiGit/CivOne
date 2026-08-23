// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using CivOne.Tasks;

namespace CivOne.Units;

/// <summary>
/// Default adapter implementation of <see cref="IGameTaskCommandQueue"/>.
/// Delegates to the static GameTask queue for backward compatibility.
/// 
/// This adapter is designed for dependency injection scenarios where task queue behavior
/// needs to be abstracted. Future refactoring can inject alternative implementations
/// to provide different queuing strategies or test-friendly mocking.
/// </summary>
internal sealed class GameTaskCommandQueueAdapter : IGameTaskCommandQueue
{
	/// <inheritdoc/>
	public void Enqueue(GameTask task) => GameTask.Enqueue(task);

	/// <inheritdoc/>
	public void Insert(GameTask task) => GameTask.Insert(task);
}
