// FILE: Assets/Scripts/PlantSystem/Visual/PlantWorldUI.cs
// TASK 9: World-space energy and HP bars above each plant.
// Auto-creates two layered SpriteRenderer bars (energy + HP).
// Add to PlantGrowth.InitializeWithState() — already wired there.

using UnityEngine;
using Abracodabra.Genes;

public class PlantWorldUI : MonoBehaviour
{
    // ── Bar geometry ──────────────────────────────────────────
    [Header("Bar Layout")]
    [SerializeField] float barWidth = 0.6f;
    [SerializeField] float barHeight = 0.06f;
    [SerializeField] float energyBarYOffset = 0.3f;   // above plant base
    [SerializeField] float hpBarYOffset = 0.22f;
    [SerializeField] int sortingOrder = 50;

    // ── Colors ────────────────────────────────────────────────
    static readonly Color EnergyFillColor = new Color(1f, 0.85f, 0f);        // yellow
    static readonly Color EnergyBgColor   = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    static readonly Color HpFillColor     = new Color(0.2f, 0.9f, 0.2f);     // green → red via lerp
    static readonly Color HpBgColor       = new Color(0.15f, 0.15f, 0.15f, 0.8f);
    static readonly Color HpLowColor      = new Color(1f, 0.15f, 0.15f);     // red when low HP

    PlantGrowth plant;

    // Bar roots
    Transform energyBarRoot;
    Transform hpBarRoot;

    // Fill transforms (we scale their x)
    Transform energyFill;
    Transform hpFill;
    SpriteRenderer hpFillRenderer;

    // One-pixel white sprite for all bar segments
    static Sprite _pixelSprite;

    // ─────────────────────────────────────────────────────────

    public void Initialize(PlantGrowth target)
    {
        plant = target;
        EnsurePixelSprite();
        BuildBars();
    }

    void LateUpdate()
    {
        if (plant == null || plant.EnergySystem == null) return;
        if (plant.CurrentState == PlantState.Dead)
        {
            gameObject.SetActive(false);
            return;
        }

        UpdateEnergyBar();
        UpdateHPBar();
    }

    // ─────────────────────────────────────────────────────────

    void BuildBars()
    {
        energyBarRoot = BuildBar("EnergyBar", energyBarYOffset, EnergyBgColor, EnergyFillColor, out energyFill, out _);
        hpBarRoot = BuildBar("HPBar", hpBarYOffset, HpBgColor, HpFillColor, out hpFill, out hpFillRenderer);
    }

    Transform BuildBar(string barName, float yOffset, Color bgColor, Color fillColor,
                       out Transform fill, out SpriteRenderer fillRenderer)
    {
        // Root
        GameObject root = new GameObject(barName);
        root.transform.SetParent(transform);
        root.transform.localPosition = new Vector3(-barWidth / 2f, yOffset, -0.1f);

        // Background
        GameObject bg = new GameObject("BG");
        bg.transform.SetParent(root.transform, false);
        bg.transform.localPosition = new Vector3(barWidth / 2f, 0f, 0f);
        var bgSR = bg.AddComponent<SpriteRenderer>();
        bgSR.sprite = _pixelSprite;
        bgSR.color = bgColor;
        bgSR.transform.localScale = new Vector3(barWidth, barHeight, 1f);
        bgSR.sortingOrder = sortingOrder;

        // Fill (anchored left, scaled on x)
        GameObject fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(root.transform, false);
        fillGO.transform.localPosition = new Vector3(0f, 0f, -0.01f);
        var fillSR = fillGO.AddComponent<SpriteRenderer>();
        fillSR.sprite = _pixelSprite;
        fillSR.color = fillColor;
        fillGO.transform.localScale = new Vector3(barWidth, barHeight, 1f);
        fillSR.sortingOrder = sortingOrder + 1;

        // Pivot fill to left edge by wrapping in a pivot GO
        GameObject pivot = new GameObject("Pivot");
        pivot.transform.SetParent(root.transform, false);
        pivot.transform.localPosition = Vector3.zero;
        fillGO.transform.SetParent(pivot.transform, true);
        // Move fill right by half width so it scales left-to-right
        fillGO.transform.localPosition = new Vector3(barWidth / 2f, 0f, -0.01f);

        fill = pivot.transform;
        fillRenderer = fillSR;
        return root.transform;
    }

    void UpdateEnergyBar()
    {
        if (energyFill == null) return;
        float t = plant.EnergySystem.MaxEnergy > 0
            ? Mathf.Clamp01(plant.EnergySystem.CurrentEnergy / plant.EnergySystem.MaxEnergy)
            : 0f;
        SetFillX(energyFill, t);
    }

    void UpdateHPBar()
    {
        if (hpFill == null) return;
        float t = plant.maxHP > 0 ? Mathf.Clamp01(plant.currentHP / plant.maxHP) : 0f;
        SetFillX(hpFill, t);

        // Color shifts from green to red as HP drops
        if (hpFillRenderer != null)
        {
            hpFillRenderer.color = Color.Lerp(HpLowColor, HpFillColor, t);
        }
    }

    void SetFillX(Transform fillPivot, float t)
    {
        // The fill child is offset by barWidth/2. Scale the pivot's x to mask the bar.
        // We scale the fill child's x; pivot is at 0, fill at barWidth/2 (world).
        // Simpler: just scale the fill GO (direct child of pivot) on x
        var fillGO = fillPivot.GetChild(0);
        if (fillGO != null)
        {
            Vector3 s = fillGO.localScale;
            s.x = barWidth * Mathf.Max(0.001f, t);
            fillGO.localScale = s;
            // Keep fill left-anchored: position = half of fill width
            Vector3 pos = fillGO.localPosition;
            pos.x = s.x / 2f;
            fillGO.localPosition = pos;
        }
    }

    // ─────────────────────────────────────────────────────────
    // Pixel sprite factory (shared)
    // ─────────────────────────────────────────────────────────

    static void EnsurePixelSprite()
    {
        if (_pixelSprite != null) return;

        Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        _pixelSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }
}
