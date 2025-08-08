// File: Assets/Scripts/Genes/Core/PayloadGene.cs
using UnityEngine;
using Abracodabra.Genes.Runtime;

namespace Abracodabra.Genes.Core
{
    /// <summary>
    /// Base class for genes that attach to Active genes to add effects,
    /// such as adding damage, nutrition, or status effects to a projectile.
    /// </summary>
    public abstract class PayloadGene : GeneBase
    {
        public override GeneCategory Category => GeneCategory.Payload;

        [Header("Payload Settings")]
        public PayloadType payloadType;
        public float basePotency = 1f;

        /// <summary>
        /// Called when the parent Active gene executes, applying this payload's effect.
        /// </summary>
        public abstract void ApplyPayload(PayloadContext context);

        /// <summary>
        /// A specific helper for fruit-based active genes to attach components.
        /// </summary>
        public virtual void ConfigureFruit(Fruit fruit, RuntimeGeneInstance instance) { }

        /// <summary>
        /// A specific helper for direct-effect payloads (e.g., an aura).
        /// </summary>
        public virtual void ApplyToTarget(GameObject target, RuntimeGeneInstance instance) { }

        public override bool CanAttachTo(GeneBase other)
        {
            return other.Category == GeneCategory.Active;
        }

        public float GetFinalPotency(RuntimeGeneInstance instance)
        {
            if (instance == null) return basePotency;
            return basePotency * instance.GetValue("potency_multiplier", 1f);
        }
    }

    public enum PayloadType
    {
        Substance,  // Damage/status effects
        Nutrition,  // Healing/hunger
        Special     // Unique effects
    }

    /// <summary>
    /// A context object containing information for a PayloadGene's application.
    /// </summary>
    public class PayloadContext
    {
        public GameObject target;
        public PlantGrowth source;
        public ActiveGene parentGene;
        public RuntimeGeneInstance payloadInstance;
        public float effectMultiplier = 1f;
    }
}