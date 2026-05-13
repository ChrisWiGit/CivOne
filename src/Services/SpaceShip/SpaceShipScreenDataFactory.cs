// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;

namespace CivOne.Services.SpaceShip
{
	public class SpaceShipScreenDataFactory(ISpaceShipSlotBlueprint slotBlueprint) : ISpaceShipScreenDataFactory
	{
		private readonly ISpaceShipSlotBlueprint _slotBlueprint = slotBlueprint;

		private readonly record struct MissionStatus(
			int CommandCount,
			int HabitationCount,
			int LifeSupportCount,
			int PropulsionCount,
			int FuelCount,
			int Structural,
			int Support,
			int Energy,
			int FuelPercent);

		private static int LegacyOrDetailed(bool useDetailed, int detailedValue, int legacyValue)
		{
			return useDetailed ? detailedValue : legacyValue;
		}

		private static int Percent(int value, int max)
		{
			if (max <= 0)
			{
				return 100;
			}

			return Math.Max(0, Math.Min(100, (int)Math.Round((value * 100.0) / max, MidpointRounding.AwayFromZero)));
		}

		private static int CalculateMass(int structural, int commandCount, int habitationCount, int lifeSupportCount, int solarCount, int propulsionCount, int fuelCount)
		{
			return (structural * 700)
				+ (commandCount * 3_500)
				+ (habitationCount * 3_200)
				+ (lifeSupportCount * 2_400)
				+ (solarCount * 1_600)
				+ (propulsionCount * 1_800)
				+ (fuelCount * 1_200);
		}

		private static int CalculateSuccessProbability(bool canLaunch, MissionStatus status)
		{
			int successProbability = 100;
			if (status.CommandCount == 0) successProbability -= 45;
			if (status.HabitationCount == 0) successProbability -= 25;
			if (status.LifeSupportCount == 0) successProbability -= 25;
			if (status.PropulsionCount < 2) successProbability -= 25;
			if (status.FuelCount < 2) successProbability -= 25;
			if (status.Structural == 0) successProbability -= 15;

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

		public SpaceShipScreenData Create(IPlayerSpaceRace player, bool canLaunch)
		{
			if (player == null)
			{
				return new SpaceShipScreenData(0, 0, 0, 0, 0, 0.0, 0, 0, 0, 0, 0, false);
			}

			SpaceShipPartCounts counts = SpaceShipPartCounter.Count(player.SpaceShipGrid);
			bool useDetailed = counts.DetailedPartCount > 0;

			int legacyCommand = counts.Module > 0 ? 1 : 0;
			int commandCount = LegacyOrDetailed(useDetailed, counts.CommandModule, legacyCommand);
			int habitationCount = LegacyOrDetailed(useDetailed, counts.HabitationModule, counts.Module);
			int lifeSupportCount = LegacyOrDetailed(useDetailed, counts.LifeSupportModule, counts.Module);
			int solarCount = LegacyOrDetailed(useDetailed, counts.SolarPanelModule, counts.Module);
			int propulsionCount = LegacyOrDetailed(useDetailed, counts.PropulsionComponent, counts.Component);
			int fuelCount = LegacyOrDetailed(useDetailed, counts.FuelComponent, counts.Component);

			int structural = counts.StructuralTotal;
			int component = counts.ComponentTotal;
			int module = counts.ModuleTotal;
			int totalParts = counts.TotalParts;

			int population = Math.Max((int)player.SpaceShipPopulation, habitationCount * 10_000);
			int support = Percent(structural, _slotBlueprint.MaxStructuralSlots);
			int modulesNeedingPower = commandCount + habitationCount + lifeSupportCount;
			int energy = modulesNeedingPower == 0 ? 0 : Percent(solarCount * 3, modulesNeedingPower);
			int mass = CalculateMass(structural, commandCount, habitationCount, lifeSupportCount, solarCount, propulsionCount, fuelCount);
			int fuel = propulsionCount == 0 ? 0 : Percent(fuelCount, propulsionCount);
			double flightTime = propulsionCount == 0
				? 0.0
				: Math.Max(3.0, 22.0 - (propulsionCount * 2.1) - (fuelCount * 0.6));
			MissionStatus missionStatus = new(
				commandCount,
				habitationCount,
				lifeSupportCount,
				propulsionCount,
				fuelCount,
				structural,
				support,
				energy,
				fuel);
			int successProbability = CalculateSuccessProbability(canLaunch, missionStatus);

			return new SpaceShipScreenData(
				population,
				support,
				energy,
				mass,
				fuel,
				flightTime,
				successProbability,
				structural,
				component,
				module,
				totalParts,
				canLaunch);
		}
	}
}
