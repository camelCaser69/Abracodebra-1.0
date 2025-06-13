// Assets\Scripts\Ecosystem\Effects\FireflyDefinition.cs
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FireflyDef_", menuName = "Ecosystem/Firefly Definition")]
public class FireflyDefinition : ScriptableObject
{
    [Header("Grid Movement")]
    public int movementTickInterval = 5;    // How often firefly decides to move tiles
    public int tileSearchRadius = 3;        // How far to look for attraction targets

    [Header("Local (Intra-Tile) Movement")]
    [Range(0.1f, 0.5f)]
    public float localMovementRadius = 0.4f; // Fraction of tile size for wander radius
    public float localMovementTurnSpeed = 90f; // Degrees per second
    public float minLocalSpeed = 0.5f;       // Units per second within tile
    public float maxLocalSpeed = 1.0f;       // Units per second within tile

    [Header("Lifetime")]
    public int minLifetimeTicks = 40;
    public int maxLifetimeTicks = 90;
    public int fadeInTicks = 4;
    public int fadeOutTicks = 8;

    [Header("Visual Effects")]
    public float glowFlickerAmount = 0.2f;  // Intensity variance from base
    public float glowFlickerSpeed = 5.0f;   // How fast the glow flickers
    public bool useSpawnEffect = true;      // Use a visual effect on spawn
    public int spawnEffectTicks = 3;        // How long the spawn effect lasts

    [Header("Attraction")]
    public List<ScentDefinition> attractiveScentDefinitions = new List<ScentDefinition>();
    public float scentAttractionWeight = 2.0f; // Multiplier for scent scores
    public float growingPlantAttraction = 1.0f; // Bonus score for being near a growing plant
}