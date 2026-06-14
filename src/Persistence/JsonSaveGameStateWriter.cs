using System;
using System.Text.Json;

namespace CivOne.Persistence
{
	/// <summary>
	/// Serializes game-state DTO projections to JSON for MCP responses.
	/// This class is format-focused and has no YAML dependency.
	/// </summary>
	public sealed class JsonSaveGameStateWriter
	{
		private readonly JsonSerializerOptions _jsonOptions;

		public string AsString(object value)
		{
			ArgumentNullException.ThrowIfNull(value);
			return JsonSerializer.Serialize(value, _jsonOptions);
		}

		public JsonSaveGameStateWriter(JsonSerializerOptions? jsonOptions = null)
		{
			_jsonOptions = jsonOptions ?? new JsonSerializerOptions
			{
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase
			};
		}
	}
}
