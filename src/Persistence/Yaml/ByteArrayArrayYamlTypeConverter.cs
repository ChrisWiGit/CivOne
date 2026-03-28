using System;
using System.Text;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace CivOne.Persistence.Yaml
{
    /// <summary>
    /// Custom converter for byte[][] that serializes each row as a compact line.
    /// Converts [[0, 10, 20], [30, 40]] to:
    /// - [0, 10, 20]
    /// - [30, 40]
    /// </summary>
    public class ByteArrayArrayYamlTypeConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type) => type == typeof(byte[][]);

        public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
        {
            var rows = (byte[][])rootDeserializer(typeof(byte[][]));
            return rows;
        }

        public void WriteYaml(IEmitter emitter, object value, Type type, ObjectSerializer serializer)
        {
            var byteArrayArray = (byte[][])value;
            var rows = new string[byteArrayArray.Length];

            // Convert each row to a compact string format
            for (int i = 0; i < byteArrayArray.Length; i++)
            {
                rows[i] = FormatByteArray(byteArrayArray[i]);
            }

            // Serialize as string array
            serializer(rows, typeof(string[]));
        }

        private string FormatByteArray(byte[] bytes)
        {
            var sb = new StringBuilder("[");
            for (int i = 0; i < bytes.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(bytes[i]);
            }
            sb.Append("]");
            return sb.ToString();
        }
    }
}
