using CivOne.Tasks;

namespace CivOne.Units;

/// <summary>
/// Game task command queue.
/// Abstracts GameTask static queue for dependency injection.
/// CQRS-style: supports Enqueue (append to end) and Insert (prepend for immediate execution).
/// 
/// This interface is provided for future dependency injection refactoring.
/// Implement this interface to provide alternative task queuing mechanisms.
/// </summary>
public interface IGameTaskCommandQueue
{
    /// <summary>
    /// Enqueue a task to the end of the queue.
    /// </summary>
    void Enqueue(GameTask task);

    /// <summary>
    /// Insert a task to the front of the queue (execute immediately after current task).
    /// </summary>
    void Insert(GameTask task);
}
