// FILE: Assets/Scripts/ProceduralGeneration/SimplexNoise.cs
using UnityEngine;
using System;

namespace WegoSystem.ProceduralGeneration
{
    /// <summary>
    /// Simplex noise generator for procedural terrain generation.
    /// Provides better visual quality than Perlin with no directional artifacts.
    /// </summary>
    public static class SimplexNoise
    {
        private static readonly int[] perm = new int[512];
        private static readonly float F2 = 0.5f * (Mathf.Sqrt(3f) - 1f);
        private static readonly float G2 = (3f - Mathf.Sqrt(3f)) / 6f;

        private static readonly Vector2[] grad2 = {
            new Vector2(1, 1), new Vector2(-1, 1), new Vector2(1, -1), new Vector2(-1, -1),
            new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, 1), new Vector2(0, -1)
        };

        /// <summary>
        /// Initialize permutation table with seed for deterministic generation
        /// </summary>
        public static void Initialize(int seed)
        {
            System.Random rand = new System.Random(seed);
            int[] p = new int[256];

            for (int i = 0; i < 256; i++)
            {
                p[i] = i;
            }

            // Shuffle
            for (int i = 255; i > 0; i--)
            {
                int j = rand.Next(i + 1);
                int temp = p[i];
                p[i] = p[j];
                p[j] = temp;
            }

            for (int i = 0; i < 512; i++)
            {
                perm[i] = p[i & 255];
            }
        }

        /// <summary>
        /// Generate 2D simplex noise value at given coordinates
        /// </summary>
        /// <returns>Value between -1 and 1</returns>
        public static float Generate2D(float x, float y)
        {
            float s = (x + y) * F2;
            int i = Mathf.FloorToInt(x + s);
            int j = Mathf.FloorToInt(y + s);

            float t = (i + j) * G2;
            float X0 = i - t;
            float Y0 = j - t;
            float x0 = x - X0;
            float y0 = y - Y0;

            int i1, j1;
            if (x0 > y0)
            {
                i1 = 1; j1 = 0;
            }
            else
            {
                i1 = 0; j1 = 1;
            }

            float x1 = x0 - i1 + G2;
            float y1 = y0 - j1 + G2;
            float x2 = x0 - 1f + 2f * G2;
            float y2 = y0 - 1f + 2f * G2;

            int ii = i & 255;
            int jj = j & 255;
            int gi0 = perm[ii + perm[jj]] % 8;
            int gi1 = perm[ii + i1 + perm[jj + j1]] % 8;
            int gi2 = perm[ii + 1 + perm[jj + 1]] % 8;

            float n0 = 0f, n1 = 0f, n2 = 0f;

            float t0 = 0.5f - x0 * x0 - y0 * y0;
            if (t0 >= 0)
            {
                t0 *= t0;
                n0 = t0 * t0 * Dot(grad2[gi0], x0, y0);
            }

            float t1 = 0.5f - x1 * x1 - y1 * y1;
            if (t1 >= 0)
            {
                t1 *= t1;
                n1 = t1 * t1 * Dot(grad2[gi1], x1, y1);
            }

            float t2 = 0.5f - x2 * x2 - y2 * y2;
            if (t2 >= 0)
            {
                t2 *= t2;
                n2 = t2 * t2 * Dot(grad2[gi2], x2, y2);
            }

            return 70f * (n0 + n1 + n2);
        }

        /// <summary>
        /// Generate octaved noise for more natural terrain
        /// </summary>
        public static float GenerateOctaves(float x, float y, NoiseParameters parameters)
        {
            if (parameters.octaves <= 0) return 0f;

            float amplitude = 1f;
            float frequency = 1f;
            float noiseValue = 0f;
            float maxValue = 0f;

            for (int i = 0; i < parameters.octaves; i++)
            {
                float sampleX = (x + parameters.offset.x) * parameters.scale * frequency;
                float sampleY = (y + parameters.offset.y) * parameters.scale * frequency;

                float octaveValue = Generate2D(sampleX, sampleY);
                noiseValue += octaveValue * amplitude;
                maxValue += amplitude;

                amplitude *= parameters.persistence;
                frequency *= parameters.lacunarity;
            }

            // Normalize to -1 to 1 range
            if (maxValue > 0)
            {
                return noiseValue / maxValue;
            }
            return 0f;
        }

        private static float Dot(Vector2 g, float x, float y)
        {
            return g.x * x + g.y * y;
        }

        /// <summary>
        /// Remap noise value from one range to another
        /// </summary>
        public static float Remap(float value, float from1, float to1, float from2, float to2)
        {
            return from2 + (value - from1) * (to2 - from2) / (to1 - from1);
        }
    }

    /// <summary>
    /// Configurable parameters for noise generation
    /// </summary>
    [System.Serializable]
    public class NoiseParameters
    {
        [Header("Basic Settings")]
        [Tooltip("Scale of the noise (smaller = more zoomed in)")]
        [Range(0.001f, 1f)]
        public float scale = 0.1f;

        [Tooltip("Seed for deterministic generation")]
        public int seed = 42;

        [Header("Octave Settings")]
        [Tooltip("Number of noise layers (more = more detail)")]
        [Range(1, 8)]
        public int octaves = 4;

        [Tooltip("Amplitude decrease per octave (smaller = less detail influence)")]
        [Range(0f, 1f)]
        public float persistence = 0.5f;

        [Tooltip("Frequency increase per octave (higher = more detail frequency)")]
        [Range(1f, 4f)]
        public float lacunarity = 2f;

        [Header("Offset")]
        [Tooltip("X/Y offset for noise sampling")]
        public Vector2 offset = Vector2.zero;

        public NoiseParameters() { }

        public NoiseParameters(float scale, int octaves, int seed)
        {
            this.scale = scale;
            this.octaves = octaves;
            this.seed = seed;
        }

        /// <summary>
        /// Sample noise at position with these parameters. Returns a value between -1 and 1.
        /// </summary>
        public float Sample(float x, float y)
        {
            SimplexNoise.Initialize(seed);
            return SimplexNoise.GenerateOctaves(x, y, this);
        }
    }
}