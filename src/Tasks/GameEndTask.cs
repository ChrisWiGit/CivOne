using System;
using System.Threading.Tasks;

namespace CivOne.Tasks
{
	/// <summary>
	/// Runs the end-game flow as a game task, so the next turn's tasks wait until the player decided to keep playing.
	/// </summary>
	/// <remarks>
	/// The task finishes only when the flow returns <c>true</c> ("keep playing"). When the game ends, the flow clears
	/// all tasks itself and shows the credits.
	/// </remarks>
	/// <param name="flow">The end-game flow; returns whether the player keeps playing.</param>
	internal sealed class GameEndTask(Func<Task<bool>> flow) : GameTask
	{
		private readonly Func<Task<bool>> _flow = flow ?? throw new ArgumentNullException(nameof(flow));

		public override void Run() => _ = RunFlowAsync();

		private async Task RunFlowAsync()
		{
			if (await _flow().ConfigureAwait(continueOnCapturedContext: true))
			{
				EndTask();
			}
		}
	}
}
