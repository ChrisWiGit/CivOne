// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using CivOne.Graphics;

namespace CivOne.Concepts
{
	internal abstract class BaseConcept : BaseInstance, IConcept
	{
		/// <summary>
		/// Gets the localized display name shown to the player.
		/// </summary>
		/// <remarks>
		/// Derived concept classes must set this from <c>Translate("...")</c>.
		/// <para>
		/// The value of <see cref="Name"/> is also used as the invariant Civilopedia key,
		/// so it must be set to the English base value, for example <c>"Tax Rate"</c>.
		/// </para>
		/// <para>
		/// For concepts that do not exist in the original game, use a unique
		/// <see cref="Name"/> value and use the same value as the Civilopedia text key,
		/// for example <c>"MySpecialConcept"</c>.
		/// </para>
		/// <para>
		/// The test <c>RegisteredCivilopediaNamesTests</c>
		/// (<c>xunit/src/RegisteredCivilopediaNamesTests.cs</c>) verifies that all items
		/// have a non-empty translated name.
		/// </para>
		/// </remarks>
		/// <example>
		/// <code>
		/// Name = "Tax Rate";
		/// TranslatedName = Translate("Tax Rate");
		/// </code>
		/// </example>
		public string TranslatedName { get; protected set; } = "";
		/// <summary>
		/// Gets the invariant civilopedia key name.
		/// </summary>
		/// <example>
		/// <code>
		/// Name = "Tax Rate";
		/// TranslatedName = Translate("Tax Rate");
		/// </code>
		/// </example>
		public string Name { get; protected set; } = "";
		public IBitmap? Icon => null;
		public byte PageCount => 2;
		public Picture DrawPage(byte pageNumber)
		{
			string[] text = [];
			switch (pageNumber)
			{
				case 1:
					text = Resources.GetCivilopediaText("BLURB4/" + Name.ToUpperInvariant());
					break;
				case 2:
					text = Resources.GetCivilopediaText("BLURB4/" + Name.ToUpperInvariant() + "2");
					break;
				default:
					Log("Invalid page number: {0}", pageNumber);
					break;
			}
			
			Picture output = new Picture(320, 200);
			
			int yy = 76;
			foreach (string line in text)
			{
				Log(line);
				output.DrawText(line, 6, 1, 12, yy);
				yy += 9;
			}
			
			return output;
		}
    }
}