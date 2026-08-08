using System;
using System.Collections.Generic;
using System.Linq;
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
    /// <param name="players">
    /// The players of the save file currently being hydrated, in player-number order.
    /// They are needed because city hydration resolves its owner by player number, and the
    /// <see cref="Game"/> singleton still holds the previously loaded game at that point.
    /// </param>
    internal class PlayerGameStub(IReadOnlyList<Player>? players = null) : IPlayerGame
    {
        private readonly IReadOnlyList<Player> _players = players ?? [];

        public bool Started => false;
        public ushort GameTurn => 0;
        public int Difficulty => 0;
        public bool DisableBuddyCivilizationRespawn => false;
        public Player HumanPlayer => null!;
        public Player CurrentPlayer => null!;
        public IEnumerable<Player> Players => _players;
        public IWonder[] BuiltWonders => [];

        public byte PlayerNumber(Player player)
        {
            int index = _players.ToList().IndexOf(player);
            return index < 0 ? (byte)0 : (byte)index;
        }

        /// <summary>
        /// Returns the player being hydrated for the given player number, or <c>null</c> if the
        /// number is outside the save file's player range.
        /// </summary>
        public Player GetPlayer(byte number) => number < _players.Count ? _players[number] : null!;
        public City[] GetCities() => [];
        public IUnit[] GetUnits() => [];
        public void DisbandUnit(IUnit? unit) { }
        public bool WonderObsolete<T>() where T : IWonder, new() => false;
        public bool WonderBuilt<T>() where T : IWonder => false;
        public void SetAdvanceOrigin(IAdvance advance, Player player) { }
    }
}
