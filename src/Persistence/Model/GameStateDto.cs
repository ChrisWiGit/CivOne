using System;
using System.Collections.Generic;
using CivOne.Persistence.Model.Attributes;

namespace CivOne.Persistence.Model
{
    public class GameStateDto
    {
        private uint _gameRandomSeed;

        [Doc("The difficulty level of the game.",
            nameof(DifficultyAll))]
        public DifficultyLevel Difficulty { get; set; }

        public uint GameTurn { get; set; }

        [Doc("The id of the human-controlled player. This usually matches CurrentPlayer at the start/end of turns, but can differ while AI turns are processed.")]
        public ushort HumanPlayer { get; set; }

        [Doc("The id of the player whose turn is currently active. This is typically equal to HumanPlayer, but can differ when loading/saving in the middle of AI turns.")]
        public ushort CurrentPlayer { get; set; }

        public List<PlayerDto> Players { get; set; }

        [Doc("Seed for the game-wide random generator used by gameplay logic.")]
        public uint GameRandomSeed
        {
            get => _gameRandomSeed;
            set => _gameRandomSeed = value;
        }

        public uint RandomSeed
        {
            get => _gameRandomSeed;
            set => _gameRandomSeed = value;
        }

        public uint AnthologyTurn { get; set; }

        // in MapDto.cs
        // public uint TerrainSeed { get; set; }

        public MapDto Map { get; set; }

        [Doc("The game options that are enabled in the game.",
            nameof(GameOptionsAll))]
        [YamlDotNet.Serialization.YamlMember(typeof(List<string>))]
        public List<GameOptionEnum> GameOptions { get; set; }

        [Doc("Maps each advance ID to the player number who first discovered it.")]
        public Dictionary<byte, byte> AdvanceOrigin { get; set; }

        [Doc("Replay events recorded during the game session.")]
        public List<ReplayDataDto> ReplayData { get; set; }

        [Doc("Global peace turn counter from the original save format. Reserved for future diplomacy logic.", 0, ushort.MaxValue)]
        public ushort PeaceTurns { get; set; }

        [Doc("Legacy future-tech counter from the original save format. New YAML should prefer Players[].FutureTechCount. For backward compatibility this still mirrors the human player's count.", 0, ushort.MaxValue)]
        public ushort PlayerFutureTech { get; set; }

		[Doc("Global warming simulation state (count, pollution level, warning indicator).")]
		public GlobalWarmingDto GlobalWarming { get; set; }

        private static string DifficultyAll { get => string.Join(", ", Enum.GetNames<DifficultyLevel>()); }
        private static string GameOptionsAll { get => string.Join(", ", Enum.GetNames<GameOptionEnum>()); }
    }
}