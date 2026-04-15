using System;
using CivOne.Persistence.Model;
using CivOne.Services.GlobalWarming;

namespace CivOne.Persistence.Mapper
{
    public class GlobalWarmingDtoMapper(IValueSanitizer valueSanitizer) : DtoMapper<GlobalWarmingDto, GameState>
    {
        public GameState FromDto(GlobalWarmingDto dto)
        {
            if (dto == null)
            {
                return new GameState
                {
                    GlobalWarmingCount = 0,
                    PollutedSquaresCount = 0,
                    WarmingIndicator = WarmingIndicator.None
                };
            }

            var globalWarmingCount = valueSanitizer.ClampToInt32(
                dto.GlobalWarmingCount,
                nameof(GlobalWarmingDtoMapper),
                nameof(GlobalWarmingDto.GlobalWarmingCount),
                min: 0,
                max: short.MaxValue);

            var pollutedSquaresCount = valueSanitizer.ClampToInt32(
                dto.PollutedSquaresCount,
                nameof(GlobalWarmingDtoMapper),
                nameof(GlobalWarmingDto.PollutedSquaresCount),
                min: 0,
                max: int.MaxValue);

            var warmingIndicator = Enum.IsDefined(typeof(WarmingIndicator), dto.WarmingIndicator)
                ? dto.WarmingIndicator
                : WarmingIndicator.None;

            return new GameState
            {
                GlobalWarmingCount = globalWarmingCount,
                PollutedSquaresCount = pollutedSquaresCount,
                WarmingIndicator = warmingIndicator
            };
        }

        public GlobalWarmingDto ToDto(GameState domain)
        {
            ArgumentNullException.ThrowIfNull(domain);

            var globalWarmingCount = valueSanitizer.ClampToInt32(
                domain.GlobalWarmingCount,
                nameof(GlobalWarmingDtoMapper),
                nameof(GameState.GlobalWarmingCount),
                min: 0,
                max: short.MaxValue);

            var pollutedSquaresCount = valueSanitizer.ClampToInt32(
                domain.PollutedSquaresCount,
                nameof(GlobalWarmingDtoMapper),
                nameof(GameState.PollutedSquaresCount),
                min: 0,
                max: int.MaxValue);

            var warmingIndicator = Enum.IsDefined(typeof(WarmingIndicator), domain.WarmingIndicator)
                ? domain.WarmingIndicator
                : WarmingIndicator.None;

            return new GlobalWarmingDto
            {
                GlobalWarmingCount = globalWarmingCount,
                PollutedSquaresCount = pollutedSquaresCount,
                WarmingIndicator = warmingIndicator
            };
        }
    }
}
