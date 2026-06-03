using System;
using CivOne.Persistence.Model;

namespace CivOne.Persistence.Factories
{
	/// <summary>
	/// Provides centralized access to sanitizer instances for runtime code paths that are not yet wired via dependency injection.
	/// </summary>
	/// <remarks>
	/// Why this exists:
	/// <list type="bullet">
	/// <item><description>Many legacy/static call sites cannot receive dependencies through constructors.</description></item>
	/// <item><description>Checked numeric conversion behavior must still be configurable for integration tests.</description></item>
	/// <item><description>A scoped override enables deterministic test behavior without changing broad production code paths.</description></item>
	/// </list>
	///
	/// Production usage example:
	/// <code>
	/// var checkedSanitizer = ValueSanitizerFactory.GetCheckedValueSanitizer();
	/// var value = checkedSanitizer.CheckedToByteOrClamp(raw, nameof(MyMapper), "MyField");
	/// </code>
	///
	/// Test usage example (temporary override):
	/// <code>
	/// using var scope = ValueSanitizerFactory.UseCheckedValueSanitizer(new UncheckedCastValueSanitizer());
	/// // execute code that should avoid clamping in this test scope
	/// </code>
	/// </remarks>
	public static class ValueSanitizerFactory
	{
		private static readonly Lazy<ValueSanitizer> _defaultInstance =
			new(() => new ValueSanitizer(new RuntimeLogger()));
		private static readonly ICheckedValueSanitizer _uncheckedInstance = new UncheckedCastValueSanitizer();
		private static readonly object _sync = new();
		private static ICheckedValueSanitizer? _runtimeCheckedValueSanitizer;
		private static ICheckedValueSanitizer? _scopedCheckedValueSanitizer;

		/// <summary>
		/// Returns the default value sanitizer instance used by persistence and YAML mapping code.
		/// </summary>
		/// <remarks>
		/// This always returns the default runtime sanitizer and is not affected by
		/// <see cref="UseCheckedValueSanitizer(ICheckedValueSanitizer)"/> overrides.
		/// </remarks>
		public static IValueSanitizer GetValueSanitizer() => _defaultInstance.Value;

		/// <summary>
		/// Returns the active checked sanitizer for cast-sensitive runtime paths.
		/// </summary>
		/// <remarks>
		/// If no override is active, the default runtime sanitizer is returned.
		/// If a test override is active, the overridden instance is returned until the scope is disposed.
		/// </remarks>
		public static ICheckedValueSanitizer GetCheckedValueSanitizer()
		{
			lock (_sync)
			{
				return _scopedCheckedValueSanitizer ?? _runtimeCheckedValueSanitizer ?? _defaultInstance.Value;
			}
		}

		/// <summary>
		/// Sets the runtime checked sanitizer mode used outside scoped test overrides.
		/// </summary>
		/// <param name="useUnchecked">
		/// If true, unchecked legacy cast behavior is used. If false, checked/clamping sanitizer is used.
		/// </param>
		public static void SetRuntimeUseUncheckedCastSanitizer(bool useUnchecked)
		{
			lock (_sync)
			{
				_runtimeCheckedValueSanitizer = useUnchecked ? _uncheckedInstance : null;
			}
		}

		/// <summary>
		/// Temporarily overrides the process-wide checked sanitizer until the returned scope is disposed.
		/// </summary>
		/// <param name="sanitizer">The checked sanitizer to activate for the override scope.</param>
		/// <returns>
		/// A disposable scope that restores the previous checked sanitizer when disposed.
		/// </returns>
		/// <remarks>
		/// Intended for tests where clamping must be disabled or behavior must be tightly controlled.
		/// Always dispose the returned scope to avoid leaking test configuration into other tests.
		/// </remarks>
		public static IDisposable UseCheckedValueSanitizer(ICheckedValueSanitizer sanitizer)
		{
			ArgumentNullException.ThrowIfNull(sanitizer);

			lock (_sync)
			{
				var previous = _scopedCheckedValueSanitizer;
				_scopedCheckedValueSanitizer = sanitizer;
				return new CheckedValueSanitizerScope(previous);
			}
		}

		private sealed class CheckedValueSanitizerScope(ICheckedValueSanitizer? previous) : IDisposable
		{
			private readonly ICheckedValueSanitizer? _previous = previous;
			private bool _disposed;

			public void Dispose()
			{
				if (_disposed)
				{
					return;
				}

				lock (_sync)
				{
					_scopedCheckedValueSanitizer = _previous;
				}

				_disposed = true;
			}
		}
	}
}
