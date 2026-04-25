using CivOne.Buildings;
using CivOne.Wonders;
using System.Drawing;
using CivOne.Tiles;
using CivOne.Units;
using System;
using System.Linq;
using System.Collections.Generic;
using CivOne.Enums;
using CivOne.Persistence.Model;
using CivOne.Graphics;
using CivOne.Civilizations;
using CivOne.Leaders;

namespace CivOne.UnitTests
{
	public class MockedICivilization : ICivilization
	{
		public static List<MockedICivilization> Mock(int count)
		{
			return [.. Enumerable.Range(1, count).Select(i => new MockedICivilization(i, (byte)i))];
		}

		public MockedICivilization(int leaderId, byte id = 1)
		{
			Id = id;
			Name = $"TestCiv{id}";
			NamePlural = $"TestCivs{id}";
			// Counterpart to the comment in MockedILeader: tests resolve Civilization via
			// dto.LeaderClassName == civ.Leader.GetType().Name, so leader type names must be unique.
			Leader = CreateLeader(leaderId);
			PreferredPlayerNumber = id;
			StartX = (byte)(id * 2);
			StartY = (byte)(id * 3);
			CityNames = [$"TestCity{id}A", $"TestCity{id}B", $"TestCity{id}C"];
			Tune = $"TestTune{id}";
		}

		private static ILeader CreateLeader(int leaderId)
			=> leaderId switch
			{
				1 => new MockedILeader1(),
				2 => new MockedILeader2(),
				3 => new MockedILeader3(),
				_ => throw new ArgumentOutOfRangeException(
					nameof(leaderId),
					leaderId,
					"No dedicated MockedILeaderN type exists for this leaderId. Add a new MockedILeaderN class to keep LeaderClassName mapping unique in mapper tests.")
			};

		public int Id { get; set; }

		public string Name { get; set; }

		public string NamePlural { get; set; }

		public ILeader Leader { get; set; }

		public byte PreferredPlayerNumber { get; set; }

		public byte StartX { get; set; }

		public byte StartY { get; set; }

		public string[] CityNames { get; set; }

		public string Tune { get; set; }
	}
}