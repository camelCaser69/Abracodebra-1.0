using UnityEngine;

[CreateAssetMenu(fileName = "NewStatusEffect", menuName = "Ecosystem/Status Effect")]
public class StatusEffect : ScriptableObject
{
    public string effectID = "status_effect";
    public string displayName = "Status Effect";
    public Sprite icon;
    public string unicodeSymbol = "?";
    public Color effectColor = Color.white;

    public bool isPermanent = false;
    public int durationTicks = 10;

    public bool modifyAnimalColor = false;
    public Color animalTintColor = Color.white;
    public GameObject visualEffectPrefab;

    public bool damagePerTick = false;
    public float damageAmount = 0f;

    public bool healPerTick = false;
    public float healAmount = 0f;

    public bool modifyHunger = false;
    public float hungerModifier = 0f;

    [Tooltip("Multiplier for the VISUAL movement speed between tiles. Does not affect logical tiles-per-tick speed.")]
    public float visualInterpolationSpeedMultiplier = 1f;

    public float damageResistanceMultiplier = 1f;

    public int additionalMoveTicks = 0;

    public bool canStack = false;
    public int maxStacks = 1;
}