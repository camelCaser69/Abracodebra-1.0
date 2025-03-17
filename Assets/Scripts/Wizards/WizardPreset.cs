using UnityEngine;


[CreateAssetMenu(fileName = "WizardPreset", menuName = "Wizards/WizardPreset")]
public class WizardPreset : ScriptableObject
{
    public string presetName;
    public float maxHP = 100f;
    public float accuracy = 5f;      // In degrees: how much the projectile can deviate.
    public float critChance = 0f;    // Future use.
    public FiringDirection baseFiringDirection = FiringDirection.Up;
    // Add additional stats as needed.
}