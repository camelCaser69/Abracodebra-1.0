// FILE: Assets/Scripts/Genes/Core/PayloadGene.cs
using UnityEngine;
using Abracodabra.Genes.Runtime;
using Abracodabra.Genes.Components;

namespace Abracodabra.Genes.Core
{
    public abstract class PayloadGene : GeneBase
    {
        public override GeneCategory Category => GeneCategory.Payload;

        [Header("Payload Settings")]
        public PayloadType payloadType;
        public float basePotency = 1f;

        public abstract void ApplyPayload(PayloadContext context);

        public virtual void ConfigureFruit(Fruit fruit, RuntimeGeneInstance instance) { }

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

        /// <summary>
        /// Returns true if this payload has plant-healing behavior (leaf regrowth).
        /// Used by Cloud/Aura effects to activate plant healing logic.
        /// </summary>
        public virtual bool IsPlantHealingPayload => payloadType == PayloadType.Healing;
    }

    public enum PayloadType
    {
        Substance,  // Damage/status effects (Poison, Slow, Freeze, Fear)
        Nutrition,  // Food/hunger (Nutritious)
        Healing,    // HP restoration + leaf regrowth
        Special     // Unique effects (Explosive, etc.)
    }

    public class PayloadContext
    {
        public GameObject target;
        public PlantGrowth source;
        public ActiveGene parentGene;
        public RuntimeGeneInstance payloadInstance;
        public float effectMultiplier = 1f;
    }
}