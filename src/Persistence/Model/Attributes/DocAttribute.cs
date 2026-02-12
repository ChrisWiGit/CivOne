using System;

namespace CivOne.Persistence.Model.Attributes
{
    public sealed class DocAttribute : Attribute
    {
        public string Description { get; }
        public string[] AllowedValues { get; }

        public DocAttribute(
            string description,
            params string[] allowedValues)
        {
            Description = description;
            AllowedValues = allowedValues;
        }
    }

}