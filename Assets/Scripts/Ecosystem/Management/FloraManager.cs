// REWORKED FILE: Assets/Scripts/Ecosystem/Management/FloraManager.cs
using UnityEngine;

public class FloraManager : MonoBehaviour
{
    public static FloraManager Instance { get; private set; }

    [Tooltip("The base rate of energy generation per leaf per tick, before sunlight, genes, or other modifiers.")]
    [SerializeField] public float basePhotosynthesisRatePerLeaf = 0.1f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
}