using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "AnimalLibrary", menuName = "Ecosystem/Animal Library")]
public class AnimalLibrary : ScriptableObject
{
    public List<AnimalDefinition> animals;
}