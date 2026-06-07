namespace CivOne.src
{
	using System.Collections.Generic;
	using System.Linq;
	using CivOne.Services;
	using Xunit;

	/// <summary>
	/// Verifies that all <see cref="ICivilopedia"/> items registered in the reflection
	/// and common registries have non-empty <c>Name</c> and <c>TranslatedName</c> values.
	/// Civilizations are excluded because <c>ICivilization</c> does not expose <c>TranslatedName</c>.
	/// </summary>
	public class RegisteredCivilopediaNamesTests : TestsBase
	{
		/// <summary>Initializes identity translation so no translation files are needed.</summary>
		public RegisteredCivilopediaNamesTests()
		{
			TranslationServiceFactory.UseIdentity();
		}

		/// <summary>
		/// All advances, buildings and wonders from <see cref="Common"/> must have non-empty names.
		/// </summary>
		[Fact]
		public void CommonRegistriesAllItemsHaveNonEmptyNames()
		{
			var entries = new List<(string Registry, ICivilopedia Item)>();
			foreach (var item in Common.Advances.ToArray())
				entries.Add(("Advances", item));
			foreach (var item in Common.Buildings.ToArray())
				entries.Add(("Buildings", item));
			foreach (var item in Common.Wonders.ToArray())
				entries.Add(("Wonders", item));

			AssertNames(entries);
		}

		/// <summary>
		/// All units, governments, concepts and terrain types from <see cref="Reflect"/> must have non-empty names.
		/// </summary>
		[Fact]
		public void ReflectRegistriesAllItemsHaveNonEmptyNames()
		{
			var entries = new List<(string Registry, ICivilopedia Item)>();
			foreach (var item in Reflect.GetUnits().ToArray())
				entries.Add(("Units", item));
			foreach (var item in Reflect.GetGovernments().ToArray())
				entries.Add(("Governments", item));
			foreach (var item in Reflect.GetConcepts().ToArray())
				entries.Add(("Concepts", item));
			foreach (var item in Reflect.GetCivilopediaTerrainTypes().ToArray())
				entries.Add(("TerrainTypes", item));

			AssertNames(entries);
		}

		private static void AssertNames(List<(string Registry, ICivilopedia Item)> entries)
		{
			foreach (var (registry, item) in entries)
			{
				Assert.False(string.IsNullOrWhiteSpace(item.Name), $"Item of type {item.GetType().Name} in registry {registry} has empty Name");
				Assert.False(string.IsNullOrWhiteSpace(item.TranslatedName), $"Item of type {item.GetType().Name} in registry {registry} has empty TranslatedName");
			}
		}
	}
}
