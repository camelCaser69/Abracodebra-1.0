// File: Assets/Scripts/Genes/Components/Creature.cs
using UnityEngine;

namespace Abracodabra.Genes.Components
{
    /// <summary>
    /// A component for any entity that can eat or be healed, allowing it to be a target for payloads.
    /// You would add this to your Animal and Player prefabs.
    /// </summary>
    public class Creature : MonoBehaviour
    {
        public void Feed(float amount)
        {
            Debug.Log($"{name} was fed for {amount} nutrition points.", this);
            // In a real implementation, you would hook this into the Animal's hunger system.
            var animalNeeds = GetComponent<AnimalNeeds>();
            if (animalNeeds != null)
            {
                animalNeeds.ModifyHunger(-amount); // Negative amount to reduce hunger
            }
        }

        public void Heal(float amount)
        {
            Debug.Log($"{name} was healed for {amount} HP.", this);
            // In a real implementation, you would hook this into the Animal's health system.
            var animalNeeds = GetComponent<AnimalNeeds>();
            if (animalNeeds != null)
            {
                animalNeeds.Heal(amount);
            }
        }
    }
}