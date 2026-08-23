using System.Linq;
using CivOne.Persistence.Model.Attributes;

namespace CivOne.Persistence.Model
{
    #pragma warning disable CS8618, CA2211 // Non-nullable property must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public class CivilizationDto
    {
        [Doc("The class name of the civilization's leader.",
            nameof(AllLeaderClassNames))]
        public string LeaderClassName { get; set; }

        /**
        * This is not a property of the dto, but a helper to provide documentation.
        * It will be ignored during serialization.
        */
        public static string[] AllLeaderClassNames = [];
    }
}