using System;
using System.Collections.Generic;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace CivOne.Persistence.Yaml
{
	/// <summary>
	/// Alternative YAML converter for byte[][] that produces true nested arrays in YAML.
	/// Instead of converting to strings like "[0, 10, 20]", this produces real YAML arrays.
	/// 
	/// Output example:
	/// LandValues:
	///   - [0, 10, 20, 30, 40]
	///   - [10, 20, 30, 40, 50]
	/// 
	/// This works by converting byte[][] to a List<List<byte>> which YamlDotNet
	/// serializes with the correct flow style for inner arrays.
	/// </summary>
	public class ByteArrayArrayFlowStyleYamlTypeConverter : IYamlTypeConverter
	{
		public bool Accepts(Type type) => type == typeof(byte[][]);

		public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
		{
			// Deserialize as List<List<byte>> to avoid infinite recursion, then convert to byte[][]
			var listOfLists = (List<List<byte>>)rootDeserializer(typeof(List<List<byte>>));

			if (listOfLists == null)
				return null;

			var result = new byte[listOfLists.Count][];
			for (int i = 0; i < listOfLists.Count; i++)
			{
				if (listOfLists[i] != null)
				{
					result[i] = new byte[listOfLists[i].Count];
					for (int j = 0; j < listOfLists[i].Count; j++)
					{
						result[i][j] = listOfLists[i][j];
					}
				}
			}

			return result;
		}
		public void WriteYaml(IEmitter emitter, object value, Type type, ObjectSerializer serializer)
		{
			var data = (byte[][])value;

			// Outer array (block style)
			emitter.Emit(new SequenceStart(null, null, false, SequenceStyle.Block));

			foreach (var row in data)
			{
				// Inner arrays (flow style → [1, 2, 3])
				emitter.Emit(new SequenceStart(null, null, false, SequenceStyle.Flow));

				foreach (var b in row)
				{
					emitter.Emit(new Scalar(b.ToString()));
				}

				emitter.Emit(new SequenceEnd());
			}

			emitter.Emit(new SequenceEnd());
		}
	}
}
