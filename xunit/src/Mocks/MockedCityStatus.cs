using System.Collections.Generic;
using CivOne.Tiles;
using CivOne.Units;
using System;
using CivOne.UserInterface;

namespace CivOne.UnitTests
{
    public class MockedCityStatus : ICityStatus
    {
        public bool IsRiot { get; set; }
        public bool IsCoastal { get; set; }
        public bool CelebrationCancelled { get; set; }
        public bool HydroAvailable { get; set; }
        public bool AutoBuild { get; set; }
        public bool TechStolen { get; set; }
        public bool CelebrationOrRapture { get; set; }
        public bool BuildingSold { get; set; }
    }
}
