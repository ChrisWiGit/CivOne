using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using CivOne.Persistence.Mapper;

namespace CivOne.Persistence.Model
{
	/// <summary>
	/// Maps between <see cref="ReplayData"/> domain objects and <see cref="ReplayDataDto"/>.
	/// The event type is implicitly determined by which nested-data property is non-null.
	/// <para>
	/// Only the three C# subtypes (<c>CityBuilt</c>, <c>CityDestroyed</c>,
	/// <c>CivilizationDestroyed</c>) have corresponding domain classes and can be fully
	/// round-tripped. All other types (WarDeclared, PeaceMade, …) can be stored in YAML
	/// but <see cref="FromDto"/> will throw for them until domain classes are added.
	/// </para>
	/// </summary>
	[SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists", Justification = "We want to use List<T> for simplicity and ease of use in YAML serialization, and we don't need the additional features of IReadOnlyList<T> or IEnumerable<T>.")]
	public class ReplayDataDtoMapper : IDtoMapper<ReplayDataDto, ReplayData>
	{
		public ReplayDataDto ToDto(ReplayData domain)
		{
			ArgumentNullException.ThrowIfNull(domain);

			return domain switch
			{
				ReplayData.CityBuilt cb => new ReplayDataDto
				{
					Turn = cb.Turn,
					CityBuilt = new ReplayDataDto.CityBuiltData
					{
						OwnerId = cb.OwnerId,
						CityId = cb.CityId,
						CityNameId = cb.CityNameId,
						X = cb.X,
						Y = cb.Y
					}
				},

				ReplayData.CityDestroyed cd => new ReplayDataDto
				{
					Turn = cd.Turn,
					CityDestroyed = new ReplayDataDto.CityDestroyedData
					{
						CityId = cd.CityId,
						CityNameId = cd.CityNameId,
						X = cd.X,
						Y = cd.Y
					}
				},

				ReplayData.CivilizationDestroyed civd => new ReplayDataDto
				{
					Turn = civd.Turn,
					CivilizationDestroyed = new ReplayDataDto.CivilizationDestroyedData
					{
						DestroyedId = civd.DestroyedId,
						DestroyedById = civd.DestroyedById
					}
				},

				// ── Types without domain class yet ──
				// Uncomment each block once the corresponding ReplayData subclass is added
				// in api/src/ReplayData.cs, and remove the matching throw in FromDto.

				// ReplayData.CityCaptured cc => new ReplayDataDto
				// {
				// 	Turn = cc.Turn,
				// 	CityCaptured = new ReplayDataDto.CityCapturedData
				// 	{
				// 		CivId      = cc.CivId,
				// 		CityNameId = cc.CityNameId,
				// 		X          = cc.X,
				// 		Y          = cc.Y
				// 	}
				// },

				// ReplayData.WarDeclared wd => new ReplayDataDto
				// {
				// 	Turn = wd.Turn,
				// 	WarDeclared = new ReplayDataDto.TwoCivsData { CivId = wd.CivId, CivId2 = wd.CivId2 }
				// },

				// ReplayData.PeaceMade pm => new ReplayDataDto
				// {
				// 	Turn = pm.Turn,
				// 	PeaceMade = new ReplayDataDto.TwoCivsData { CivId = pm.CivId, CivId2 = pm.CivId2 }
				// },

				// ReplayData.AdvanceDiscovered ad => new ReplayDataDto
				// {
				// 	Turn = ad.Turn,
				// 	AdvanceDiscovered = new ReplayDataDto.CivWithTypeIdData { CivId = ad.CivId, TypeId = ad.TypeId }
				// },

				// ReplayData.UnitFirstBuilt ub => new ReplayDataDto
				// {
				// 	Turn = ub.Turn,
				// 	UnitFirstBuilt = new ReplayDataDto.CivWithTypeIdData { CivId = ub.CivId, TypeId = ub.TypeId }
				// },

				// ReplayData.GovernmentChanged gc => new ReplayDataDto
				// {
				// 	Turn = gc.Turn,
				// 	GovernmentChanged = new ReplayDataDto.CivWithTypeIdData { CivId = gc.CivId, TypeId = gc.TypeId }
				// },

				// ReplayData.WonderBuilt wb => new ReplayDataDto
				// {
				// 	Turn = wb.Turn,
				// 	WonderBuilt = new ReplayDataDto.CivWithTypeIdData { CivId = wb.CivId, TypeId = wb.TypeId }
				// },

				// ReplayData.ReplaySummary rs => new ReplayDataDto
				// {
				// 	Turn = rs.Turn,
				// 	ReplaySummary = new ReplayDataDto.ReplaySummaryData { CityCount = rs.CityCount, Population = rs.Population }
				// },

				// ReplayData.CivRankings cr => new ReplayDataDto
				// {
				// 	Turn = cr.Turn,
				// 	CivRankings = new ReplayDataDto.CivRankingsData { Rankings = [.. cr.Rankings] }
				// },

				_ => throw new InvalidOperationException(
					$"Unsupported ReplayData subtype '{domain.GetType().Name}'.")
			};
		}


		private static bool ThrowIfMultipleNotNull(params object?[] properties)
		{
			if (properties.Count(p => p != null) > 1)
				throw new InvalidDataException(
					"ReplayDataDto has multiple event data properties set. Exactly one must be non-null.");
			return true;
		}

		public ReplayData FromDto(ReplayDataDto dto)
		{
			ArgumentNullException.ThrowIfNull(dto);

			ThrowIfMultipleNotNull(
				dto.CityBuilt,
				dto.CityDestroyed,
				dto.CityCaptured,
				dto.CivilizationDestroyed,
				dto.WarDeclared,
				dto.PeaceMade,
				dto.AdvanceDiscovered,
				dto.UnitFirstBuilt,
				dto.GovernmentChanged,
				dto.WonderBuilt,
				dto.ReplaySummary,
				dto.CivRankings);

			if (dto.CityBuilt is { } cb)
			{
				return new ReplayData.CityBuilt(
					turn: dto.Turn,
					ownerId: (byte)cb.OwnerId,
					cityId: cb.CityId,
					cityNameId: cb.CityNameId,
					x: cb.X,
					y: cb.Y);
			}

			if (dto.CityDestroyed is { } cd)
			{
				return new ReplayData.CityDestroyed(
					turn: dto.Turn,
					cityId: cd.CityId,
					cityNameId: cd.CityNameId,
					x: cd.X,
					y: cd.Y);
			}

			if (dto.CivilizationDestroyed is { } civd)
			{
				return new ReplayData.CivilizationDestroyed(
					turn: dto.Turn,
					destroyedId: (byte)civd.DestroyedId,
					destroyedById: (byte)civd.DestroyedById);
			}

			// ── Types without domain class yet ──
			//
			// These can appear in YAML files (e.g. loaded from a binary save and re-saved),
			// but cannot be converted to domain objects until the C# classes are added.
			// Add a case here and a new ReplayData subclass in api/src/ReplayData.cs.

			if (dto.WarDeclared != null) throw NotYetImplemented(nameof(ReplayDataDto.WarDeclared));
			if (dto.PeaceMade != null) throw NotYetImplemented(nameof(ReplayDataDto.PeaceMade));
			if (dto.AdvanceDiscovered != null) throw NotYetImplemented(nameof(ReplayDataDto.AdvanceDiscovered));
			if (dto.UnitFirstBuilt != null) throw NotYetImplemented(nameof(ReplayDataDto.UnitFirstBuilt));
			if (dto.GovernmentChanged != null) throw NotYetImplemented(nameof(ReplayDataDto.GovernmentChanged));
			if (dto.WonderBuilt != null) throw NotYetImplemented(nameof(ReplayDataDto.WonderBuilt));
			if (dto.ReplaySummary != null) throw NotYetImplemented(nameof(ReplayDataDto.ReplaySummary));
			if (dto.CivRankings != null) throw NotYetImplemented(nameof(ReplayDataDto.CivRankings));
			if (dto.CityCaptured != null) throw NotYetImplemented(nameof(ReplayDataDto.CityCaptured));

			throw new InvalidOperationException(
				"ReplayDataDto has no event data set. Exactly one nested property must be non-null.");
		}

		private static NotImplementedException NotYetImplemented(string typeName)
			=> new($"ReplayData type '{typeName}' is not yet implemented as a domain class. "
					+ $"Add a matching subclass to ReplayData in api/src/ReplayData.cs.");

		// ── Convenience list helpers ───────────────────────────────────────────────

		/// <summary>Maps a full list in one call.</summary>
		public List<ReplayDataDto> ToDtoList(IEnumerable<ReplayData> items)
			=> items?.Select(ToDto).ToList() ?? [];

		/// <summary>Maps a full list in one call.</summary>
		public List<ReplayData> FromDtoList(IEnumerable<ReplayDataDto> dtos)
			=> dtos?.Select(FromDto).ToList() ?? [];
	}
}
