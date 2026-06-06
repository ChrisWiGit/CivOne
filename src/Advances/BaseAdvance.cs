// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CivOne.Buildings;
using CivOne.Enums;
using CivOne.Graphics;
using CivOne.Units;
using CivOne.Wonders;

namespace CivOne.Advances
{
	internal abstract class BaseAdvance(byte page, byte column, byte row, params Advance[] requiredTechs) : BaseInstance, IAdvance
	{
		private readonly byte _page = page, _column = column, _row = row;

		private readonly Advance[] _requiredTechs = requiredTechs;
		
		private IEnumerable<IAdvance> GetRequiredTechs()
		{
			foreach (Advance advance in _requiredTechs)
			{
				IAdvance requiredTech = Common.Advances.FirstOrDefault(x => x.Id == (byte)advance)
					?? throw new InvalidOperationException($"Required tech '{advance}' (Id {(byte)advance}) is not registered.");
				yield return requiredTech;
			}
		}
		
		public IBitmap Icon
		{
			get
			{
				int xx = 1 + (111 * _column);
				int yy = 1 + (69 * _row);
				int ww = _column < 2 ? 112 : 96;
				int hh = _row < 2 ? 68 : 60;

				using Picture icon = Resources[$"ICONPG{_page}"][xx, yy, ww, hh];
				
				OriginalColours = icon.Palette.Copy();
				return new Picture(112, 68, icon.Palette)
					.AddLayer(icon, _column < 2 ? 0 : 7, _row < 2 ? 0 : 4)
					.FillRectangle(110, 0, 2, 68, 0);
			}
		}

		public Palette OriginalColours { get; private set; } = new Palette();

		/// <summary>
		/// Gets the localized display name shown to the player.
		/// </summary>
		/// <remarks>
		/// Derived advance classes must set this from <c>Translate("...")</c>.
		/// <para>
		/// The value of <see cref="Name"/> is also used as the invariant Civilopedia key,
		/// so it must be set to the English base value, for example <c>"Alphabet"</c>.
		/// </para>
		/// <para>
		/// For advances that do not exist in the original game, use a unique
		/// <see cref="Name"/> value and use the same value as the Civilopedia text key,
		/// for example <c>"MySpecialAdvance"</c>.
		/// </para>
		/// <para>
		/// The test <c>RegisteredCivilopediaNamesTests</c>
		/// (<c>xunit/src/RegisteredCivilopediaNamesTests.cs</c>) verifies that all items
		/// have a non-empty translated name.
		/// </para>
		/// </remarks>
		/// <example>
		/// <code>
		/// Name = "Alphabet";
		/// TranslatedName = Translate("Alphabet");
		/// </code>
		/// </example>
		public string TranslatedName { get; protected set; } = "Invalid translated advance name";
		/// <summary>
		/// Gets the invariant civilopedia key name.
		/// </summary>
		/// <example>
		/// <code>
		/// Name = "Alphabet";
		/// TranslatedName = Translate("Alphabet");
		/// </code>
		/// </example>
		public string Name { get; protected set; } = "Invalid advance name";

		public byte PageCount => 2;
		public Picture DrawPage(byte pageNumber)
		{
			Picture output = new(320, 200);
			
			int yy;
			switch (pageNumber)
			{
				case 1:
					string [] text = Resources.GetCivilopediaText($"BLURB0/{Name.ToUpperInvariant()}");
					
					yy = 76;
					foreach (string line in text)
					{
						Log(line);
						output.DrawText(line, 6, 1, 12, yy);
						yy += 9;
					}
					
					break;
				case 2:
					yy = 84;
					if (pageNumber == 2)
					{
						if (RequiredTechs.Length > 0)
						{
							StringBuilder requiredTech = new();
							foreach (IAdvance tech in RequiredTechs)
							{
								if (requiredTech.Length > 0)
									requiredTech.Append(" and ");
								requiredTech.Append(tech.TranslatedName);
							}
							output.DrawText(TranslateFormatted("Requires {0}", requiredTech), 6, 1, 32, yy); yy += 8;
						}
						yy += 16;
						output.DrawText(Translate("Allows:"), 6, 1, 32, yy); yy += 8;
						foreach (IAdvance tech in Common.Advances.Where(a => a.Requires(Id)))
						{
							string allows = tech.TranslatedName;
							foreach (IAdvance at in tech.RequiredTechs.Where(a => a.Id != Id))
								allows += TranslateFormatted(" (with {0})", at.TranslatedName);
							output.DrawText(allows, 6, 9, 40, yy); yy += 8;
						}
						yy += 4;
						foreach (IUnit unit in Reflect.GetUnits().Where(u => u.RequiredTech?.Id == Id))
						{
							output.AddLayer(unit.ToBitmap(Game.PlayerNumber(Human)), 40, yy - 5);
							output.DrawText(TranslateFormatted("{0} unit", unit.TranslatedName), 6, 12, 60, yy); yy += 12;
						}
						foreach (IBuilding building in Reflect.GetBuildings().Where(b => b.RequiredTech?.Id == Id))
						{
							if (building.SmallIcon != null)
								output.AddLayer(building.SmallIcon, 39, yy - 2);
							output.DrawText(TranslateFormatted("{0} improvement", building.TranslatedName), 6, 2, 60, yy); yy += 12;
						}
						foreach (IWonder wonder in Reflect.GetWonders().Where(w => w.RequiredTech?.Id == Id))
						{
							if (wonder.SmallIcon != null)
								output.AddLayer(wonder.SmallIcon, 39, yy - 2);
							output.DrawText(TranslateFormatted("{0} Wonder", wonder.TranslatedName), 6, 2, 60, yy); yy += 12;
						}
					}
					break;
				default:
					Log("Invalid page number: {0}", pageNumber);
					break;
			}
			
			return output;
		}
		
		protected Advance Type { get; set; }
		
		public IAdvance[] RequiredTechs => [.. GetRequiredTechs()];
		
		public byte Id => (byte)Type;
		
		public bool Requires(byte id)
		{
			foreach (IAdvance tech in GetRequiredTechs())
				if (tech.Id == id) return true;
			return false;
		}

		public bool Is<T>() where T : IAdvance => this is T;

		public bool Not<T>() where T : IAdvance => this is not T;
	}
}