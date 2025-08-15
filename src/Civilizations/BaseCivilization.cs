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
using System.Linq;
using CivOne.Enums;
using CivOne.Leaders;

namespace CivOne.Civilizations
{
	public abstract class BaseCivilization<T> : BaseCivilization, ICivilization where T : ILeader, new()
	{
		public int Id { get; }

		private string _name;
		public string Name
		{
			get => Modifications.LastOrDefault(x => x.Name.HasValue)?.Name.Value.Name ?? _name;
			private set => _name = value;
		}

		private string _namePlural;
		public string NamePlural
		{
			get => Modifications.LastOrDefault(x => x.Name.HasValue)?.Name.Value.Plural ?? _namePlural;
			private set => _namePlural = value;
		}

		private ILeader _leader;
		public ILeader Leader
		{
			get => Modifications.LastOrDefault(x => x.LeaderId.HasValue)?.LeaderId.Value.ToInstance() ?? _leader;
			private set => _leader = value;
		}
		
		public byte PreferredPlayerNumber { get; }

		private byte _startX;
		public byte StartX
		{
			get => (byte)(Modifications.LastOrDefault(x => x.StartingPosition.HasValue)?.StartingPosition.Value.X ?? _startX);
			protected set => _startX = value;
		}

		private byte _startY;
		public byte StartY
		{
			get => (byte)(Modifications.LastOrDefault(x => x.StartingPosition.HasValue)?.StartingPosition.Value.Y ?? _startY);
			protected set => _startY = value;
		}

		private string[] _cityNames;
		public string[] CityNames
		{
			get => Modifications.LastOrDefault(x => x.CityNames.HasValue)?.CityNames.Value ?? _cityNames;
			protected set => _cityNames = value;
		}

		public string Tune { get; private set; }

		public BaseCivilization(Civilization civilization, string name, string namePlural, string tune = null) : base(civilization)
		{
			Id = (Civilization == Civilization.Barbarians ? 15 : (int)Civilization);
			PreferredPlayerNumber = (byte)(Civilization == Civilization.Barbarians ? 0 : ((int)Civilization - 1) % 7 + 1);
			Name = name;
			NamePlural = namePlural;
			Leader = new T();
			Tune = tune;
		}
	}

	public abstract class BaseCivilization : BaseInstance
	{
		protected Civilization Civilization { get; }

		private static Dictionary<Civilization, List<CivilizationModification>> _modifications = new Dictionary<Civilization, List<CivilizationModification>>();
		internal static void LoadModifications()
		{
			_modifications.Clear();

			CivilizationModification[] modifications = Reflect.GetModifications<CivilizationModification>().ToArray();
			if (modifications.Length == 0) return;

			Log("Applying civilization modifications");

			foreach (CivilizationModification modification in modifications)
			{
				if (!_modifications.ContainsKey(modification.Civilization))
					_modifications.Add(modification.Civilization, new List<CivilizationModification>());
				_modifications[modification.Civilization].Add(modification);
			}

			Log("Finished applying civilization modifications");
		}
		public IEnumerable<CivilizationModification> Modifications => _modifications.ContainsKey(Civilization) ? _modifications[Civilization].ToArray() : new CivilizationModification[0];

		protected BaseCivilization(Civilization civilization)
		{
			Civilization = civilization;
		}

		public delegate ICivilization BuddyCivilization(int preferredPlayerNumber);

		/// <summary>
		/// Returns a function that can be used to get a buddy civilization for a given player
		/// number. 
		/// As in Civilization.cs, civilizations are grouped by their preferred player id (1-7, 0 are barbarians, and one of seven is the human itself).
		/// Some (most) of civilizations have a buddy civilization (e.g. Babylonians and Zulus = 2)
		/// This method returns a delegate that returns a buddy civilization for a given player number (1-7).
		/// If you start a game with a specific seed, the buddy civilizations will be chosen randomly, but consistently for that seed.
		/// So you apply the InitialSeed to this method to get the same buddy civilizations every time.
		/// E.g.
		/// GetBuddyCivilizationSupplier(Common.Random.InitialSeed)(2) returns Babylonians
		/// GetBuddyCivilizationSupplier(Common.Random.InitialSeed)(2) returns Zulus
		/// or the other way around, depending on the InitialSeed.
		public static BuddyCivilization GetBuddyCivilizationSupplier(short InitialSeed)
		{
			Dictionary<int, int> _firstChoiceIndex = [];
			Random startRandom = new(InitialSeed);

			return preferredPlayerNumber =>
			{
				var civBuds = Common.Civilizations.OrderByDescending(c => c.Id)
					.Where(c => c.PreferredPlayerNumber == preferredPlayerNumber).ToArray();

				if (civBuds.Length == 0)
				{
					throw new System.Exception($"No civilization found for preferred player number {preferredPlayerNumber}.");
				}

				if (_firstChoiceIndex.TryGetValue(preferredPlayerNumber, out var firstIndex))
				{
					if (civBuds.Length < 2)
					{
						return civBuds[firstIndex];
					}

					return civBuds[1 - firstIndex];
				}

				int r = startRandom.Next(civBuds.Length);

				// Console.WriteLine($"Civilization {civBuds[r].Name} ({civBuds[r].Id}) is chosen as buddy for player number {preferredPlayerNumber} (index {r}).");

				_firstChoiceIndex[preferredPlayerNumber] = r;

				return civBuds[r];
			};
		}
	}
}