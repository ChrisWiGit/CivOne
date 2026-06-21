// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace CivOne
{
	public class RuntimeSettings
	{
		private readonly Dictionary<string, object?> _customSettings = [];

		private bool _free;

		public bool Demo { get; set; }
		public bool Setup { get; set; }
		public bool DataCheck { get; set; }
		public bool McpEnabled { get; set; }
		public bool McpNoAuth { get; set; }
		public bool ConsoleLogging { get; set; }

		public Tuple<char, int>? LoadSaveGameSlot { get; set; }
		public string? LoadCosFile { get; set; }
		public string? LanguagePostfix { get; set; }
		public static Tuple<char, int> UseLoadingScreen => new Tuple<char, int>('0', -1);

        // fire-eggs 20190711 allow specifying the initial RNG seed for game repeatability/debugging
		public ushort InitialSeed { get; set; }

		public bool Free
		{
			get
			{
				return _free;
			}
			set
			{
				if (_free = value)
				{
					DataCheck = false;
					ShowCredits = false;
					ShowIntro = false;
				}
			}
		}
		public bool ShowCredits { get; set; }
		public bool ShowIntro { get; set; }

		public object? this[string customSetting]
		{
			get
			{
				ArgumentNullException.ThrowIfNull(customSetting);

				if (_customSettings.ContainsKey(customSetting.ToUpperInvariant()))
					return _customSettings[customSetting.ToUpperInvariant()];
				return null;
			}
			set
			{
				ArgumentNullException.ThrowIfNull(customSetting);

				if (_customSettings.ContainsKey(customSetting.ToUpperInvariant()))
				{
					_customSettings[customSetting.ToUpperInvariant()] = value;
					return;
				}
				_customSettings.Add(customSetting.ToUpperInvariant(), value);
			}
		}

		[SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Catching all exceptions is necessary to ensure that failure to retrieve a custom setting does not crash the application, and that any exceptions are logged appropriately.")]
		public T? Get<T>(string customSetting)
		{
            if (this[customSetting] == null)
                return default;

			try
			{
				return (T?)this[customSetting];
			}
			catch
			{
				return default;
			}
		}

		public RuntimeSettings()
		{
			DataCheck = true;
			ShowCredits = true;
			ShowIntro = true;
			Free = false;
			McpEnabled = false;
			McpNoAuth = false;
			ConsoleLogging = true;
		}
	}
}