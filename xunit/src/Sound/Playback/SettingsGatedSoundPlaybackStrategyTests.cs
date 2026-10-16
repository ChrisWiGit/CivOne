using System;
using System.Collections.Generic;
using CivOne.Enums;
using CivOne.Sound.Playback;
using Xunit;

namespace CivOne.UnitTests.Sound.Playback
{
	public class SettingsGatedSoundPlaybackStrategyTests
	{
		private readonly RecordingStrategy _inner = new();

		[Fact]
		public void PlaysWhenSoundIsOnInGameAndSetup()
		{
			Assert.True(CreateGate(gameSound: true, setupSound: GameOption.On).PlaySound("tune"));

			Assert.Equal(["tune"], _inner.Calls);
		}

		[Fact]
		public void PlaysWhenSetupUsesTheDefault()
		{
			CreateGate(gameSound: true, setupSound: GameOption.Default).PlaySound("tune");

			Assert.Equal(["tune"], _inner.Calls);
		}

		[Theory]
		[InlineData(false, GameOption.On)]
		[InlineData(true, GameOption.Off)]
		public void StaysSilentWhenSoundIsOff(bool gameSound, GameOption setupSound)
		{
			Assert.True(CreateGate(gameSound, setupSound).PlaySound("tune"));

			Assert.Empty(_inner.Calls);
		}

		[Fact]
		public void StaysSilentForAnEmptyName()
		{
			CreateGate(gameSound: true, setupSound: GameOption.On).PlaySound("");

			Assert.Empty(_inner.Calls);
		}

		[Fact]
		public void StopsAndReadsDurationsEvenWhenSoundIsOff()
		{
			_inner.Durations["tune"] = TimeSpan.FromSeconds(3);
			SettingsGatedSoundPlaybackStrategy gate = CreateGate(gameSound: false, setupSound: GameOption.Off);

			gate.Abort();
			bool known = gate.TryGetDuration("tune", out TimeSpan duration);

			Assert.Equal(["stop"], _inner.Calls);
			Assert.True(known);
			Assert.Equal(TimeSpan.FromSeconds(3), duration);
		}

		private SettingsGatedSoundPlaybackStrategy CreateGate(bool gameSound, GameOption setupSound)
			=> new(_inner, new FakeGameSound(gameSound), new FakeSettings(setupSound));

		private sealed class FakeGameSound(bool sound) : IGameSoundOption
		{
			public bool Sound { get; } = sound;
		}

		private sealed class FakeSettings(GameOption sound) : ISettings
		{
			public GameOption Sound { get; } = sound;
			public string MapsDirectory => throw new NotImplementedException();
			public string PicturesDirectory => throw new NotImplementedException();
			public string SavesDirectory => throw new NotImplementedException();
			public string CosSavesDirectory => throw new NotImplementedException();
			public string StorageDirectory => throw new NotImplementedException();
			public string CaptureDirectory => throw new NotImplementedException();
			public string DataDirectory => throw new NotImplementedException();
			public string PluginsDirectory => throw new NotImplementedException();
			public string SoundsDirectory => throw new NotImplementedException();
			public bool RevealWorld => throw new NotImplementedException();
			public bool DebugMenu => throw new NotImplementedException();
			public bool AutoSaveOnQuit => throw new NotImplementedException();
			public bool ConfirmExit => throw new NotImplementedException();
			public CivOne.Enums.HistorianAiPalace HistorianAiPalace => throw new NotImplementedException();
		}

		private sealed class RecordingStrategy : ISoundPlaybackStrategy
		{
			public List<string> Calls { get; } = [];
			public Dictionary<string, TimeSpan> Durations { get; } = [];

			public bool PlaySound(string soundName)
			{
				Calls.Add(soundName);
				return true;
			}

			public void Abort() => Calls.Add("stop");

			public bool TryGetDuration(string soundName, out TimeSpan duration)
				=> Durations.TryGetValue(soundName, out duration);
		}
	}
}
