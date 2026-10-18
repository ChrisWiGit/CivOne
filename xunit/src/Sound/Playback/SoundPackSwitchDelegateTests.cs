using System.Collections.Generic;
using CivOne.Enums;
using CivOne.Sound.Playback;
using Xunit;

namespace CivOne.UnitTests.Sound.Playback
{
	public class SoundPackSwitchDelegateTests
	{
		private readonly List<string> _calls = [];
		private string? _writtenPack;
		private bool? _gameSound;
		private GameOption _sound = GameOption.On;

		private SoundPackSwitchDelegate CreateSwitch() => new(
			writeSoundPack: pack =>
			{
				_writtenPack = pack;
				_sound = pack == SoundPlaybackStrategyConstants.NoSoundPack ? GameOption.Off : GameOption.On;
				_calls.Add("write");
			},
			readSound: () => _sound,
			setGameSound: on => _gameSound = on,
			abort: () => _calls.Add("abort"),
			warmUp: () => _calls.Add("warmUp"),
			notify: name => _calls.Add("notify:" + name));

		[Fact]
		public void SwitchWritesPackSwitchesSoundOnAndWarmsUp()
		{
			CreateSwitch().Switch("adlib", () => "AdLib");

			Assert.Equal("adlib", _writtenPack);
			Assert.True(_gameSound);
			Assert.Equal(["write", "warmUp", "notify:AdLib"], _calls);
		}

		[Fact]
		public void SwitchToNoneAbortsAndSwitchesGameSoundOff()
		{
			CreateSwitch().Switch(SoundPlaybackStrategyConstants.NoSoundPack, () => "None");

			Assert.False(_gameSound);
			Assert.Equal(["write", "abort", "warmUp", "notify:None"], _calls);
		}

		[Fact]
		public void SwitchWithNullPackWritesEmptySetting()
		{
			CreateSwitch().Switch(null, () => string.Empty);

			Assert.Equal(string.Empty, _writtenPack);
		}

		[Fact]
		public void PackNameIsReadAfterTheSwitch()
		{
			string? nameAtNotify = null;
			SoundPackSwitchDelegate soundPackSwitch = new(
				writeSoundPack: pack => _writtenPack = pack,
				readSound: () => GameOption.On,
				setGameSound: _ => { },
				abort: () => { },
				warmUp: () => { },
				notify: name => nameAtNotify = name);

			soundPackSwitch.Switch("pc-speaker", () => _writtenPack!);

			Assert.Equal("pc-speaker", nameAtNotify);
		}
	}
}
