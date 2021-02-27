using System;

namespace TidalLocking
{
    public struct PlanetConfig
    {
        public int id;
        public int seed;
        public float orbitPhase;

        public PlanetConfig(int id, int seed, float orbitPhase)
        {
            this.id = id;
            this.seed = seed;
            this.orbitPhase = orbitPhase;
        }

        public override bool Equals(object obj)
        {
            if (obj is PlanetConfig item)
            {
                Console.WriteLine($"this.seed={seed}item.seed={item.seed},this.id={id}item.id={item.id}");
                return item.seed == seed && item.id == id;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return id + seed;
        }
    }
}