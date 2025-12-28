// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

// Author: Kevin Routley : July, 2019

using System.Linq;
using System.Collections.Generic;
using CivOne.Wonders;
using System;
using CivOne.Governments;
using CivOne.Advances;

namespace CivOne.UnitTests
{
    partial class MockedPlayer : Player
    {
        private int citiesCount;
        private City[] _cities;
        private ICity[] _citiesInterface;

        public MockedPlayer() : base()
        {
            this.citiesCount = 0;
        }

        public override City[] Cities => _cities;
        public override ICity[] CitiesInterface => _citiesInterface;
        public MockedPlayer withCities(City[] cities)
        {
            this._cities = cities;
            this.citiesCount = cities.Length;
            return this;
        }
        public MockedPlayer withCitiesInterface(ICity[] cities)
        {
            this._citiesInterface = cities;
            this.citiesCount = cities.Length;
            return this;
        }

        public MockedPlayer withCitiesCount(int count)
        {
            this.citiesCount = count;
            this._cities = new City[count];
            return this;
        }

        private IAdvance[] _advances = Array.Empty<IAdvance>();
        public MockedPlayer withAdvances(params IAdvance[] advances)
        {
            this._advances = advances;
            return this;
        }
        public MockedPlayer withAdvance<TAdvance>(bool add = true)
            where TAdvance : IAdvance, new()
        {
            if (add)
            {
                var advancesList = _advances.ToList();
                advancesList.Add(new TAdvance());
                _advances = advancesList.ToArray();
            }
            else
            {
                _advances = _advances
                    .Where(a => a.GetType() != typeof(TAdvance))
                    .ToArray();
            }
            return this;
        }

        public override bool HasAdvance<T>()
             => _advances.Any(a => a.GetType() == typeof(T));

        public override bool HasAdvance(IAdvance advance)
        {
            return _advances.Any(a => a.GetType() == advance.GetType());
        }

        private HashSet<Type> _wonderEffects = new HashSet<Type>();
        public MockedPlayer WithWonderEffect<T>(bool add = true)
            where T : IWonder, new()
        {
            if (add)
            {
                _wonderEffects.Add(typeof(T));
            }
            else
            {
                _wonderEffects.Remove(typeof(T));
            }
            return this;
        }
        public override bool HasWonderEffect<T>()
             => _wonderEffects.Contains(typeof(T));

        public MockedPlayer withGovernment(IGovernment government)
        {
            this.Government = government;
            return this;
        }
        public MockedPlayer WithGovernmentType(Type government)
        {
            IGovernment gov = government switch
            {
                var t when t == typeof(Republic) => new Republic(),
                var t when t == typeof(CivOne.Governments.Democracy) => new CivOne.Governments.Democracy(),
                var t when t == typeof(Anarchy) => new Anarchy(),
                var t when t == typeof(Despotism) => new Despotism(),
                var t when t == typeof(CivOne.Governments.Monarchy) => new CivOne.Governments.Monarchy(),
                _ => throw new NotImplementedException($"Government type {government} not implemented in MockPlayer"),
            };
            this.Government = gov;
            return this;
        }
    }
}
