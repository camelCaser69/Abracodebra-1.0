// FILE: Assets/Scripts/Visual/Effects/FloatingCombatText.cs
// TASK 9: Spawns a floating TextMeshPro world-space label that rises and fades.
// Usage: FloatingCombatText.Spawn(worldPos, "-10", Color.red);

using System.Collections;
using UnityEngine;
using TMPro;

public class FloatingCombatText : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] float riseSpeed = 0.4f;     // units/second
    [SerializeField] float lifetime = 1.0f;       // seconds before destroyed
    [SerializeField] float fontSize = 2f;

    TextMeshPro tmp;
    float elapsed;
    Color startColor;

    // ─────────────────────────────────────────────────────────
    // Static spawn helper — call from anywhere
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Spawns a floating combat text label at the given world position.
    /// </summary>
    public static FloatingCombatText Spawn(Vector3 worldPosition, string text, Color color)
    {
        GameObject go = new GameObject("FloatingCombatText");
        go.transform.position = worldPosition;

        var fct = go.AddComponent<FloatingCombatText>();
        fct.Setup(text, color);
        return fct;
    }

    void Setup(string text, Color color)
    {
        startColor = color;

        // TextMeshPro in world space
        tmp = gameObject.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;

        // Render above everything else
        tmp.sortingOrder = 100;

        // Small random horizontal drift for variety
        float driftX = Random.Range(-0.15f, 0.15f);
        transform.position += new Vector3(driftX, 0f, 0f);

        Destroy(gameObject, lifetime + 0.05f);
    }

    void Update()
    {
        if (tmp == null) return;

        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / lifetime);

        // Rise
        transform.position += Vector3.up * riseSpeed * Time.deltaTime;

        // Fade out in the last 40% of lifetime
        float fadeStart = 0.6f;
        float alpha = t < fadeStart ? 1f : Mathf.Lerp(1f, 0f, (t - fadeStart) / (1f - fadeStart));
        tmp.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
    }
}
