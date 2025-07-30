// Assets/Scripts/Ecosystem/UI/ThoughtBubbleController.cs

using UnityEngine;
using TMPro;
using WegoSystem;

public class ThoughtBubbleController : MonoBehaviour
{
    public TMP_Text messageText;

    float lifetimeTicks;
    Transform followTarget;
    TickManager _tickManagerInstance; // Cached instance

    void Start()
    {
        _tickManagerInstance = TickManager.Instance;
        if (_tickManagerInstance == null)
        {
            Debug.LogWarning($"[{GetType().Name}] TickManager not found! Lifetime will use a fallback duration.", this);
        }
    }
	
    public void Initialize(string message, Transform target, float durationInTicks)
    {
        if (messageText != null)
            messageText.text = message;
		
        followTarget = target;
        lifetimeTicks = durationInTicks;
    }

    void Update()
    {
        if (_tickManagerInstance?.Config != null)
        {
            lifetimeTicks -= _tickManagerInstance.Config.ticksPerRealSecond * Time.deltaTime;
        }
        else
        {
            // Fallback if TickManager is not available
            lifetimeTicks -= 2f * Time.deltaTime;
        }

        if (lifetimeTicks <= 0f)
            Destroy(gameObject);

        if (followTarget != null)
        {
            transform.position = followTarget.position;
        }
    }
}