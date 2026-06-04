using System;
using CivOne.Persistence.Yaml;

namespace CivOne.Agents
{
	/// <summary>
	/// Generic <see cref="IAgentMemory"/> implementation that maps YAML text to a DTO.
	/// It uses delegates so each AI can decide how DTO state is captured and restored.
	/// </summary>
	/// <typeparam name="TDto">The DTO type used for memory serialization.</typeparam>
	public sealed class AgentMemoryDtoDelegate<TDto> : IAgentMemory
		where TDto : class
	{
		private readonly Func<TDto> _snapshotDelegate;
		private readonly Action<TDto> _restoreDelegate;
		private readonly Func<TDto> _createDefaultDelegate;
		private readonly bool _useDefaultOnDeserializationError;

		/// <summary>
		/// Creates a delegate-based memory bridge.
		/// </summary>
		/// <param name="snapshotDelegate">
		/// Returns a DTO snapshot of current AI memory state.
		/// </param>
		/// <param name="restoreDelegate">
		/// Restores AI memory state from a DTO.
		/// </param>
		/// <param name="createDefaultDelegate">
		/// Creates a default DTO used for empty YAML or optional fallback handling.
		/// </param>
		/// <param name="useDefaultOnDeserializationError">
		/// When <see langword="true"/>, invalid YAML falls back to default DTO.
		/// When <see langword="false"/>, deserialization exceptions are rethrown.
		/// </param>
		public AgentMemoryDtoDelegate(
			Func<TDto> snapshotDelegate,
			Action<TDto> restoreDelegate,
			Func<TDto> createDefaultDelegate,
			bool useDefaultOnDeserializationError = true)
		{
			_snapshotDelegate = snapshotDelegate ?? throw new ArgumentNullException(nameof(snapshotDelegate));
			_restoreDelegate = restoreDelegate ?? throw new ArgumentNullException(nameof(restoreDelegate));
			_createDefaultDelegate = createDefaultDelegate ?? throw new ArgumentNullException(nameof(createDefaultDelegate));
			_useDefaultOnDeserializationError = useDefaultOnDeserializationError;
		}

		/// <summary>
		/// Restores AI memory from YAML.
		/// </summary>
		/// <param name="yaml">Serialized YAML memory content.</param>
		public void SetMemory(string yaml)
		{
			if (string.IsNullOrWhiteSpace(yaml))
			{
				_restoreDelegate(_createDefaultDelegate());
				return;
			}

			try
			{
				TDto? dto = YamlReader.OfString(yaml)
					.WithStandard()
					.As<TDto>();

				_restoreDelegate(dto ?? _createDefaultDelegate());
			}
			catch when (_useDefaultOnDeserializationError)
			{
				_restoreDelegate(_createDefaultDelegate());
			}
		}

		/// <summary>
		/// Serializes current AI memory into YAML.
		/// </summary>
		/// <returns>Serialized YAML memory content.</returns>
		public string GetMemory()
		{
			TDto dto = _snapshotDelegate() ?? throw new InvalidOperationException("Snapshot delegate returned null DTO.");
			return YamlWriter.Of(dto)
				.WithStandard()
				.AsString();
		}
	}
}
