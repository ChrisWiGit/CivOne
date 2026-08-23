using System.Linq;
using CivOne.Advances;

namespace CivOne.Services
{
	/// <summary>
	/// Implementation of IAdvanceManagementService.
	/// Provides functionality to manage technological advances for civilizations.
	/// </summary>
	internal class AdvanceManagementService : IAdvanceManagementService
	{
		private static readonly IAdvance[] AllAdvancesSorted =
			[.. Reflect.GetAdvances().OrderBy(x => x.TranslatedName)];

		public IAdvance[] GetAllAdvances() => AllAdvancesSorted;

		public bool HasAdvance(byte civNumber, IAdvance advance)
		{
			var player = Game.Instance.GetPlayer(civNumber);
			return player?.HasAdvance(advance) ?? false;
		}

		public void ToggleAdvance(byte civNumber, IAdvance advance)
		{
			var player = Game.Instance.GetPlayer(civNumber);
			if (player == null) return;

			if (player.HasAdvance(advance))
				player.DeleteAdvance(advance);
			else
				player.AddAdvance(advance);
		}
	}
}
