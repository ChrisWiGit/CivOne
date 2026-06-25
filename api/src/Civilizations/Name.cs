// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Diagnostics.CodeAnalysis;

namespace CivOne.Civilizations
{
	/// <summary>
	/// Modifiy the civilization name.
	/// </summary>
	/// <param name="name">The new name for the civilization. (example: Roman)</param>
	/// <param name="namePlural">The new plural name for the civilization. (example: Romans)</param>
	[SuppressMessage("Design", "CA1019:Define accessors for attribute arguments", Justification = "The Name attribute is intended to be used with positional parameters for the name and plural name, and does not require additional named properties for these values. The Value property provides access to the CivilizationName instance containing both the singular and plural names, and separate properties for NameValue and NamePlural are also provided for convenience.")]
	public sealed class Name(string name, string namePlural) : BaseAttribute(typeof(CivilizationName), new CivilizationName(name, namePlural), InRange)
	{
		public static bool InRange(object value) => ((CivilizationName)value).Valid;

		public CivilizationName Value => GetRequiredValue<CivilizationName>();

		public string NameValue => Value.Name;
		public string NamePlural => Value.Plural;
	}
}