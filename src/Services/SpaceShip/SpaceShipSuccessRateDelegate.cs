using System;
using System.Diagnostics.CodeAnalysis;

namespace CivOne.Services.SpaceShip
{
	/// <summary>
	/// Calculates the mission success rate of a spaceship from its <see cref="SpaceShipMissionStatus"/>.
	/// Used by <see cref="SpaceShipScreenDataFactory"/>; read through <see cref="ISpaceShipService.SuccessRate"/>.
	/// </summary>
	internal sealed class SpaceShipSuccessRateDelegate
	{
		/// <summary>
		/// Calculates the success rate in percent.
		/// </summary>
		/// <param name="canLaunch">
		/// Whether the ship counts as complete.
		/// When <c>false</c>, a rate of 100 is capped to 99.
		/// </param>
		/// <param name="status">Part counts and derived percentages of the ship.</param>
		/// <returns>The success rate, clamped to 0-100.</returns>
		[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This class is a delegate, not a static utility.")]
		public int Calculate(bool canLaunch, SpaceShipMissionStatus status)
		{
			int successProbability = 100;
			if (status.CommandCount == 0)
			{
				successProbability -= 45;
			}
			if (status.HabitationCount == 0)
			{
				successProbability -= 25;
			}
			if (status.LifeSupportCount == 0)
			{
				successProbability -= 25;
			}
			if (status.PropulsionCount < 2)
			{
				successProbability -= 25;
			}
			if (status.FuelCount < 2)
			{
				successProbability -= 25;
			}
			if (status.Structural == 0)
			{
				successProbability -= 15;
			}

			successProbability -= (100 - status.Support) / 3;
			successProbability -= (100 - status.Energy) / 3;
			successProbability -= (100 - status.FuelPercent) / 4;

			successProbability = Math.Max(0, Math.Min(100, successProbability));
			if (!canLaunch && successProbability == 100)
			{
				successProbability = 99;
			}

			return successProbability;
		}
	}
}
