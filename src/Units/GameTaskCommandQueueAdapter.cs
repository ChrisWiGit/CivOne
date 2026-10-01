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