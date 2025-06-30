using UnityEngine;
using UnityEditor;
using WegoSystem;

[CustomEditor(typeof(WaveManager))]
public class WaveManagerEditor : Editor {
    SerializedProperty waveDurationInDays;
    SerializedProperty spawnTimeNormalized;
    SerializedProperty continuousSpawning;
    SerializedProperty deletePreviousWaveAnimals;
    SerializedProperty wavesSequence;
    
    // Foldouts
    bool showTimingSettings = true;
    bool showWaveSequence = true;
    bool showDebugInfo = false;
    
    void OnEnable() {
        waveDurationInDays = serializedObject.FindProperty("waveDurationInDays");
        spawnTimeNormalized = serializedObject.FindProperty("spawnTimeNormalized");
        continuousSpawning = serializedObject.FindProperty("continuousSpawning");
        deletePreviousWaveAnimals = serializedObject.FindProperty("deletePreviousWaveAnimals");
        wavesSequence = serializedObject.FindProperty("wavesSequence");
    }
    
    public override void OnInspectorGUI() {
        serializedObject.Update();
        
        WaveManager waveManager = (WaveManager)target;
        
        // Header
        EditorGUILayout.LabelField("Wave Manager", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // Core References (keep default drawing)
        DrawPropertiesExcluding(serializedObject, 
            "waveDurationInDays", 
            "spawnTimeNormalized", 
            "continuousSpawning",
            "deletePreviousWaveAnimals",
            "wavesSequence"
        );
        
        EditorGUILayout.Space();
        
        // Wave Timing Section
        showTimingSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showTimingSettings, "Wave Timing Settings");
        if (showTimingSettings) {
            EditorGUI.indentLevel++;
            
            // Wave Duration
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(waveDurationInDays, new GUIContent("Wave Duration (Days)"));
            EditorGUILayout.EndHorizontal();
            
            // Show calculated ticks
            if (Application.isPlaying && TickManager.Instance?.Config != null) {
                var config = TickManager.Instance.Config;
                int totalTicks = config.ticksPerDay * waveDurationInDays.intValue;
                EditorGUILayout.HelpBox($"Wave will last {totalTicks} ticks", MessageType.Info);
            }
            
            EditorGUILayout.Space();
            
            // Spawn Timing
            float spawnPercent = spawnTimeNormalized.floatValue * 100f;
            EditorGUILayout.LabelField($"Spawn Start: {spawnPercent:F0}% into wave");
            spawnTimeNormalized.floatValue = EditorGUILayout.Slider("Spawn Time", spawnTimeNormalized.floatValue, 0f, 1f);
            
            // Visual representation
            DrawTimingBar(spawnTimeNormalized.floatValue);
            
            EditorGUILayout.Space();
            
            // Spawning Mode
            EditorGUILayout.PropertyField(continuousSpawning, new GUIContent("Continuous Spawning", "If enabled, enemies spawn throughout the wave"));
            
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        
        EditorGUILayout.Space();
        
        // Wave Settings
        EditorGUILayout.PropertyField(deletePreviousWaveAnimals, new GUIContent("Clear Previous Wave", "Delete all animals when starting a new wave"));
        
        EditorGUILayout.Space();
        
        // Wave Sequence Section
        showWaveSequence = EditorGUILayout.BeginFoldoutHeaderGroup(showWaveSequence, "Wave Sequence");
        if (showWaveSequence) {
            EditorGUI.indentLevel++;
            
            if (wavesSequence.arraySize == 0) {
                EditorGUILayout.HelpBox("No waves defined! Add wave definitions to the sequence.", MessageType.Warning);
            }
            
            // Custom list display
            for (int i = 0; i < wavesSequence.arraySize; i++) {
                EditorGUILayout.BeginHorizontal();
                
                var element = wavesSequence.GetArrayElementAtIndex(i);
                var waveDef = element.objectReferenceValue as WaveDefinition;
                
                string label = $"Round {i + 1}";
                if (waveDef != null && !string.IsNullOrEmpty(waveDef.waveName)) {
                    label += $": {waveDef.waveName}";
                }
                
                EditorGUILayout.PropertyField(element, new GUIContent(label));
                
                if (GUILayout.Button("X", GUILayout.Width(20))) {
                    wavesSequence.DeleteArrayElementAtIndex(i);
                    break;
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            // Add button
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Add Wave", GUILayout.Width(100))) {
                wavesSequence.InsertArrayElementAtIndex(wavesSequence.arraySize);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        
        EditorGUILayout.Space();
        
        // Runtime Debug Info
        if (Application.isPlaying) {
            showDebugInfo = EditorGUILayout.BeginFoldoutHeaderGroup(showDebugInfo, "Runtime Debug");
            if (showDebugInfo) {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.LabelField("Status", waveManager.IsWaveActive ? "Wave Active" : "Idle");
                
                if (waveManager.IsWaveActive && TickManager.Instance != null) {
                    // Show wave progress bar
                    EditorGUILayout.Space();
                    DrawRuntimeWaveProgress(waveManager);
                }
                
                EditorGUILayout.Space();
                
                // Debug buttons
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Force End Wave")) {
                    waveManager.SendMessage("Debug_ForceEndWave", SendMessageOptions.DontRequireReceiver);
                }
                if (GUILayout.Button("Force Spawn")) {
                    waveManager.SendMessage("Debug_ForceSpawn", SendMessageOptions.DontRequireReceiver);
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        
        serializedObject.ApplyModifiedProperties();
    }
    
    void DrawTimingBar(float spawnTime) {
        Rect rect = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true));
        
        // Background
        EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f));
        
        // Wave duration bar
        Rect waveRect = new Rect(rect.x, rect.y, rect.width, rect.height);
        EditorGUI.DrawRect(waveRect, new Color(0.3f, 0.5f, 0.3f));
        
        // Spawn marker
        float spawnX = rect.x + (rect.width * spawnTime);
        Rect spawnRect = new Rect(spawnX - 2, rect.y, 4, rect.height);
        EditorGUI.DrawRect(spawnRect, Color.yellow);
        
        // Labels
        GUI.Label(new Rect(rect.x, rect.y, 50, rect.height), "Start", EditorStyles.miniLabel);
        GUI.Label(new Rect(rect.x + rect.width - 30, rect.y, 30, rect.height), "End", EditorStyles.miniLabel);
        GUI.Label(new Rect(spawnX - 25, rect.y - 20, 50, 20), "Spawn", EditorStyles.centeredGreyMiniLabel);
    }
    
    void DrawRuntimeWaveProgress(WaveManager waveManager) {
        // This would need access to private fields, so we'd need to make them public or add properties
        EditorGUILayout.HelpBox("Wave progress visualization requires exposing runtime data", MessageType.Info);
    }
}