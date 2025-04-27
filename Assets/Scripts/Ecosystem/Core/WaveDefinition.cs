// FILE: Assets/Scripts/Ecosystem/Core/WaveDefinition.cs
using UnityEngine;
using System.Collections.Generic;

public enum WaveEndCondition
{
    DefeatAllSpawned, // Wave ends when all enemies *spawned by this wave* are gone
    Timer,            // Wave ends after a set duration
    // SurviveDuration // Could be added later if needed
}

public enum WaveSpawnLocationType
{
    GlobalSpawnArea, // Use FaunaManager's defined spawn area
    RandomNearPlayer // Spawn within a radius of the player
    // SpecificPoints // Could be added later (requires point references)
}

[System.Serializable] // Allow this class to be edited within WaveDefinition's Inspector
public class WaveSpawnEntry
{
    [Tooltip("Optional description for this specific spawn group within the wave.")]
    public string description = "Spawn Group";

    [Tooltip("The type of animal to spawn.")]
    public AnimalDefinition animalDefinition;

    [Tooltip("How many of this animal to spawn in this specific entry.")]
    [Min(1)]
    public int spawnCount = 1;

    [Tooltip("Delay (in seconds) after the wave starts before *this entry* begins spawning.")]
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

[CreateAssetMenu(fileName = "Wave_", menuName = "Ecosystem/Wave Definition")]
public class WaveDefinition : ScriptableObject
{
    [Header("Wave Identification")]
    [Tooltip("Editor-only name for this wave.")]
    public string waveName = "New Wave";

    [Header("Wave Content")]
    [Tooltip("Define the groups of animals that spawn during this wave.")]
    public List<WaveSpawnEntry> spawnEntries = new List<WaveSpawnEntry>();

    [Header("Wave Completion")]
    [Tooltip("How this wave is considered completed.")]
    public WaveEndCondition endCondition = WaveEndCondition.DefeatAllSpawned;

    [Tooltip("Duration in seconds (only used if End Condition is Timer).")]
    [Min(1)]
    public float durationSeconds = 60f;

    [Header("Post-Wave")]
    [Tooltip("Pause duration (in seconds) after this wave ends before the next wave can start.")]
    [Min(0)]
    public float delayBeforeNextWave = 5.0f;
}