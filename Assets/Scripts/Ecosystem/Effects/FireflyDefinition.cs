using System.Collections.Generic;
using UnityEngine;

// [CreateAssetMenu(fileName = "FireflyDef", menuName = "Ecosystem/Firefly Definition")]
public class FireflyDefinition : ScriptableObject
{
    [Header("Behavior")]
    public int movementTickInterval = 5;
    public int tileSearchRadius = 3;

    [Header("Local Movement")]
    public Vector2 flightBounds = new Vector2(0.4f, 0.4f);
    public float flightHeightOffset = 0.2f;
    public float localMovementTurnSpeed = 90f;
    public float minLocalSpeed = 0.5f;
    public float maxLocalSpeed = 1.0f;

    [Header("Lifetime & Visuals")]
    public int minLifetimeTicks = 40;
    public int maxLifetimeTicks = 90;
    public float fadeInSeconds = 1.5f;
    public float fadeOutSeconds = 2.5f;
    public float glowFlickerAmount = 0.2f;
    public float glowFlickerSpeed = 5.0f;
    [Range(0f, 1f)] public float groundLightMinIntensity = 0.1f;

    [Header("Attractions")]
    public List<ScentDefinition> attractiveScentDefinitions = new List<ScentDefinition>();
    public float scentAttractionWeight = 2.0f;
    public float growingPlantAttraction = 1.0f;
    
    // Note: useSpawnEffect and spawnEffectTicks are now obsolete and replaced by fadeInSeconds
    [HideInInspector] public bool useSpawnEffect = true;
    [HideInInspector] public int spawnEffectTicks = 3;
}