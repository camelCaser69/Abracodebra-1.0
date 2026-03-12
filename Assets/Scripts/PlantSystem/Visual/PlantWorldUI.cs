// FILE: Assets/Scripts/PlantSystem/Visual/PlantWorldUI.cs
using UnityEngine;
using Abracodabra.Genes;

public class PlantWorldUI : MonoBehaviour {
    [Header("Bar Layout")]
    [SerializeField] float barWidth = 0.6f;
    [SerializeField] float barHeight = 0.06f;
    [SerializeField] float energyBarYOffset = 0.3f;
    [SerializeField] int sortingOrder = 50;

    static readonly Color EnergyFillColor = new Color(1f, 0.85f, 0f);        // yellow
    static readonly Color EnergyBgColor   = new Color(0.2f, 0.2f, 0.2f, 0.8f);

    PlantGrowth plant;

    Transform energyBarRoot;
    Transform energyFill;

    static Sprite _pixelSprite;

    public void Initialize(PlantGrowth target) {
        plant = target;
        EnsurePixelSprite();
        BuildBars();
    }

    void LateUpdate() {
        if (plant == null || plant.EnergySystem == null) return;
        if (plant.CurrentState == PlantState.Dead) {
            gameObject.SetActive(false);
            return;
        }

        UpdateEnergyBar();
    }

    void BuildBars() {
        energyBarRoot = BuildBar("EnergyBar", energyBarYOffset, EnergyBgColor, EnergyFillColor, out energyFill);
    }

    Transform BuildBar(string barName, float yOffset, Color bgColor, Color fillColor, out Transform fill) {
        GameObject root = new GameObject(barName);
        root.transform.SetParent(transform);
        root.transform.localPosition = new Vector3(-barWidth / 2f, yOffset, -0.1f);

        GameObject bg = new GameObject("BG");
        bg.transform.SetParent(root.transform, false);
        bg.transform.localPosition = new Vector3(barWidth / 2f, 0f, 0f);
        var bgSR = bg.AddComponent<SpriteRenderer>();
        bgSR.sprite = _pixelSprite;
        bgSR.color = bgColor;
        bgSR.transform.localScale = new Vector3(barWidth, barHeight, 1f);
        bgSR.sortingOrder = sortingOrder;

        GameObject fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(root.transform, false);
        fillGO.transform.localPosition = new Vector3(0f, 0f, -0.01f);
        var fillSR = fillGO.AddComponent<SpriteRenderer>();
        fillSR.sprite = _pixelSprite;
        fillSR.color = fillColor;
        fillGO.transform.localScale = new Vector3(barWidth, barHeight, 1f);
        fillSR.sortingOrder = sortingOrder + 1;

        GameObject pivot = new GameObject("Pivot");
        pivot.transform.SetParent(root.transform, false);
        pivot.transform.localPosition = Vector3.zero;
        fillGO.transform.SetParent(pivot.transform, true);
        fillGO.transform.localPosition = new Vector3(barWidth / 2f, 0f, -0.01f);

        fill = pivot.transform;
        return root.transform;
    }

    void UpdateEnergyBar() {
        if (energyFill == null) return;
        float t = plant.EnergySystem.MaxEnergy > 0
            ? Mathf.Clamp01(plant.EnergySystem.CurrentEnergy / plant.EnergySystem.MaxEnergy)
            : 0f;
        SetFillX(energyFill, t);
    }

    void SetFillX(Transform fillPivot, float t) {
        var fillGO = fillPivot.GetChild(0);
        if (fillGO != null) {
            Vector3 s = fillGO.localScale;
            s.x = barWidth * Mathf.Max(0.001f, t);
            fillGO.localScale = s;
            Vector3 pos = fillGO.localPosition;
            pos.x = s.x / 2f;
            fillGO.localPosition = pos;
        }
    }

    static void EnsurePixelSprite() {
        if (_pixelSprite != null) return;

        Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        _pixelSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }
}