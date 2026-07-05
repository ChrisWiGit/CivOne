using System.IO;

namespace CivOne.Persistence
{
    public interface IGameStateWriter
    {
        void Write(Stream stream, GameState snapshot);
    }
}