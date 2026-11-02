using System;

namespace ZooWorld.Definitions
{
    public static class SpeciesIdGenerator
    {
        public static string Generate()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}
