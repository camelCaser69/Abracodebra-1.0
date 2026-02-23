// File: Assets/Scripts/Genes/WorldEffects/ProjectileWorldEffect.cs
using System.Collections.Generic;
using UnityEngine;
using Abracodabra.Genes.Core;
using Abracodabra.Genes.Runtime;

namespace Abracodabra.Genes.WorldEffects
{
    /// <summary>
    /// Visual projectile that travels from plant to target creature.
    /// Uses Update() for real-time travel (NOT tick-based).
    /// On arrival, applies damage + payload effects and destroys itself.
    /// </summary>
    public class ProjectileWorldEffect : MonoBehaviour
    {
        [Header("Runtime State")]
        public PlantGrowth sourcePlant;
        public AnimalController target;
        public float baseDamage;
        public float speed = 8f;
        public float effectMultiplier = 1f;
        public List<RuntimeGeneInstance> payloadInstances = new List<RuntimeGeneInstance>();

        private SpriteRenderer spriteRenderer;
        private bool hasResolved;
        private Vector3 lastKnownTargetPos;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        /// <summary>
        /// Initialize the projectile. Call after instantiation.
        /// </summary>
        public void Initialize(PlantGrowth source, AnimalController targetAnimal, float damage, float projectileSpeed, List<RuntimeGeneInstance> payloads, float multiplier = 1f)
        {
            sourcePlant = source;
            target = targetAnimal;
            baseDamage = damage;
            speed = projectileSpeed;
            effectMultiplier = multiplier;
            payloadInstances = payloads != null ? new List<RuntimeGeneInstance>(payloads) : new List<RuntimeGeneInstance>();
            hasResolved = false;

            if (target != null)
            {
                lastKnownTargetPos = target.transform.position;
            }

            // Tint based on primary payload color
            if (spriteRenderer != null && payloadInstances.Count > 0)
            {
                var primaryPayload = payloadInstances[0]?.GetGene<PayloadGene>();
                if (primaryPayload != null)
                {
                    spriteRenderer.color = primaryPayload.geneColor;
                }
            }
        }

        private void Update()
        {
            if (hasResolved) return;

            // If target died mid-flight, self-destruct
            if (target == null || target.IsDying)
            {
                Destroy(gameObject);
                return;
            }

            lastKnownTargetPos = target.transform.position;

            // Move towards target
            Vector3 direction = (lastKnownTargetPos - transform.position);
            float distanceThisFrame = speed * Time.deltaTime;

            if (direction.magnitude <= distanceThisFrame + 0.1f)
            {
                // Arrived at target — resolve hit
                OnHitTarget();
                return;
            }

            transform.position += direction.normalized * distanceThisFrame;

            // Rotate to face movement direction
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }

        private void OnHitTarget()
        {
            if (hasResolved) return;
            hasResolved = true;

            if (target != null && !target.IsDying)
            {
                // Apply base damage (goes through AnimalController which applies resistance)
                float finalDamage = baseDamage * effectMultiplier;
                target.TakeDamage(finalDamage);

                Debug.Log($"[Projectile] Hit {target.SpeciesName} for {finalDamage:F1} damage");

                // Apply payloads
                if (payloadInstances != null)
                {
                    foreach (var instance in payloadInstances)
                    {
                        if (instance == null) continue;

                        var payloadGene = instance.GetGene<PayloadGene>();
                        if (payloadGene == null) continue;

                        var context = new PayloadContext
                        {
                            target = target.gameObject,
                            source = sourcePlant,
                            payloadInstance = instance,
                            effectMultiplier = effectMultiplier,
                            parentGene = null
                        };

                        try
                        {
                            payloadGene.ApplyPayload(context);
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError($"[Projectile] Error applying payload {payloadGene.geneName}: {e.Message}");
                        }
                    }
                }
            }

            Destroy(gameObject);
        }
    }
}
