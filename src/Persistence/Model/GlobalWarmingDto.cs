using CivOne.Persistence.Model.Attributes;
using CivOne.Services.GlobalWarming;

namespace CivOne.Persistence.Model
{
    /// <summary>
    /// Persisted snapshot of the global warming simulation state.
    /// </summary>
    public class GlobalWarmingDto
    {
        /// <summary>
        /// Number of times global warming has triggered since the start of the game.
        /// Each event raises the pollution threshold for the next event by 2.
        /// </summary>
		[Doc("The number of times global warming has triggered since the start of the game. Each event raises the pollution threshold for the next event by 2.")]
        public int GlobalWarmingCount { get; set; }

        /// <summary>
        /// The number of polluted map squares at the end of the last turn.
        /// </summary>
		[Doc("The number of polluted map squares at the end of the last turn.")]
        public int PollutedSquaresCount { get; set; }

        /// <summary>
        /// The warming indicator color shown in the status bar (None, DarkRed, LightRed, Yellow, White).
        /// </summary>
		[Doc("The warming indicator color shown in the status bar.", typeof(WarmingIndicator))]
        public WarmingIndicator WarmingIndicator { get; set; }
    }
}
