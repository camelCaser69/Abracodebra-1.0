using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

[CustomEditor(typeof(WaveDefinition))]
public class WaveDefinitionEditor : Editor {
    ReorderableList spawnList;
    SerializedProperty spawnEntries;
    
    void OnEnable() {
        spawnEntries = serializedObject.FindProperty("spawnEntries");
        
        spawnList = new ReorderableList(serializedObject, spawnEntries, true, true, true, true);
        
        spawnList.drawHeaderCallback = (Rect rect) => {
            EditorGUI.LabelField(rect, "Spawn Entries");
        };
        
        spawnList.elementHeightCallback = (int index) => {
            return EditorGUIUtility.singleLineHeight * 4 + 10;
        };
        
        spawnList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
            var element = spawnEntries.GetArrayElementAtIndex(index);
            rect.y += 2;
            
            // Draw spawn entry with better layout
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = 2;
            
            // Description and animal on same line
            Rect descRect = new Rect(rect.x, rect.y, rect.width * 0.4f, lineHeight);
            Rect animalRect = new Rect(rect.x + rect.width * 0.42f, rect.y, rect.width * 0.58f, lineHeight);
            
            EditorGUI.PropertyField(descRect, element.FindPropertyRelative("description"), GUIContent.none);
            EditorGUI.PropertyField(animalRect, element.FindPropertyRelative("animalDefinition"), GUIContent.none);
            
            rect.y += lineHeight + spacing;
            
            // Count and delay
            Rect countRect = new Rect(rect.x, rect.y, rect.width * 0.3f, lineHeight);
            Rect delayRect = new Rect(rect.x + rect.width * 0.32f, rect.y, rect.width * 0.3f, lineHeight);
            Rect intervalRect = new Rect(rect.x + rect.width * 0.64f, rect.y, rect.width * 0.36f, lineHeight);
            
            EditorGUI.LabelField(countRect, "Count:");
            countRect.x += 40;
            countRect.width -= 40;
            EditorGUI.PropertyField(countRect, element.FindPropertyRelative("spawnCount"), GUIContent.none);
            
            EditorGUI.LabelField(delayRect, "Delay:");
            delayRect.x += 35;
            delayRect.width -= 35;
            EditorGUI.PropertyField(delayRect, element.FindPropertyRelative("delayAfterSpawnTime"), GUIContent.none);
            
            EditorGUI.LabelField(intervalRect, "Interval:");
            intervalRect.x += 45;
            intervalRect.width -= 45;
            EditorGUI.PropertyField(intervalRect, element.FindPropertyRelative("spawnInterval"), GUIContent.none);
            
            rect.y += lineHeight + spacing;
            
            // Location type and radius
            Rect locTypeRect = new Rect(rect.x, rect.y, rect.width * 0.6f, lineHeight);
            Rect radiusRect = new Rect(rect.x + rect.width * 0.62f, rect.y, rect.width * 0.38f, lineHeight);
            
            EditorGUI.PropertyField(locTypeRect, element.FindPropertyRelative("spawnLocationType"), GUIContent.none);
            
            EditorGUI.LabelField(radiusRect, "Radius:");
            radiusRect.x += 45;
            radiusRect.width -= 45;
            EditorGUI.PropertyField(radiusRect, element.FindPropertyRelative("spawnRadius"), GUIContent.none);
        };
        
        spawnList.onAddCallback = (ReorderableList list) => {
            var index = list.serializedProperty.arraySize;
            list.serializedProperty.arraySize++;
            list.index = index;
            
            var element = list.serializedProperty.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("description").stringValue = "New Spawn Entry";
            element.FindPropertyRelative("spawnCount").intValue = 1;
            element.FindPropertyRelative("delayAfterSpawnTime").floatValue = 0f;
            element.FindPropertyRelative("spawnInterval").floatValue = 0.5f;
            element.FindPropertyRelative("spawnRadius").floatValue = 5f;
        };
    }
    
    public override void OnInspectorGUI() {
        serializedObject.Update();
        
        WaveDefinition waveDef = (WaveDefinition)target;
        
        // Header
        EditorGUILayout.LabelField("Wave Definition", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // Wave name with larger field
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Wave Name:", GUILayout.Width(80));
        SerializedProperty waveNameProp = serializedObject.FindProperty("waveName");
        waveNameProp.stringValue = EditorGUILayout.TextField(waveNameProp.stringValue);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        // Summary info
        int totalEnemies = 0;
        float totalDuration = 0;
        
        foreach (WaveSpawnEntry entry in waveDef.spawnEntries) {
            if (entry != null) {
                totalEnemies += entry.spawnCount;
                totalDuration = Mathf.Max(totalDuration, 
                    entry.delayAfterSpawnTime + (entry.spawnCount - 1) * entry.spawnInterval);
            }
        }
        
        EditorGUILayout.HelpBox(
            $"Total Enemies: {totalEnemies}\n" +
            $"Spawn Duration: ~{totalDuration:F1} seconds", 
            MessageType.Info
        );
        
        EditorGUILayout.Space();
        
        // Spawn entries list
        spawnList.DoLayoutList();
        
        serializedObject.ApplyModifiedProperties();
        
        EditorGUILayout.Space();
        
        // Quick actions
        if (GUILayout.Button("Clear All Entries")) {
            if (EditorUtility.DisplayDialog("Clear All Entries", 
                "Are you sure you want to remove all spawn entries?", 
                "Clear", "Cancel")) {
                waveDef.spawnEntries.Clear();
                EditorUtility.SetDirty(waveDef);
            }
        }
    }
}