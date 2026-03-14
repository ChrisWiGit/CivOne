using System;

namespace CivOne.Persistence.Model.Attributes
{
	/// <summary>
	/// Attribute to provide documentation for properties in DTO classes. The description will be included as a comment in the generated YAML files.
	/// Optionally, the name of another property can be provided which lists the allowed values
	/// </summary>
	/// <param name="description">The description of the property.</param>
	/// <param name="allowedValuesPropertyName">The name of the property that lists the allowed values.</param>
	/// <example>
	/// [Doc("The architectural style of this palace section.")]
	/// public PalaceStyle Style { get; set; }
	/// 
	/// [Doc("Specialists in the city.", nameof(AllSpecialists))]
	/// public List<Citizen> Specialists { get; set; }
	/// public static readonly Citizen[] AllSpecialists = [Citizen.Entertainer, Citizen.Scientist, Citizen.Taxman];
	/// </example>
	[AttributeUsage(AttributeTargets.Property)]
	public sealed class DocAttribute(
		string description,
		string allowedValuesPropertyName = null,
		string commentValuesPropertyName = null) : Attribute
	{
		public string Description { get; } = description;
		public string AllowedValuesPropertyName { get; } = allowedValuesPropertyName;
		public string CommentValuesPropertyName { get; } = commentValuesPropertyName;
		public string AllowedValues { get; }

		public DocAttribute(string description, string[] allowedValues) : this(description)
		{
			AllowedValues = string.Join(", ", allowedValues);
		}

		public DocAttribute(string description, long minValue, long maxValue) : this(description)
		{
			AllowedValues = $"[{minValue} to {maxValue}]";
		}
	}
}