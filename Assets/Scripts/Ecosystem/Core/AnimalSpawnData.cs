using UnityEngine;

[System.Serializable]
public class AnimalSpawnData {
    public AnimalDefinition animalDefinition;
    [Range(0f, 1f)]
    [Tooltip("Spawn rate multiplier (0 = no spawn; lower values = less frequent spawns).")]
    public float spawnRateMultiplier = 1f;
    
    [HideInInspector]
    public float spawnTimer = 0f; // Internal timer, do not edit in inspector
}