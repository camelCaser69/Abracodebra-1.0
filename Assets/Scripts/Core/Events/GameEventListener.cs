// Assets/Scripts/Core/Events/GameEventListener.cs
using UnityEngine;
using UnityEngine.Events;

public class GameEventListener : MonoBehaviour, IGameEventListener
{
    [Tooltip("The event to register with.")]
    [SerializeField] private GameEvent gameEvent;

    [Tooltip("The response to invoke when the event is raised.")]
    [SerializeField] private UnityEvent response;

    private void OnEnable()
    {
        if (gameEvent != null)
        {
            gameEvent.RegisterListener(this);
        }
    }

    private void OnDisable()
    {
        if (gameEvent != null)
        {
            gameEvent.UnregisterListener(this);
        }
    }

    public void OnEventRaised()
    {
        response.Invoke();
    }
}