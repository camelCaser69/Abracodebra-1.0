// File: Assets/Scripts/Genes/Services/GeneEventBus.cs
using System;
using System.Collections.Generic;
using Abracodabra.Genes.Core; // Assuming Gene classes will be in this namespace

namespace Abracodabra.Genes.Services
{
    public class GeneEventBus : IGeneEventBus
    {
        private Dictionary<Type, List<Delegate>> handlers = new Dictionary<Type, List<Delegate>>();

        public void Subscribe<T>(Action<T> handler) where T : class
        {
            var type = typeof(T);
            if (!handlers.ContainsKey(type))
                handlers[type] = new List<Delegate>();
            handlers[type].Add(handler);
        }

        public void Unsubscribe<T>(Action<T> handler) where T : class
        {
            var type = typeof(T);
            if (handlers.ContainsKey(type))
                handlers[type].Remove(handler);
        }

        public void Publish<T>(T message) where T : class
        {
            var type = typeof(T);
            if (handlers.TryGetValue(type, out var list))
            {
                // Create a copy to prevent issues with modification during iteration
                var handlersToInvoke = new List<Delegate>(list);
                foreach (Action<T> handler in handlersToInvoke)
                    handler?.Invoke(message);
            }
        }
    }

    #region Event Message Classes

    public class GeneExecutedEvent
    {
        public ActiveGene Gene { get; set; }
        public int SequencePosition { get; set; }
        public bool Success { get; set; }
        public float EnergyCost { get; set; }
    }

    public class SequenceCompletedEvent
    {
        public int TotalSlotsExecuted { get; set; }
        public float TotalEnergyUsed { get; set; }
    }

    public class GeneValidationFailedEvent
    {
        public string GeneId { get; set; }
        public string Reason { get; set; }
    }

    #endregion
}