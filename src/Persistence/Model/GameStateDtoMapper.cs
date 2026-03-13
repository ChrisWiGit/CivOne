using System;
using System.Collections.Generic;
using System.Linq;
using CivOne.Civilizations;
using CivOne.Persistence.Model.Attributes;

namespace CivOne.Persistence.Model
{
    public class GameStateDtoMapper(
        DtoMapper<PlayerDto, Player> playerMapper
    ) : DtoMapper<GameStateDto, GameState>
    {
        public GameState FromDto(GameStateDto dto)
        {
            throw new NotImplementedException();
        }

        ushort FindHumanPlayerIndex(Player[] players, Player humanPlayer)
        {
            for (ushort i = 0; i < players.Length; i++)
            {
                if (players[i] == humanPlayer)
                {
                    return i;
                }
            }
            throw new Exception("Human player not found in players array");
        }

        public GameStateDto ToDto(GameState gameState)
		{
			return new GameStateDto
            {
                Difficulty = (DifficultyLevel)gameState.Difficulty,
                GameTurn = gameState.GameTurn,
                Players = [.. gameState.Players.Select(playerMapper.ToDto)],
                HumanPlayer = FindHumanPlayerIndex(gameState.Players, gameState.HumanPlayer),
                
                RandomSeed = (uint)gameState.RandomSeed,
                AnthologyTurn = gameState.AnthologyTurn,
                TerrainSeed = (uint)gameState.TerrainSeed,
                // Map = new MapDto(), // TODO: implement map dto and mapper

                GameOptions = gameState.GameOptions,
            };
		}

        // public class GameStateDto
        // {
        //     [Doc("The difficulty level of the game.",
        //         nameof(DifficultyAll))]
        //     public DifficultyLevel Difficulty { get; set; }

        //     public uint GameTurn { get; set; }
        //     public ushort HumanPlayer { get; set; }

        //     public List<PlayerDto> Players { get; set; }

        //     public uint RandomSeed { get; set; }

        //     public uint AnthologyTurn { get; set; }

        //     public uint TerrainSeed { get; set; }

        //     public MapDto Map { get; set; }

        //     [Doc("The game options that are enabled in the game.",
        //         nameof(GameOptionsAll))]
        //     [YamlDotNet.Serialization.YamlMember(typeof(List<string>))]
        //     public List<GameOptionEnum> GameOptions { get; set; }

        //     private static string DifficultyAll { get => string.Join(", ", Enum.GetNames<DifficultyLevel>()); }
        //     private static string GameOptionsAll { get => string.Join(", ", Enum.GetNames<GameOptionEnum>()); }
        // }
    }
}