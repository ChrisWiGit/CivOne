using CivOne.Advances;
using CivOne.Persistence.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CivOne.Persistence.Factories
{
    public sealed class RuntimeAdvanceResolver : IAdvanceResolver
    {
        public IAdvance ResolveById(uint id)
        {
            return Common.Advances.FirstOrDefault(a => a.Id == id)
                ?? throw new InvalidOperationException($"Advance with ID {id} was not found.");
        }

        public IEnumerable<byte> ResolveAllIds()
        {
            return Common.Advances
                .OrderBy(a => a.Id)
                .Select(a => a.Id);
        }
    }
}
