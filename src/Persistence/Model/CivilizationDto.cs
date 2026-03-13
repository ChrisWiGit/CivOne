using System.Linq;
using CivOne.Persistence.Model.Attributes;

namespace CivOne.Persistence.Model
{
    public class CivilizationDto
    {
        [Doc("The class name of the civilization's leader.",
            nameof(AllLeaderClassNames))]
        public string LeaderClassName { get; set; }

        /**
        * This is not a property of the dto, but a helper to provide documentation.
        * It will be ignored during serialization.
        */
        public static string AllLeaderClassNames { get => string.Join(", ", 
                Common.Civilizations.Select(c => c.Leader.GetType().Name)); }
    }
}