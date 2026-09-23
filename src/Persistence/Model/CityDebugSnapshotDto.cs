namespace CivOne.Persistence.Model
{
	using CivOne.Persistence.Model.Attributes;

	/// <summary>
	/// Optional write-only save snapshot with city happiness diagnostics.
	/// These values are for manual save inspection and are ignored when loading a save.
	/// </summary>
	public class CityDebugSnapshotDto
	{
		[Doc("Whether Original city happiness model is enabled when the save is written.")]
		public bool OriginalHappinessModelEnabled { get; set; }

		[Doc("Difficulty level used for the snapshot.")]
		public int Difficulty { get; set; }

		[Doc("Government id of the city owner at snapshot time.")]
		public int GovernmentId { get; set; }

		[Doc("Whether the city owner is the human player.")]
		public bool IsHumanOwner { get; set; }

		[Doc("City index in owner city list at snapshot time.")]
		public int CityIndex { get; set; }

		[Doc("Luxuries rate of the owner player.")]
		public int LuxuriesRate { get; set; }

		[Doc("Taxes rate of the owner player.")]
		public int TaxesRate { get; set; }

		[Doc("Science rate of the owner player.")]
		public int ScienceRate { get; set; }

		[Doc("Final happy citizens after all modifiers.")]
		public int FinalHappy { get; set; }

		[Doc("Final content citizens after all modifiers.")]
		public int FinalContent { get; set; }

		[Doc("Final unhappy citizens after all modifiers.")]
		public int FinalUnhappy { get; set; }

		[Doc("Final parked/pending unhappy citizens shown as red shirts.")]
		public int FinalRedShirt { get; set; }

		[Doc("Specialist count at snapshot time.")]
		public int Specialists { get; set; }

		[Doc("Whether the city is in disorder based on current citizen result.")]
		public bool InDisorder { get; set; }

		[Doc("Total units currently standing on the city tile.")]
		public int UnitsInCityTile { get; set; }

		[Doc("Total units with this city as home city.")]
		public int HomeUnitsTotal { get; set; }

		[Doc("Home units currently standing on the city tile.")]
		public int HomeUnitsInCityTile { get; set; }

		[Doc("Home units currently outside the city tile.")]
		public int HomeUnitsOutside { get; set; }

		[Doc("Raw BaseUnhappy value from OriginalCityCitizenService, if available.")]
		public int? BaseUnhappyRaw { get; set; }

		[Doc("EmpireSizeBase value from OriginalCityCitizenService, if available.")]
		public int? EmpireSizeBase { get; set; }

		[Doc("EmpireSizePenalty value from OriginalCityCitizenService, if available.")]
		public int? EmpireSizePenalty { get; set; }
	}
}
