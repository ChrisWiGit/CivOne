using System;
using System.Collections.Generic;
using CivOne.Advances;
using CivOne.Units;
using CivOne.Wonders;

namespace CivOne.Persistence.Stubs
{
    /// <summary>
    /// Minimal stub implementation of IPlayerGame for use during YAML loading.
    /// Provides just enough functionality to allow City hydration without needing
    /// the full Game instance initialized.
    /// </summary>
    internal class PlayerGameStub : IPlayerGame
    {
        public bool Started => false;
        public ushort GameTurn => 0;
        public int Difficulty => 0;
        public Player HumanPlayer => null;
        public Player CurrentPlayer => null;
        public IEnumerable<Player> Players => [];
        public IWonder[] BuiltWonders => [];

        public byte PlayerNumber(Player player) => 0;
        public Player GetPlayer(byte number) => null;
        public City[] GetCities() => [];
        public IUnit[] GetUnits() => [];
        public void DisbandUnit(IUnit? unit) { }
        public bool WonderObsolete<T>() where T : IWonder, new() => false;
        public bool WonderBuilt<T>() where T : IWonder => false;
        public void SetAdvanceOrigin(IAdvance advance, Player player) { }
    }
}
