using System;

namespace CivOne.Agents
{
	/// <summary>
	/// Represents the common result of one command executed via
	/// <see cref="ITurnCommandGateway"/>.
	/// </summary>
	public interface ICommandResult
	{
		/// <summary>
		/// Gets a value indicating whether the command succeeded.
		/// </summary>
		bool Success { get; }

		/// <summary>
		/// Gets the machine-readable error code.
		/// </summary>
		string ErrorCode { get; }

		/// <summary>
		/// Gets the human-readable error message if any.
		/// </summary>
		string? ErrorMessage { get; }

		/// <summary>
		/// Gets the journal sequence before the command ran.
		/// Can be used with <see cref="IEventJournal.ReadSince(ulong)"/>.
		/// </summary>
		ulong SequenceBefore { get; }

		/// <summary>
		/// Gets the journal sequence after the command finished.
		/// Can be used with <see cref="IEventJournal.ReadSince(ulong)"/>.
		/// </summary>
		ulong SequenceAfter { get; }
	}

	/// <summary>
	/// Default command result implementation.
	/// </summary>
	/// <param name="Success">True if the command succeeded.</param>
	/// <param name="ErrorCode">The machine-readable error code.</param>
	/// <param name="ErrorMessage">The human-readable error message if any.</param>
	/// <param name="SequenceBefore">The journal sequence before the command ran.</param>
	/// <param name="SequenceAfter">The journal sequence after the command finished.</param>
	public readonly record struct CommandResult(
		bool Success,
		string ErrorCode,
		string? ErrorMessage,
		ulong SequenceBefore,
		ulong SequenceAfter) : ICommandResult;

	/// <summary>
	/// Exposes unit commands for the current turn.
	/// </summary>
	public interface IUnitCommandGateway
	{
		/// <summary>
		/// Moves one own unit by the provided delta.
		/// </summary>
		/// <param name="unitId">The unit identifier.</param>
		/// <param name="dx">Relative X movement.</param>
		/// <param name="dy">Relative Y movement.</param>
		/// <returns>The command result including sequence boundaries.</returns>
		ICommandResult Move(Guid unitId, int dx, int dy);

		/// <summary>
		/// Fortifies one own unit.
		/// </summary>
		/// <param name="unitId">The unit identifier.</param>
		/// <returns>The command result including sequence boundaries.</returns>
		ICommandResult Fortify(Guid unitId);

		/// <summary>
		/// Wakes one own unit from sentry or fortify states.
		/// </summary>
		/// <param name="unitId">The unit identifier.</param>
		/// <returns>The command result including sequence boundaries.</returns>
		ICommandResult Wake(Guid unitId);

		/// <summary>
		/// Sets a goto destination for one own unit.
		/// </summary>
		/// <param name="unitId">The unit identifier.</param>
		/// <param name="x">Destination X coordinate.</param>
		/// <param name="y">Destination Y coordinate.</param>
		/// <returns>The command result including sequence boundaries.</returns>
		ICommandResult SetGoto(Guid unitId, int x, int y);

		/// <summary>
		/// Clears a goto destination for one own unit.
		/// </summary>
		/// <param name="unitId">The unit identifier.</param>
		/// <returns>The command result including sequence boundaries.</returns>
		ICommandResult ClearGoto(Guid unitId);

		/// <summary>
		/// Disbands one own unit.
		/// </summary>
		/// <param name="unitId">The unit identifier.</param>
		/// <returns>The command result including sequence boundaries.</returns>
		ICommandResult Disband(Guid unitId);

		/// <summary>
		/// Orders one own settler to found a city.
		/// </summary>
		/// <param name="unitId">The unit identifier.</param>
		/// <returns>The command result including sequence boundaries.</returns>
		ICommandResult FoundCity(Guid unitId);

		/// <summary>
		/// Orders one own settler to build a road.
		/// </summary>
		/// <param name="unitId">The unit identifier.</param>
		/// <returns>The command result including sequence boundaries.</returns>
		ICommandResult BuildRoad(Guid unitId);

		/// <summary>
		/// Orders one own settler to build irrigation.
		/// </summary>
		/// <param name="unitId">The unit identifier.</param>
		/// <returns>The command result including sequence boundaries.</returns>
		ICommandResult BuildIrrigation(Guid unitId);

		/// <summary>
		/// Orders one own settler to build a mine.
		/// </summary>
		/// <param name="unitId">The unit identifier.</param>
		/// <returns>The command result including sequence boundaries.</returns>
		ICommandResult BuildMine(Guid unitId);
	}

	/// <summary>
	/// Exposes city production commands for the current turn.
	/// </summary>
	public interface ICityCommandGateway
	{
		/// <summary>
		/// Chooses city production by internal production name.
		/// </summary>
		/// <param name="cityId">The city identifier.</param>
		/// <param name="productionName">The internal production type name.</param>
		/// <returns>The command result including sequence boundaries.</returns>
		ICommandResult ChooseProduction(Guid cityId, string productionName);
	}

	/// <summary>
	/// Exposes research commands for the current turn.
	/// </summary>
	public interface IResearchCommandGateway
	{
		/// <summary>
		/// Chooses current research by internal advance name.
		/// </summary>
		/// <param name="researchName">The internal advance type name.</param>
		/// <returns>The command result including sequence boundaries.</returns>
		ICommandResult ChooseResearch(string researchName);
	}

	/// <summary>
	/// Root command gateway for the current turn.
	/// Exposed by <see cref="ITurnSession.Commands"/>.
	/// </summary>
	public interface ITurnCommandGateway
	{
		/// <summary>
		/// Gets unit commands.
		/// </summary>
		IUnitCommandGateway Units { get; }

		/// <summary>
		/// Gets city commands.
		/// </summary>
		ICityCommandGateway Cities { get; }

		/// <summary>
		/// Gets research commands.
		/// </summary>
		IResearchCommandGateway Research { get; }
	}
}