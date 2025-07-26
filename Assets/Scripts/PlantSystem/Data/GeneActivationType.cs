// Assets/Scripts/PlantSystem/Data/GeneActivationType.cs
using System;

[Serializable]
public enum GeneActivationType
{
    /// <summary>
    /// Always active. Effects are applied once at plant creation to modify base stats.
    /// (e.g., growth speed, energy storage, leaf pattern).
    /// </summary>
    Passive,

    /// <summary>
    /// Executes on a cooldown cycle, consuming energy. Can be a simple action (like GrowBerry)
    /// or a Trigger that activates a subsequent Payload gene.
    /// </summary>
    Active,

    /// <summary>
    /// Does nothing on its own. Is activated by a preceding Active/Trigger gene in the sequence.
    /// (e.g., a damage effect that is launched by a TimerCast).
    /// </summary>
    Payload
}