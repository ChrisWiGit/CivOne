using System.IO;

namespace CivOne.Persistence
{
    public interface IGameStateWriter
    {
        void Write(Stream file, GameState snapshot);
    }
}