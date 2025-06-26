using UnityEngine;
using TMPro;
using WegoSystem;

public class ThoughtBubbleController : MonoBehaviour {
    public TMP_Text messageText;
    
    private float lifetimeTicks;
    private Transform followTarget;

    public void Initialize(string message, Transform target, float durationInTicks) {
        if (messageText != null)
            messageText.text = message;
        followTarget = target;
        lifetimeTicks = durationInTicks;
    }

    void Update() {
        // Convert real-time to tick-based countdown
        if (TickManager.Instance?.Config != null) {
            lifetimeTicks -= TickManager.Instance.Config.ticksPerRealSecond * Time.deltaTime;
        } else {
            // Fallback: assume 2 ticks per second
            lifetimeTicks -= 2f * Time.deltaTime;
        }

        if (lifetimeTicks <= 0f)
            Destroy(gameObject);

        if (followTarget != null) {
            transform.position = followTarget.position;
        }
    }
}