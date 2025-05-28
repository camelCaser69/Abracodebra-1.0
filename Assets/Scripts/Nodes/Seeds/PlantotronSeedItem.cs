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
        }
        
        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(OnDeleteButtonClicked);
        }
        
        // Set initial background color
        if (backgroundImage != null)
            backgroundImage.color = normalColor;
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
        if (parentUI != null && seed != null)
            parentUI.OnSeedSelected(seed);
    }
    
    private void OnSelectButtonClicked()
    {
        if (parentUI != null && seed != null)
            parentUI.OnSeedSelected(seed);
    }
    
    private void OnDeleteButtonClicked()
    {
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