using UnityEngine;
using TMPro;

public class ThoughtBubbleController : MonoBehaviour
{
    public TMP_Text messageText;
    public float lifetime = 2f;

    private Transform followTarget;

    public void Initialize(string message, Transform target, float duration = 2f)
    {
        if (messageText != null)
            messageText.text = message;
        followTarget = target;
        lifetime = duration;
    }

    private void Update()
    {
        lifetime -= Time.deltaTime;
        if (lifetime <= 0f)
            Destroy(gameObject);

        // Follow the target if needed (optional if already a child)
        if (followTarget != null)
        {
            transform.position = followTarget.position;
        }
    }
}