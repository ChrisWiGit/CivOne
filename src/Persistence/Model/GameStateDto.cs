using System;
using System.Collections.Generic;
using CivOne.Persistence.Model.Attributes;

namespace CivOne.Persistence.Model
{
    public class GameStateDto
    {
        [Doc("The difficulty level of the game.",
            nameof(DifficultyAll))]
        public DifficultyLevel Difficulty { get; set; }

        public uint GameTurn { get; set; }
        public ushort HumanPlayer { get; set; }

        public List<PlayerDto> Players { get; set; }

        public uint RandomSeed { get; set; }

        public uint AnthologyTurn { get; set; }

        // in MapDto.cs
        // public uint TerrainSeed { get; set; }

        public MapDto Map { get; set; }

        [Doc("The game options that are enabled in the game.",
            nameof(GameOptionsAll))]
        [YamlDotNet.Serialization.YamlMember(typeof(List<string>))]
        public List<GameOptionEnum> GameOptions { get; set; }

        private static string DifficultyAll { get => string.Join(", ", Enum.GetNames<DifficultyLevel>()); }
        private static string GameOptionsAll { get => string.Join(", ", Enum.GetNames<GameOptionEnum>()); }
    }
}