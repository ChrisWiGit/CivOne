using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CivOne.Persistence
{
    public class YamlSaveWriter : IGameStateWriter
    {
        public void Write(Stream stream, GameState snapshot)
        {
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var yaml = serializer.Serialize(snapshot);
            StreamWriter writer = new(stream);
            writer.Write(yaml);
        }
    }
}