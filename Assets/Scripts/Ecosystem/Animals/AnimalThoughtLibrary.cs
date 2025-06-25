using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AnimalThoughtLibrary", menuName = "Ecosystem/Animal Thought Library")]
public class AnimalThoughtLibrary : ScriptableObject
{
    public List<AnimalThoughtLine> allThoughts;
}

[System.Serializable]
public class AnimalThoughtLine
{
    [Header("Which Animal?")]
    public string speciesName; // e.g. "Bunny", "Fox", "Bird"

    [Header("Trigger Condition")]
    public ThoughtTrigger trigger;

    [Header("Possible Lines to Randomly Choose From")]
    public List<string> lines;
}
