// Assets/Scripts/Utility/PrefabOrigin.cs

using UnityEngine;

/// <summary>
/// A utility component to adjust a prefab's position upon instantiation.
/// When this prefab is spawned at a specific world position, this script ensures
/// that the assigned 'originTransform' child ends up at that exact position,
/// by offsetting the root object accordingly.
/// This component self-destructs after its one-time operation.
/// </summary>
public class PrefabOrigin : MonoBehaviour
{
    [Tooltip("Drag the child GameObject here that should act as the prefab's true origin/pivot point.")]
    public Transform originTransform;

    void Awake()
    {
        // --- Validation Step ---
        if (originTransform == null)
        {
            Debug.LogError($"[PrefabOrigin] The 'Origin Transform' is not assigned on '{gameObject.name}'. The script cannot function.", this);
            Destroy(this); // Destroy self if not configured
            return;
        }

        if (!originTransform.IsChildOf(transform))
        {
            Debug.LogError($"[PrefabOrigin] The assigned 'Origin Transform' ('{originTransform.name}') is not a child of '{gameObject.name}'. The script cannot function.", this);
            Destroy(this); // Destroy self if configuration is invalid
            return;
        }

        // --- The Core Logic ---
        // 1. Calculate the world-space offset vector from the root to the origin child.
        //    We use TransformVector to correctly account for the root's rotation and scale.
        Vector3 worldOffset = transform.TransformVector(originTransform.localPosition);

        // 2. Move the root transform by the inverse of this offset.
        //    This shifts the entire prefab so the child now occupies the root's original spawn position.
        transform.position -= worldOffset;
        
        // --- Cleanup Step ---
        // The component's job is done, so we destroy it to keep the scene clean
        // and prevent it from running again.
        Destroy(this);
    }
}