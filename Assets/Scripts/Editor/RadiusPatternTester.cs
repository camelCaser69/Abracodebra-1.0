using UnityEngine;
using WegoSystem;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(RadiusPatternTester))]
public class RadiusPatternTesterEditor : Editor {
    public override void OnInspectorGUI() {
        DrawDefaultInspector();
        
        RadiusPatternTester tester = (RadiusPatternTester)target;
        
        if (GUILayout.Button("Test Radius Pattern")) {
            tester.TestRadiusPattern();
        }
        
        if (GUILayout.Button("Compare All Patterns")) {
            tester.CompareAllPatterns();
        }
    }
}
#endif

public class RadiusPatternTester : MonoBehaviour {
    [Range(1, 10)]
    public int testRadius = 3;
    
    public void TestRadiusPattern() {
        GridRadiusUtility.DebugPrintRadius(GridPosition.Zero, testRadius);
    }
    
    public void CompareAllPatterns() {
        Debug.Log("=== RADIUS PATTERN COMPARISON ===");
        
        for (int r = 1; r <= 5; r++) {
            Debug.Log($"\n--- Radius {r} ---");
            
            // Circle pattern
            var circleTiles = GridRadiusUtility.GetTilesInCircle(GridPosition.Zero, r);
            Debug.Log($"Circle tiles: {circleTiles.Count}");
            
            // Show the pattern
            GridRadiusUtility.DebugPrintRadius(GridPosition.Zero, r);
            
            // Compare with Manhattan and Chebyshev
            int manhattanCount = 0;
            int chebyshevCount = 0;
            
            for (int x = -r; x <= r; x++) {
                for (int y = -r; y <= r; y++) {
                    if (Mathf.Abs(x) + Mathf.Abs(y) <= r) manhattanCount++;
                    if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) <= r) chebyshevCount++;
                }
            }
            
            Debug.Log($"Manhattan (diamond): {manhattanCount} tiles");
            Debug.Log($"Chebyshev (square): {chebyshevCount} tiles");
            Debug.Log($"Circle (ours): {circleTiles.Count} tiles");
        }
    }
}