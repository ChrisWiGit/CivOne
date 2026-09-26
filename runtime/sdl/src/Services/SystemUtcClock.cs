using System;

namespace CivOne
{
	public sealed class SystemUtcClock : IUtcClock
	{
		public DateTime UtcNow => DateTime.UtcNow;
	}
}