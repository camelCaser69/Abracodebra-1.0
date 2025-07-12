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

    [SerializeField] bool debugMode = true;
    [SerializeField] int tickCostPerAction = 1;

    [SerializeField] float multiTickActionDelay = 0.5f;

    public event Action<PlayerActionType, bool> OnActionExecuted;
    public event Action<string> OnActionFailed;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
    
    // This is the key updated method
    public int GetMovementTickCost(Vector3 worldPosition, Component movingEntity = null)
    {
        int totalCost = tickCostPerAction;
        int zoneCost = 0;
        int statusEffectCost = 0;

        // --- Check for slowdown zones on the tile ---
        SlowdownZone[] allZones = FindObjectsByType<SlowdownZone>(FindObjectsSortMode.None);
        foreach (var zone in allZones)
        {
            if (zone.IsPositionInZone(worldPosition))
            {
                bool shouldAffect = false;
                if (movingEntity is GardenerController && zone.AffectsPlayer) shouldAffect = true;
                else if (movingEntity is AnimalController && zone.AffectsAnimals) shouldAffect = true;
                else if (movingEntity == null) shouldAffect = true;

                if (shouldAffect)
                {
                    zoneCost = Mathf.Max(zoneCost, zone.GetAdditionalTickCost());
                }
            }
        }
        
        // <<< NEW: Check for status effect penalties on the entity
        if (movingEntity != null)
        {
            IStatusEffectable effectable = movingEntity.GetComponent<IStatusEffectable>();
            if (effectable != null)
            {
                statusEffectCost = effectable.StatusManager.AdditionalMoveTicks;
            }
        }

        totalCost += zoneCost + statusEffectCost;

        if (debugMode && totalCost > tickCostPerAction)
        {
            string entityName = movingEntity != null ? movingEntity.gameObject.name : "Unknown Entity";
            Debug.Log($"[PlayerActionManager] Movement for '{entityName}' cost breakdown: Base({tickCostPerAction}) + Zone({zoneCost}) + Status({statusEffectCost}) = {totalCost} ticks total.");
        }

        return totalCost;
    }

    // --- The rest of the script is unchanged ---
    public bool ExecutePlayerAction(PlayerActionType actionType,Vector3Int gridPosition,object actionData=null,Action onSuccessCallback=null){if(debugMode)Debug.Log($"[PlayerActionManager] Executing {actionType} at {gridPosition}");bool success=false;int tickCost=tickCostPerAction;switch(actionType){case PlayerActionType.UseTool:success=ExecuteToolUse(gridPosition,actionData as ToolDefinition);break;case PlayerActionType.PlantSeed:tickCost=2;if(tickCost>1){StartCoroutine(ExecuteDelayedAction(()=>ExecutePlantSeed(gridPosition,actionData as InventoryBarItem),tickCost,onSuccessCallback));return true;}
    else{success=ExecutePlantSeed(gridPosition,actionData as InventoryBarItem);}
    break;case PlayerActionType.Water:success=ExecuteWatering(gridPosition);break;case PlayerActionType.Harvest:success=ExecuteHarvest(gridPosition);break;case PlayerActionType.Interact:success=ExecuteInteraction(gridPosition,actionData);break;}
    if(success){AdvanceGameTick(tickCost);onSuccessCallback?.Invoke();OnActionExecuted?.Invoke(actionType,true);}
    else{OnActionFailed?.Invoke($"{actionType} failed");}
    return success;}
    IEnumerator ExecuteDelayedAction(Func<bool> action,int tickCost,Action onSuccessCallback){Debug.Log($"[PlayerActionManager] Starting delayed action. Cost: {tickCost} ticks");for(int i=0;i<tickCost-1;i++){Debug.Log($"[PlayerActionManager] Processing tick {i+1}/{tickCost} (delay tick)");TickManager.Instance.AdvanceTick();yield return new WaitForSeconds(multiTickActionDelay);}
    bool success=action.Invoke();TickManager.Instance.AdvanceTick();if(success){onSuccessCallback?.Invoke();OnActionExecuted?.Invoke(PlayerActionType.PlantSeed,true);Debug.Log($"[PlayerActionManager] Completed delayed action. Total ticks: {tickCost}");}
    else{OnActionFailed?.Invoke("Delayed action failed");}}
    bool ValidateMovement(GridPosition from,GridPosition to){int distance=from.ManhattanDistance(to);if(distance!=1){if(debugMode)Debug.Log($"[PlayerActionManager] Movement validation failed: distance {distance} != 1");return false;}
    if(!GridPositionManager.Instance.IsPositionValid(to)){if(debugMode)Debug.Log($"[PlayerActionManager] Movement validation failed: position {to} out of bounds");return false;}
    if(GridPositionManager.Instance.IsPositionOccupied(to)){if(debugMode)Debug.Log($"[PlayerActionManager] Movement validation failed: position {to} occupied");return false;}
    return true;}
    bool ExecuteImmediateMove(GardenerController gardener,GridPosition to){var gridEntity=gardener.GetComponent<GridEntity>();if(gridEntity==null)return false;gridEntity.SetPosition(to);return true;}
    bool ExecuteToolUse(Vector3Int gridPosition,ToolDefinition tool){if(tool==null)return false;TileInteractionManager.Instance?.ApplyToolAction(tool);return true;}
    bool ExecutePlantSeed(Vector3Int gridPosition,InventoryBarItem seedItem){if(seedItem==null||!seedItem.IsSeed())return false;return PlantPlacementManager.Instance?.TryPlantSeedFromInventory(seedItem,gridPosition,TileInteractionManager.Instance.interactionGrid.GetCellCenterWorld(gridPosition))??false;}
    bool ExecuteWatering(Vector3Int gridPosition){if(debugMode)Debug.Log($"[PlayerActionManager] Watering at {gridPosition} - NOT IMPLEMENTED");return false;}
    bool ExecuteHarvest(Vector3Int gridPosition){if(debugMode)Debug.Log($"[PlayerActionManager] Harvesting at {gridPosition} - NOT IMPLEMENTED");return false;}
    bool ExecuteInteraction(Vector3Int gridPosition,object interactionData){if(debugMode)Debug.Log($"[PlayerActionManager] Interaction at {gridPosition}");return true;}
    void AdvanceGameTick(int tickCount=1){if(TickManager.Instance==null){Debug.LogError("[PlayerActionManager] TickManager not found!");return;}
    for(int i=0;i<tickCount;i++){TickManager.Instance.AdvanceTick();if(debugMode&&tickCount>1){Debug.Log($"[PlayerActionManager] Processing tick {i+1}/{tickCount}");}}
    if(debugMode){Debug.Log($"[PlayerActionManager] Advanced game by {tickCount} tick(s)");}}
    public bool CanExecuteAction(PlayerActionType actionType,Vector3Int gridPosition,object actionData=null){switch(actionType){case PlayerActionType.Move:return false;case PlayerActionType.UseTool:return actionData is ToolDefinition;case PlayerActionType.PlantSeed:var seedItem=actionData as InventoryBarItem;return seedItem!=null&&seedItem.IsSeed();default:return true;}}
}