using CivOne.Screens.Services;
using CivOne.Wonders;
using CivOne.Units;
using System;

namespace CivOne.UnitTests
{

    public class MockedGame : IGameCitizenDependency
    {
        public ushort GameTurn { get; set; }
        public int Difficulty { get; set; }
        public int MaxDifficulty { get; set; }

        public Func<byte, Player> OnGetPlayer { get; set; }
        public Func<int, int, IUnit[]> OnGetUnits { get; set; }
        public Func<Type, bool> OnWonderObsoleteByType { get; set; }
        public Func<IWonder, bool> OnWonderObsolete { get; set; }

        public Player GetPlayer(byte playerId)
            => OnGetPlayer?.Invoke(playerId)
                ?? throw new NotImplementedException("GetPlayer not implemented by delegate.");

        public IUnit[] GetUnits()
            => OnGetUnits?.Invoke(int.MinValue, int.MinValue)
                ?? throw new NotImplementedException("GetUnits not implemented by delegate.");
        public IUnit[] GetUnits(int x, int y)
            => OnGetUnits?.Invoke(x, y)
                ?? throw new NotImplementedException("GetUnits not implemented by delegate.");


        public bool WonderObsolete<T>() where T : IWonder, new()
            => OnWonderObsoleteByType?.Invoke(typeof(T))
                ?? throw new NotImplementedException("WonderObsolete<T> not implemented by delegate.");

        public bool WonderObsolete(IWonder wonder)
            => OnWonderObsolete?.Invoke(wonder)
                ?? throw new NotImplementedException("WonderObsolete(IWonder) not implemented by delegate.");
    }
}
