// FILE: Assets/Scripts/Ecosystem/Core/WaveDefinition.cs
using UnityEngine;
using System.Collections.Generic;

// Enum WaveSpawnLocationType remains the same
public enum WaveSpawnLocationType
{
    GlobalSpawnArea,
    RandomNearPlayer,
    Offscreen
}

// Class WaveSpawnEntry remains the same
[System.Serializable]
public class WaveSpawnEntry
{
    [Tooltip("Optional description for this specific spawn group within the wave.")]
    public string description = "Spawn Group";
    [Tooltip("The type of animal to spawn.")]
    public AnimalDefinition animalDefinition;
    [Tooltip("How many of this animal to spawn in this specific entry.")]
    [Min(1)]
    public int spawnCount = 1;
    [Tooltip("Delay (in seconds) after the wave's gameplay starts (AFTER 'Incoming' message) before *this entry* begins spawning.")] // Clarified tooltip
    [Min(0)]
    public float delayAfterWaveStart = 0f;
    [Tooltip("Time (in seconds) between spawning each individual animal in this entry (0 = spawn all instantly).")]
    [Min(0)]
    public float spawnInterval = 0.5f;
    [Tooltip("Where these animals should spawn.")]
    public WaveSpawnLocationType spawnLocationType = WaveSpawnLocationType.GlobalSpawnArea;
    [Tooltip("Radius used for spawning (e.g., if Spawn Location Type is RandomNearPlayer).")]
    [Min(0)]
    public float spawnRadius = 5f;
}

// WaveDefinition ScriptableObject is simplified
[CreateAssetMenu(fileName = "Wave_", menuName = "Ecosystem/Wave Definition")]
public class WaveDefinition : ScriptableObject
{
    [Header("Wave Identification")]
    [Tooltip("Editor-only name for this wave.")]
    public string waveName = "New Wave";

    [Header("Wave Content")]
    [Tooltip("Define the groups of animals that spawn during this wave.")]
    public List<WaveSpawnEntry> spawnEntries = new List<WaveSpawnEntry>();

    // REMOVED: endCondition (Always Timer or Day/Night Cycle)
    // REMOVED: durationSeconds (Handled by WaveManager)
    // REMOVED: delayBeforeNextWave (Handled by WaveManager)
}