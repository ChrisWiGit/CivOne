using System.Diagnostics.CodeAnalysis;
using CivOne.Persistence.Model;

namespace CivOne.Mcp.Tools
{
	public interface IGameStateDtoSnapshotProvider
	{
		/// <summary>
		/// Attempts to get a snapshot of the current game state as a GameStateDto. 
		/// Returns false if no snapshot is available, along with error details. 
		/// The snapshot may be cached and should be treated as immutable. 
		/// Consumers should not modify the returned GameStateDto or its contents. 
		/// If the method returns true, the snapshot output will be non-null. 
		/// If it returns false, the snapshot output will be null and errorCode/errorMessage 
		/// will provide details on why the snapshot could not be retrieved (e.g. no active game session).
		/// </summary>
		/// <param name="snapshot">The snapshot of the current game state, or null if unavailable.</param>
		/// <param name="errorCode">The error code if the snapshot could not be retrieved, or null if successful.</param>
		/// <param name="errorMessage">The error message if the snapshot could not be retrieved, or null if successful.</param>
		/// <returns>True if the snapshot was successfully retrieved; otherwise, false.</returns>
		bool TryGetSnapshot( [NotNullWhen(true)] out GameStateDto? snapshot, [NotNullWhen(false)] out string? errorCode, [NotNullWhen(false)] out string? errorMessage);
	}
}
