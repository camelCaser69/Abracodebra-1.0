using UnityEngine;

/// <summary>
/// A marker component for any part of a plant that can be harvested.
/// It holds a reference to the NodeDefinition that this item becomes in the inventory.
/// </summary>
public class HarvestableTag : MonoBehaviour
{
    public NodeDefinition HarvestedItemDefinition;
}