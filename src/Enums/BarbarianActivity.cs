using System;

namespace CivOne.Enums
{
	/// <summary>
	/// Tells where barbarians may come from during a game.
	///
	/// Barbarians reach the map on three ways, and each one is a separate flag:
	/// tribal villages (huts) that release a horde when a unit enters them,
	/// raiding parties that appear inland,
	/// and raiding parties that arrive by ship and land on the coast.
	/// Land and sea raiders use their own spawn positions and their own unit lists, so they can be
	/// switched on separately.
	/// </summary>
	/// <example>
	/// <code>
	/// // Coastal raiders only, no inland uprisings and no barbarians in huts.
	/// BarbarianActivity activity = BarbarianActivity.SeaRaids;
	/// bool seaRaids = activity.HasFlag(BarbarianActivity.SeaRaids); // true
	/// </code>
	/// </example>
	[Flags]
	public enum BarbarianActivity
	{
		/// <summary>
		/// No barbarians at all.
		/// </summary>
		None = 0,

		/// <summary>
		/// Tribal villages may release barbarians.
		/// </summary>
		Villages = 1,

		/// <summary>
		/// Raiding parties may appear inland.
		/// </summary>
		LandRaids = 2,

		/// <summary>
		/// Raiding parties may arrive by ship and land on the coast.
		/// </summary>
		SeaRaids = 4,

		/// <summary>
		/// Both kinds of raiding parties, but no barbarians from villages.
		/// </summary>
		Raids = LandRaids | SeaRaids,

		/// <summary>
		/// Every source is active. This matches the original game.
		/// </summary>
		VillagesAndRaids = Villages | Raids
	}
}
