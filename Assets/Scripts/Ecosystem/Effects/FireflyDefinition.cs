using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Firefly_Def", menuName = "Ecosystem/Firefly Definition")]
public class FireflyDefinition : ScriptableObject
{
    [Header("Tile-Based Movement")]
    public int movementTickInterval = 5;
    public int tileSearchRadius = 3;

    [Header("Real-Time Flight Behavior")]
    [Tooltip("How far the firefly can fly from the tile center. X is horizontal, Y is vertical. The flight area's bottom edge starts at the tile center.")]
    public Vector2 flightBounds = new Vector2(0.4f, 0.4f); // This field replaces localMovementRadius
    public float localMovementTurnSpeed = 90f;
    public float minLocalSpeed = 0.5f;
    public float maxLocalSpeed = 1.0f;

    [Header("Lifetime")]
    public int minLifetimeTicks = 40;
    public int maxLifetimeTicks = 90;
    public int fadeInTicks = 4;
    public int fadeOutTicks = 8;

    [Header("Visuals")]
    public float glowFlickerAmount = 0.2f;
    public float glowFlickerSpeed = 5.0f;
    public bool useSpawnEffect = true;
    public int spawnEffectTicks = 3;

    [Header("Attraction")]
    public List<ScentDefinition> attractiveScentDefinitions = new List<ScentDefinition>();
    public float scentAttractionWeight = 2.0f;
    public float growingPlantAttraction = 1.0f;
}