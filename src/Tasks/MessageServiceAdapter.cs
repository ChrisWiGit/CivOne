using CivOne.Enums;
using CivOne.Units;

namespace CivOne.Tasks
{
	/// <summary>
	/// Default adapter implementation of <see cref="IMessageService"/>.
	/// Forwards to the static <see cref="Message"/> factory methods for dependency injection scenarios.
	/// </summary>
	internal sealed class MessageServiceAdapter : IMessageService
	{
		/// <inheritdoc/>
		public Message Advisor(Advisor advisor, bool leftAlign, params string[] message) => Message.Advisor(advisor, leftAlign, message);

		/// <inheritdoc/>
		public Message Spy(params string[] message) => Message.Spy(message);

		/// <inheritdoc/>
		public Message DisbandUnit(City city, IUnit unit) => Message.DisbandUnit(city, unit);

		/// <inheritdoc/>
		public Message NewGoverment(City? city, params string[] message) => Message.NewGoverment(city, message);

		/// <inheritdoc/>
		public Message Newspaper(City? city, params string[] message) => Message.Newspaper(city, message);

		/// <inheritdoc/>
		public Message General(params string[] message) => Message.General(message);

		/// <inheritdoc/>
		public Message Help(string title, params string[] message) => Message.Help(title, message);

		/// <inheritdoc/>
		public Message Error(string title, params string[] message) => Message.Error(title, message);
	}
}