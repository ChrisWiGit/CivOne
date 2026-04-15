using CivOne.Persistence.Model.Attributes;

namespace CivOne.Persistence.Model
{
	using PlayerId = System.UInt16;

	public class DiplomacyDecodedDto
	{
		// Placeholder for future decoded bit-flags from SaveData.Diplomacy.
		// Planned examples: IsAtWar, HasPeaceTreaty, HasCeaseFire, HasContact.
	}

	public class DiplomacyEntryDto
	{
		[Doc("Target player id this diplomacy entry refers to.", 0, 255)]
		public PlayerId TargetPlayerId { get; set; }

		[Doc("Target player GUID for future stable cross-player references.")]
		public System.Guid TargetPlayerGuid { get; set; }

		[Doc("Raw diplomacy flags (ushort bitmask), 1:1 with legacy save format.", 0, 65535)]
		public ushort RawFlags { get; set; }

		[Doc("Optional decoded view of RawFlags. Currently placeholder for future semantics.")]
		public DiplomacyDecodedDto Decoded { get; set; }
	}
}
