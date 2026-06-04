using System;

namespace CivOne.Agents
{
	/// <summary>
	/// Entry point for one turn-based agent implementation.
	/// One call coordinates reads via <see cref="ITurnContext"/>, journal reads via
	/// <see cref="IEventJournal"/>, and mutations via <see cref="ITurnCommandGateway"/>.
	/// </summary>
	public interface ITurnBasedController
	{
		/// <summary>
		/// Executes one complete turn session.
		/// </summary>
		/// <param name="session">The current turn session.</param>
		void OnTurn(ITurnSession session);
	}

	/// <summary>
	/// Represents one host-controlled turn session.
	/// This is the root object passed to <see cref="ITurnBasedController.OnTurn(ITurnSession)"/>.
	/// </summary>
	public interface ITurnSession
	{
		/// <summary>
		/// Gets the live readonly context.
		/// </summary>
		ITurnContext Context { get; }

		/// <summary>
		/// Gets the event journal.
		/// </summary>
		IEventJournal Events { get; }

		/// <summary>
		/// Gets the command gateway.
		/// </summary>
		ITurnCommandGateway Commands { get; }

		/// <summary>
		/// Ends the current turn explicitly.
		/// </summary>
		void EndTurn();
	}

	/// <summary>
	/// Exposes the current allowed turn state as a live readonly view.
	/// State changes can be tracked incrementally through <see cref="IEventJournal"/>,
	/// while commands run through <see cref="ITurnCommandGateway"/>.
	/// </summary>
	public interface ITurnContext
	{
		/// <summary>
		/// Gets the current game turn.
		/// </summary>
		int GameTurn { get; }

		/// <summary>
		/// Gets the current controlled civilization.
		/// </summary>
		ICivilizationView CurrentCivilization { get; }

		/// <summary>
		/// Gets the current map view.
		/// </summary>
		IMapView Map { get; }

		/// <summary>
		/// Gets the current own units.
		/// </summary>
		System.Collections.Generic.IReadOnlyList<IUnitView> OwnUnits { get; }

		/// <summary>
		/// Gets the current own cities.
		/// </summary>
		System.Collections.Generic.IReadOnlyList<ICityView> OwnCities { get; }

		/// <summary>
		/// Gets the current available research names.
		/// </summary>
		System.Collections.Generic.IReadOnlyList<string> AvailableResearchNames { get; }

		/// <summary>
		/// Gets the current research name if any.
		/// </summary>
		string? CurrentResearchName { get; }
	}
}