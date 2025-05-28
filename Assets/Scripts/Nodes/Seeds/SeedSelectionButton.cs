using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

// =====================================================================
// Seed Selection Button Component
// =====================================================================
public class SeedSelectionButton : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text seedNameText;
    public TMP_Text seedInfoText;
    public Image seedIcon;
    public Button selectButton;
    public Image backgroundImage;
    
    [Header("Visual Settings")]
    public Color normalColor = Color.white;
    public Color hoverColor = Color.yellow;
    public Color modifiedColor = Color.cyan;
    public Color vanillaColor = Color.green;
    
    private SeedInstance seed;
    private SeedSelectionUI parentUI;
    
    public void Initialize(SeedInstance seedInstance, SeedSelectionUI ui)
    {
        seed = seedInstance;
        parentUI = ui;
        
        if (seed == null || parentUI == null)
        {
            Debug.LogError("[SeedSelectionButton] Invalid initialization parameters!");
            return;
        }
        
        // Setup UI elements
        if (seedNameText != null)
            seedNameText.text = seed.seedName;
            
        if (seedInfoText != null)
        {
            string status = seed.isModified ? "Modified" : "Vanilla";
            int geneCount = seed.currentGenes?.Count ?? 0;
            seedInfoText.text = $"{status} • {geneCount} genes";
            seedInfoText.color = seed.isModified ? modifiedColor : vanillaColor;
        }
        
        if (seedIcon != null && seed.baseSeedDefinition != null)
        {
            seedIcon.sprite = seed.baseSeedDefinition.icon;
        }
        
        // Setup select button
        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(OnSelectButtonClicked);
        }
        
        // Set initial background color
        if (backgroundImage != null)
            backgroundImage.color = normalColor;
    }
    
    private void OnSelectButtonClicked()
    {
        if (parentUI != null && seed != null)
            parentUI.OnSeedSelected(seed);
    }
    
    void OnDestroy()
    {
        if (selectButton != null)
            selectButton.onClick.RemoveAllListeners();
    }
}