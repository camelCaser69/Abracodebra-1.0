using UnityEngine;

/// <summary>
/// Simple tag component that marks a GameObject as harvestable.
/// The actual harvested item is determined by the plant's NodeGraph.
/// </summary>
public class HarvestableTag : MonoBehaviour
{
    // No fields needed - this is just a marker component!
    // The harvest logic will look at the plant's NodeGraph to determine what to harvest
}