// File: Assets/Scripts/Genes/Services/DeterministicRandom.cs
using System;

namespace Abracodabra.Genes.Services
{
    public class DeterministicRandom : IDeterministicRandom
    {
        private System.Random rng;
        private int currentSeed;

        public DeterministicRandom(int seed)
        {
            SetSeed(seed);
        }

        public void SetSeed(int seed)
        {
            currentSeed = seed;
            rng = new System.Random(seed);
        }

        public float Range(float min, float max)
        {
            return (float)(rng.NextDouble() * (max - min) + min);
        }

        public int Range(int min, int max)
        {
            return rng.Next(min, max);
        }

        public void Reset()
        {
            rng = new System.Random(currentSeed);
        }
    }
}