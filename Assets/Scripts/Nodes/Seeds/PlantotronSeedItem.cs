// FILE: Assets/Scripts/Nodes/Seeds/PlantotronSeedItem.cs (FIXED)
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class PlantotronSeedItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("UI References")]
    public TMP_Text seedNameText;
    public TMP_Text seedStatusText;
    public Image seedIcon;
    public Button selectButton;
    public Button deleteButton;
    public Image backgroundImage;
    
    [Header("Visual Settings")]
    public Color normalColor = Color.white;
    public Color hoverColor = Color.yellow;
    public Color selectedColor = Color.cyan;
    public Color modifiedColor = Color.cyan;
    public Color vanillaColor = Color.green;
    
    private SeedInstance seed;
    private PlantotronUI parentUI;
    
    public void Initialize(SeedInstance seedInstance, PlantotronUI ui)
    {
        seed = seedInstance;
        parentUI = ui;
        
        if (seed == null || parentUI == null)
        {
            Debug.LogError("[PlantotronSeedItem] Invalid initialization parameters!");
            return;
        }
        
        Debug.Log($"[PlantotronSeedItem] Initializing seed item for: {seed.seedName}");
        
        // Setup UI elements
        if (seedNameText != null)
            seedNameText.text = seed.seedName;
            
        if (seedStatusText != null)
        {
            string status = seed.isModified ? "Modified" : "Vanilla";
            int geneCount = seed.currentGenes?.Count ?? 0;
            seedStatusText.text = $"{status} • {geneCount} genes";
            seedStatusText.color = seed.isModified ? modifiedColor : vanillaColor;
        }
        
        if (seedIcon != null && seed.baseSeedDefinition != null)
        {
            seedIcon.sprite = seed.baseSeedDefinition.icon;
        }
        
        // Setup buttons
        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(OnSelectButtonClicked);
            Debug.Log($"[PlantotronSeedItem] Select button configured for {seed.seedName}");
        }
        else
        {
            Debug.LogWarning($"[PlantotronSeedItem] Select button is null for {seed.seedName}!");
        }
        
        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(OnDeleteButtonClicked);
        }
        
        // Set initial background color
        if (backgroundImage != null)
            backgroundImage.color = normalColor;
        
        Debug.Log($"[PlantotronSeedItem] Successfully initialized: {seed.seedName}");
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (backgroundImage != null)
            backgroundImage.color = hoverColor;
            
        if (parentUI != null && seed != null)
            parentUI.ShowSeedDetails(seed);
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        if (backgroundImage != null)
            backgroundImage.color = normalColor;
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"[PlantotronSeedItem] OnPointerClick fired for {seed?.seedName ?? "null seed"}");
        
        if (parentUI != null && seed != null)
        {
            Debug.Log($"[PlantotronSeedItem] Calling OnSeedSelected for {seed.seedName}");
            parentUI.OnSeedSelected(seed);
        }
        else
        {
            Debug.LogError($"[PlantotronSeedItem] OnPointerClick failed - parentUI: {parentUI != null}, seed: {seed != null}");
        }
    }
    
    private void OnSelectButtonClicked()
    {
        Debug.Log($"[PlantotronSeedItem] Select button clicked for {seed?.seedName ?? "null seed"}");
        
        if (parentUI != null && seed != null)
        {
            Debug.Log($"[PlantotronSeedItem] Calling OnSeedSelected from button for {seed.seedName}");
            parentUI.OnSeedSelected(seed);
        }
        else
        {
            Debug.LogError($"[PlantotronSeedItem] Button click failed - parentUI: {parentUI != null}, seed: {seed != null}");
        }
    }
    
    private void OnDeleteButtonClicked()
    {
        Debug.Log($"[PlantotronSeedItem] Delete button clicked for {seed?.seedName ?? "null seed"}");
        
        if (seed != null && PlayerGeneticsInventory.Instance != null)
        {
            PlayerGeneticsInventory.Instance.RemoveSeed(seed);
        }
    }
    
    public void SetSelected(bool isSelected)
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = isSelected ? selectedColor : normalColor;
        }
    }
    
    void OnDestroy()
    {
        if (selectButton != null)
            selectButton.onClick.RemoveAllListeners();
        if (deleteButton != null)
            deleteButton.onClick.RemoveAllListeners();
    }
}