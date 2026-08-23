using CivOne.Governments;
using CivOne.Persistence.Model;
using System;
using System.Linq;

namespace CivOne.Persistence.Factories
{
    public sealed class RuntimeGovernmentResolver : IGovernmentResolver
    {
        public IGovernment ResolveById(byte id)
        {
            return Reflect.GetGovernments().FirstOrDefault(g => g.Id == id)
                ?? throw new InvalidOperationException($"Government with ID {id} was not found.");
        }
    }
}
