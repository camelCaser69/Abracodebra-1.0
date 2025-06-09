using UnityEngine;
using WegoSystem;

public class WegoDebugUI : MonoBehaviour {
    void OnGUI() {
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        
        GUILayout.Label($"Phase: {TurnPhaseManager.Instance?.CurrentPhase}");
        GUILayout.Label($"Tick: {TickManager.Instance?.CurrentTick}");
        
        var gardener = FindObjectOfType<GardenerController>();
        if (gardener != null) {
            GUILayout.Label($"Player Queue: {gardener.GetQueuedMoveCount()} moves");
            GUILayout.Label($"Grid Pos: {gardener.GetCurrentGridPosition()}");
        }
        
        if (TurnPhaseManager.Instance?.IsInPlanningPhase == true) {
            if (GUILayout.Button("End Planning Phase")) {
                TurnPhaseManager.Instance.EndPlanningPhase();
            }
        }
        
        if (GUILayout.Button("Force Planning Phase")) {
            TurnPhaseManager.Instance?.ForcePhase(TurnPhase.Planning);
        }
        
        if (GUILayout.Button("Clear Move Queue")) {
            gardener?.ClearQueuedMoves();
        }
        
        GUILayout.EndArea();
    }
}