using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A base class for any MonoBehaviour that needs its speed to be modifiable
/// by stacking multipliers, such as from slowdown zones.
/// </summary>
public class SpeedModifiable : MonoBehaviour
{
    #region Fields
    
    [Tooltip("The normal, unmodified speed of the entity.")]
    [SerializeField] protected float baseSpeed = 5f;

    [Tooltip("The current speed after all multipliers have been applied.")]
    [SerializeField] protected float currentSpeed;
    
    List<float> activeSpeedMultipliers = new List<float>();

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// Initializes the component, setting the current speed to the base speed.
    /// </summary>
    protected virtual void Awake()
    {
        currentSpeed = baseSpeed;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Applies a speed multiplier. If multiple multipliers are active, the lowest one takes effect.
    /// </summary>
    /// <param name="multiplier">The speed multiplier to apply (e.g., 0.5 for 50% speed).</param>
    public void ApplySpeedMultiplier(float multiplier)
    {
        if (!activeSpeedMultipliers.Contains(multiplier))
        {
            activeSpeedMultipliers.Add(multiplier);
            UpdateSpeed();
        }
    }

    /// <summary>
    /// Removes a previously applied speed multiplier.
    /// </summary>
    /// <param name="multiplier">The speed multiplier to remove.</param>
    public void RemoveSpeedMultiplier(float multiplier)
    {
        if (activeSpeedMultipliers.Remove(multiplier))
        {
            UpdateSpeed();
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Recalculates the current speed based on the lowest active multiplier and fires the OnSpeedChanged event if needed.
    /// </summary>
    void UpdateSpeed()
    {
        float lowestMultiplier = 1.0f;
        foreach (float mult in activeSpeedMultipliers)
        {
            if (mult < lowestMultiplier)
            {
                lowestMultiplier = mult;
            }
        }
        
        float newSpeed = baseSpeed * lowestMultiplier;

        // Only update and call event if the speed actually changed
        if (!Mathf.Approximately(currentSpeed, newSpeed))
        {
            currentSpeed = newSpeed;
            OnSpeedChanged(currentSpeed);
        }
    }
    
    /// <summary>
    /// A virtual method that can be overridden by child classes to react to speed changes.
    /// For example, to adjust animation playback speed.
    /// </summary>
    /// <param name="newSpeed">The newly calculated speed.</param>
    protected virtual void OnSpeedChanged(float newSpeed) { }

    #endregion
}