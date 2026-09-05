using System.Collections.Generic;
using System.Linq;

namespace CivOne.Sound.Cvl;

/// <summary>The results of converting a whole folder of CVL modules.</summary>
internal sealed class CvlConversionReport
{
    public List<CvlConversionResult> Results { get; } = [];

    public bool AnyConverted => Results.Any(r => r.Converted);

    public IEnumerable<string> Messages => Results.Select(r => r.Message);
}
