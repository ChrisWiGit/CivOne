using CivOne.Persistence.Model;

namespace CivOne.Mcp.Tools
{
	public interface IGameStateDtoSnapshotProvider
	{
		bool TryGetSnapshot(out GameStateDto snapshot, out string errorCode, out string errorMessage);
	}
}
