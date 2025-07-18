// Assets/Scripts/WorldInteraction/Player/PlayerActionManager.cs
using System;
using System.Collections;
using UnityEngine;
using WegoSystem;

public enum PlayerActionType
{
    Move,
    UseTool,
    PlantSeed,
    Harvest,
    Water,
    Interact
}

public class PlayerActionManager : MonoBehaviour
{
    public static PlayerActionManager Instance { get; set; }
    
    // <<< NEW: A simple class to bundle tool action data for the event
    public class ToolActionData
    {
        public ToolDefinition Tool;
        public Vector3Int GridPosition;
    }

    [SerializeField] bool debugMode = true;
    [SerializeField] int tickCostPerAction = 1;
    [SerializeField] float multiTickActionDelay = 0.5f;

    // <<< MODIFIED: The event now passes a generic 'object' payload
    public event Action<PlayerActionType, object> OnActionExecuted; 
    public event Action<string> OnActionFailed;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
    
    // This is the only method that needed a significant change
    public bool ExecutePlayerAction(PlayerActionType actionType, Vector3Int gridPosition, object actionData = null, Action onSuccessCallback = null)
    {
        if (debugMode) Debug.Log($"[PlayerActionManager] Executing {actionType} at {gridPosition}");

        bool success = false;
        int tickCost = tickCostPerAction;
        object eventPayload = actionData; // The data to be sent in the event

        switch (actionType)
        {
            case PlayerActionType.UseTool:
                var toolDef = actionData as ToolDefinition;
                success = ExecuteToolUse(gridPosition, toolDef);
                // <<< NEW: Create the data payload for the event
                if (success) 
                {
                    eventPayload = new ToolActionData { Tool = toolDef, GridPosition = gridPosition };
                }
                break;

            case PlayerActionType.PlantSeed:
                tickCost = 2;
                if (tickCost > 1)
                {
                    StartCoroutine(ExecuteDelayedAction(() => ExecutePlantSeed(gridPosition, actionData as InventoryBarItem), tickCost, onSuccessCallback, actionType, actionData));
                    return true;
                }
                else
                {
                    success = ExecutePlantSeed(gridPosition, actionData as InventoryBarItem);
                }
                break;
            // ... other cases remain the same
            case PlayerActionType.Water: success = ExecuteWatering(gridPosition); break;
            case PlayerActionType.Harvest: success = ExecuteHarvest(gridPosition); break;
            case PlayerActionType.Interact: success = ExecuteInteraction(gridPosition, actionData); break;
        }

        if (success)
        {
            AdvanceGameTick(tickCost);
            onSuccessCallback?.Invoke();
            // Fire the event with the correct payload
            OnActionExecuted?.Invoke(actionType, eventPayload);
        }
        else
        {
            OnActionFailed?.Invoke($"{actionType} failed");
        }
        return success;
    }
    
    // Modified to pass event data through
    IEnumerator ExecuteDelayedAction(Func<bool> action, int tickCost, Action onSuccessCallback, PlayerActionType actionType, object actionData)
    {
        for (int i = 0; i < tickCost - 1; i++)
        {
            TickManager.Instance.AdvanceTick();
            yield return new WaitForSeconds(multiTickActionDelay);
        }
        bool success = action.Invoke();
        TickManager.Instance.AdvanceTick();
        if (success)
        {
            onSuccessCallback?.Invoke();
            OnActionExecuted?.Invoke(actionType, actionData);
        }
        else
        {
            OnActionFailed?.Invoke("Delayed action failed");
        }
    }

    // The rest of the script is unchanged, but included for completeness.
    public int GetMovementTickCost(Vector3 worldPosition,Component movingEntity=null){int totalCost=tickCostPerAction;int statusEffectCost=0;if(movingEntity!=null){IStatusEffectable effectable=movingEntity.GetComponent<IStatusEffectable>();if(effectable!=null){statusEffectCost=effectable.StatusManager.AdditionalMoveTicks;}}
    totalCost+=statusEffectCost;if(debugMode&&totalCost>tickCostPerAction){string entityName=movingEntity!=null?movingEntity.gameObject.name:"Unknown Entity";Debug.Log($"[PlayerActionManager] Movement for '{entityName}' cost breakdown: Base({tickCostPerAction}) + Status({statusEffectCost}) = {totalCost} ticks total.");}
    return totalCost;}
    bool ExecuteToolUse(Vector3Int gridPosition,ToolDefinition tool){if(tool==null)return false;TileInteractionManager.Instance?.ApplyToolAction(tool);return true;}
    bool ExecutePlantSeed(Vector3Int gridPosition,InventoryBarItem seedItem){if(seedItem==null||!seedItem.IsSeed())return false;return PlantPlacementManager.Instance?.TryPlantSeedFromInventory(seedItem,gridPosition,TileInteractionManager.Instance.interactionGrid.GetCellCenterWorld(gridPosition))??false;}
    bool ExecuteWatering(Vector3Int gridPosition){if(debugMode)Debug.Log($"[PlayerActionManager] Watering at {gridPosition} - NOT IMPLEMENTED");return false;}
    bool ExecuteHarvest(Vector3Int gridPosition){if(debugMode)Debug.Log($"[PlayerActionManager] Harvesting at {gridPosition} - NOT IMPLEMENTED");return false;}
    bool ExecuteInteraction(Vector3Int gridPosition,object interactionData){if(debugMode)Debug.Log($"[PlayerActionManager] Interaction at {gridPosition}");return true;}
    void AdvanceGameTick(int tickCount=1){if(TickManager.Instance==null){Debug.LogError("[PlayerActionManager] TickManager not found!");return;}
    for(int i=0;i<tickCount;i++){TickManager.Instance.AdvanceTick();}
    if(debugMode){Debug.Log($"[PlayerActionManager] Advanced game by {tickCount} tick(s)");}}
    public bool CanExecuteAction(PlayerActionType actionType,Vector3Int gridPosition,object actionData=null){switch(actionType){case PlayerActionType.Move:return false;case PlayerActionType.UseTool:return actionData is ToolDefinition;case PlayerActionType.PlantSeed:var seedItem=actionData as InventoryBarItem;return seedItem!=null&&seedItem.IsSeed();default:return true;}}
}