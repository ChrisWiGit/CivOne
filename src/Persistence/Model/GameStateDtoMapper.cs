using System;
using System.Collections.Generic;
using System.Linq;
using CivOne.Civilizations;
using CivOne.Persistence.Model.Attributes;

namespace CivOne.Persistence.Model
{
    public class GameStateDtoMapper(
        PlayerDtoMapper playerMapper
    ) : DtoMapper<GameStateDto, GameState>
    {
        public GameState FromDto(GameStateDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            var players = dto.Players
                .Select(playerMapper.FromDto)
                .Cast<Player>()
                .ToArray();

            if (dto.HumanPlayer >= players.Length)
                throw new InvalidOperationException($"Human player index {dto.HumanPlayer} is out of range");

            var gameState = new GameState
            {
                GameTurn = dto.GameTurn,
                HumanPlayer = players[dto.HumanPlayer],
                RandomSeed = (int)dto.RandomSeed,
                Difficulty = (int)dto.Difficulty,
                Players = players,
                Units = [],  // TODO: implement units mapping from players
                GameOptions = dto.GameOptions,
                AnthologyTurn = (ushort)dto.AnthologyTurn,
                // TODO: implement map deserialization
            };

            return gameState;
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
			var gameStateDto = new GameStateDto
            {
                Difficulty = (DifficultyLevel)gameState.Difficulty,
                GameTurn = gameState.GameTurn,
                Players = [.. gameState.Players.Select(playerMapper.ToDto)],
                HumanPlayer = FindHumanPlayerIndex(gameState.Players, gameState.HumanPlayer),
                
                RandomSeed = (uint)gameState.RandomSeed,
                AnthologyTurn = gameState.AnthologyTurn,
                // Map = new MapDto(), // TODO: implement map dto and mapper
                Map = new MapDto
                {
                    // TerrainSeed = gameState.Map.TerrainSeed,
                    // Tiles = new Map2d<TileDto>(gameState.Map.Width, gameState.Map.Height)
                },

                GameOptions = gameState.GameOptions
			};
            
            foreach (var player in gameStateDto.Players)
            {
                player.Id = (ushort)gameStateDto.Players.IndexOf(player);
            }
            return gameStateDto;
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