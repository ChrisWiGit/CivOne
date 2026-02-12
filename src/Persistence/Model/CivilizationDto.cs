using System.Linq;
using CivOne.Persistence.Model.Attributes;

namespace CivOne.Persistence.Model
{
    public class CivilizationDto
    {
        [Doc("The class name of the civilization's leader.",
            nameof(AllLeaderClassNames))]
        public string LeaderClassName { get; set; }
        private string AllLeaderClassNames { get => string.Join(", ", 
                Common.Civilizations.Select(c => c.Leader.GetType().Name)); }
    }
}