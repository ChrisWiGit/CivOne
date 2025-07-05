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

namespace CivOne
{
	public class RuntimeSettings
	{
		private readonly Dictionary<string, object> _customSettings = new Dictionary<string, object>();

		private bool _free;

		public bool Demo { get; set; }
		public bool Setup { get; set; }
		public bool DataCheck { get; set; }

		public Tuple<char, int> LoadSaveGameSlot { get; set; } = null;
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

		public object this[string customSetting]
		{
			get
			{
				if (_customSettings.ContainsKey(customSetting.ToLower()))
					return _customSettings[customSetting.ToLower()];
				return null;
			}
			set
			{
				if (_customSettings.ContainsKey(customSetting.ToLower()))
				{
					_customSettings[customSetting.ToLower()] = value;
					return;
				}
				_customSettings.Add(customSetting.ToLower(), value);
			}
		}

		public T Get<T>(string customSetting)
		{
            if (this[customSetting] == null)
                return default(T);

			try
			{
				 return (T)this[customSetting];
			}
			catch
			{
				return default(T);
			}
		}

		public RuntimeSettings()
		{
			DataCheck = true;
			ShowCredits = true;
			ShowIntro = true;
			Free = false;
		}
	}
}