// FILE: Assets/Editor/FixCircularReferences.cs
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Scans ScriptableObjects, prefabs and currently-open scenes for
/// NodeData circular references / stray stored sequences and cleans them up.
/// </summary>
public class FixCircularReferences : EditorWindow
{
    private const int MaxRecursionDepth = 10;

    [MenuItem("Tools/Fix Node Circular References")]
    private static void ShowWindow() => GetWindow<FixCircularReferences>("Fix Circular Refs");

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Fix Node Circular References", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (GUILayout.Button("Fix All NodeData in Project", GUILayout.Height(30)))
            FixAllNodeData();

        EditorGUILayout.Space();

        if (GUILayout.Button("Fix All Prefabs", GUILayout.Height(30)))
            FixAllPrefabs();

        EditorGUILayout.Space();

        if (GUILayout.Button("Fix Scene Objects", GUILayout.Height(30)))
            FixSceneObjects();
    }

    #region  Project-wide ScriptableObjects --------------------------------------------------

    private static void FixAllNodeData()
    {
        int fixedCount = 0;

        foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

            if (so is NodeDefinition nodeDef && FixNodeDefinition(nodeDef))
            {
                EditorUtility.SetDirty(nodeDef);
                fixedCount++;
            }
        }

        if (fixedCount > 0)
            AssetDatabase.SaveAssets();

        Debug.Log($"[FixCircularReferences] Fixed {fixedCount} NodeDefinition assets");
    }

    private static bool FixNodeDefinition(NodeDefinition def)
    {
        if (def?.effects == null) return false;

        bool changed = false;

        // Example: strip any hidden NodeData refs out of effect assets
        foreach (var effect in def.effects)
        {
            if (effect == null) continue;

            var f = effect.GetType().GetField("nodeData",
                                              System.Reflection.BindingFlags.Instance |
                                              System.Reflection.BindingFlags.NonPublic |
                                              System.Reflection.BindingFlags.Public);
            if (f != null && f.GetValue(effect) != null)
            {
                f.SetValue(effect, null);
                changed = true;
            }
        }

        return changed;
    }

    #endregion

    #region  Prefabs -------------------------------------------------------------------------

    private static void FixAllPrefabs()
    {
        int fixedCount = 0;

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            bool changed = false;

            foreach (var nv in prefab.GetComponentsInChildren<NodeView>(true))
                changed |= CleanNodeView(nv);

            foreach (var tv in prefab.GetComponentsInChildren<ToolView>(true))
                changed |= CleanToolView(tv);

            if (changed)
            {
                PrefabUtility.SavePrefabAsset(prefab);
                fixedCount++;
            }
        }

        Debug.Log($"[FixCircularReferences] Fixed {fixedCount} prefabs");
    }

    #endregion

    #region  Scene objects -------------------------------------------------------------------

    private static void FixSceneObjects()
    {
        int fixedCount = 0;

        foreach (var nv in FindObjectsOfType<NodeView>(true))
            if (CleanNodeView(nv)) { EditorUtility.SetDirty(nv); fixedCount++; }

        foreach (var tv in FindObjectsOfType<ToolView>(true))
            if (CleanToolView(tv)) { EditorUtility.SetDirty(tv); fixedCount++; }

        foreach (var pg in FindObjectsOfType<PlantGrowth>(true))
            if (CleanPlantGrowth(pg)) { EditorUtility.SetDirty(pg); fixedCount++; }

        Debug.Log($"[FixCircularReferences] Fixed {fixedCount} scene objects");
    }

    #endregion

    #region  Cleaning helpers ----------------------------------------------------------------

    private static bool CleanNodeView(NodeView view)
    {
        if (view == null) return false;

        var field = typeof(NodeView).GetField("_nodeData",
                     System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (field?.GetValue(view) is not NodeData data) return false;

        CleanNodeDataRecursive(data, 0);
        return true;
    }

    private static bool CleanToolView(ToolView view)
    {
        if (view == null) return false;

        var field = typeof(ToolView).GetField("_nodeData",
                     System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (field?.GetValue(view) is not NodeData data) return false;

        if (data.storedSequence != null)
        {
            data.storedSequence = null;
            return true;
        }
        return false;
    }

    private static bool CleanPlantGrowth(PlantGrowth growth)
    {
        if (growth == null) return false;

        var field = typeof(PlantGrowth).GetField("nodeGraph",
                     System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (field?.GetValue(growth) is not NodeGraph graph || graph.nodes == null) return false;

        foreach (var n in graph.nodes)
            if (n != null) CleanNodeDataRecursive(n, 0);

        return true;
    }

    private static void CleanNodeDataRecursive(NodeData nodeData, int depth)
    {
        if (nodeData == null || depth > MaxRecursionDepth) return;

        // Non-seed nodes must never keep a sequence
        if (!nodeData.IsSeed())
        {
            nodeData.storedSequence = null;
            return;
        }

        // Seed nodes: clean child nodes in their sequence
        if (nodeData.storedSequence?.nodes == null) return;

        foreach (var inner in nodeData.storedSequence.nodes)
            if (inner != null) inner.storedSequence = null;
    }

    #endregion
}
#endif
