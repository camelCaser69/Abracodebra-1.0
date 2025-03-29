using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using TMPro;

public class NodeEditorController : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References")]
    // Instead of Horizontal Layout Group, the panel now has a GridLayoutGroup.
    public RectTransform slotPanel;        
    public GameObject nodeSlotPrefab;      

    [Header("Dropdown (using TMP_Dropdown)")]
    public TMP_Dropdown nodeDropdown;  // Ensure this TMP_Dropdown is set inactive by default.

    [Header("Node Data")]
    public NodeDefinitionLibrary definitionLibrary;  
    public NodeGraph currentGraph;
    
    [Header("Execution")]
    public NodeExecutor nodeExecutor;  // Assign in the inspector.

    private List<NodeView> nodeViews = new List<NodeView>();
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        if (currentGraph == null)
            currentGraph = new NodeGraph();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (nodeDropdown != null)
            nodeDropdown.gameObject.SetActive(false);
    }

    private void Start()
    {
        // Hide the node editor initially.
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        // >>> ADD THIS <<<
        // Make sure NodeExecutor uses the same NodeGraph.
        if (nodeExecutor != null)
            nodeExecutor.SetGraph(currentGraph);
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
            ToggleVisibility();

        // Only hide the dropdown on a left-click if the pointer is NOT over the dropdown.
        if (Input.GetMouseButtonDown(0) && nodeDropdown.gameObject.activeSelf)
        {
            if (!IsPointerOverDropdown())
            {
                HideDropdown();
            }
        }

        // Delete node when DELETE key is pressed on a selected node.
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            if (NodeSelectable.CurrentSelected != null)
                DeleteSelectedNode();
        }
    }

    // IPointerClickHandler: On right-click on the panel, show the dropdown.
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            ShowDropdown(eventData.position);
        }
    }

    public void ToggleVisibility()
    {
        if (canvasGroup.alpha > 0)
        {
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            HideDropdown();
        }
        else
        {
            canvasGroup.alpha = 1;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }

    private void ShowDropdown(Vector2 screenPos)
    {
        if (nodeDropdown == null || definitionLibrary == null) return;

        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        options.Add(new TMP_Dropdown.OptionData("Select Node")); // Default option.
        foreach (var def in definitionLibrary.definitions)
        {
            options.Add(new TMP_Dropdown.OptionData(def.displayName));
        }
        nodeDropdown.ClearOptions();
        nodeDropdown.AddOptions(options);
        nodeDropdown.value = 0;
        nodeDropdown.RefreshShownValue();

        RectTransform dropdownRect = nodeDropdown.GetComponent<RectTransform>();
        dropdownRect.position = screenPos;

        nodeDropdown.gameObject.SetActive(true);
        nodeDropdown.onValueChanged.RemoveAllListeners();
        nodeDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
    }

    private void HideDropdown()
    {
        if (nodeDropdown != null)
            nodeDropdown.gameObject.SetActive(false);
    }

    // Helper to check if the pointer is over the dropdown or its children.
    private bool IsPointerOverDropdown()
    {
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = Input.mousePosition;
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        foreach (var result in results)
        {
            if (result.gameObject == nodeDropdown.gameObject || result.gameObject.transform.IsChildOf(nodeDropdown.transform))
                return true;
        }
        return false;
    }

    private void OnDropdownValueChanged(int value)
    {
        if (value == 0) return;
        int index = value - 1;
        if (index >= 0 && index < definitionLibrary.definitions.Count)
        {
            AddNode(definitionLibrary.definitions[index]);
        }
        HideDropdown();
    }

    public void AddNode(NodeDefinition def)
    {
        NodeData newNode = new NodeData();
        newNode.nodeDisplayName = def.displayName;
        newNode.effects = new List<NodeEffectData>();
        foreach (var effect in def.effects)
        {
            NodeEffectData newEffect = new NodeEffectData
            {
                effectType = effect.effectType,
                primaryValue = effect.primaryValue,
                secondaryValue = effect.secondaryValue
            };
            newNode.effects.Add(newEffect);
        }
        newNode.orderIndex = currentGraph.nodes.Count;
        currentGraph.nodes.Add(newNode);

        GameObject nodeObj = Instantiate(nodeSlotPrefab, slotPanel);
        NodeView nodeView = nodeObj.GetComponent<NodeView>();
        if (nodeView != null)
        {
            nodeView.Initialize(newNode, def.thumbnail, def.backgroundColor, def.description, def.effects);
            nodeViews.Add(nodeView);
        }

        // >>> ADD THIS <<<
        // Ensure NodeExecutor sees the updated graph with the new node.
        if (nodeExecutor != null)
            nodeExecutor.SetGraph(currentGraph);
    }

    public void DeleteSelectedNode()
    {
        NodeView selectedView = NodeSelectable.CurrentSelected?.GetComponent<NodeView>();
        if (selectedView != null)
        {
            string nodeId = selectedView.GetNodeData().nodeId;
            currentGraph.nodes.RemoveAll(n => n.nodeId == nodeId);
            nodeViews.Remove(selectedView);
            Destroy(selectedView.gameObject);
            NodeSelectable.CurrentSelected = null;
        }
    }

    // Called by NodeDraggable on drag end to reorder nodes based on their horizontal positions.
    public void ReorderNodes()
    {
        List<RectTransform> children = new List<RectTransform>();
        foreach (RectTransform child in slotPanel)
            children.Add(child);
        children.Sort((a, b) => a.anchoredPosition.x.CompareTo(b.anchoredPosition.x));

        for (int i = 0; i < children.Count; i++)
        {
            children[i].SetSiblingIndex(i);
            NodeView nv = children[i].GetComponent<NodeView>();
            if (nv != null)
            {
                nv.GetNodeData().orderIndex = i;
            }
        }
    }
}
