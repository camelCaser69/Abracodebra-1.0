using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(SmartRuleTile))]
public class SmartRuleTileEditor : Editor
{
    // Serialized properties for SmartRuleTile fields
    private SerializedProperty centerSpriteProp;
    private SerializedProperty topEdgeProp, bottomEdgeProp, leftEdgeProp, rightEdgeProp;
    private SerializedProperty topLeftCornerProp, topRightCornerProp, bottomLeftCornerProp, bottomRightCornerProp;
    private SerializedProperty blendingTagProp;

    // Serialized properties for standard RuleTile fields
    private SerializedProperty defaultSpriteProp;
    private SerializedProperty defaultGameObjectProp;
    private SerializedProperty defaultColliderProp; // Keep this reference for drawing the default field
    private SerializedProperty tilingRulesProp;

    private void OnEnable()
    {
        // Link serialized properties
        centerSpriteProp = serializedObject.FindProperty("centerSprite");
        topEdgeProp = serializedObject.FindProperty("topEdge");
        bottomEdgeProp = serializedObject.FindProperty("bottomEdge");
        leftEdgeProp = serializedObject.FindProperty("leftEdge");
        rightEdgeProp = serializedObject.FindProperty("rightEdge");
        topLeftCornerProp = serializedObject.FindProperty("topLeftCorner");
        topRightCornerProp = serializedObject.FindProperty("topRightCorner");
        bottomLeftCornerProp = serializedObject.FindProperty("bottomLeftCorner");
        bottomRightCornerProp = serializedObject.FindProperty("bottomRightCorner");
        blendingTagProp = serializedObject.FindProperty("blendingTag");

        defaultSpriteProp = serializedObject.FindProperty("m_DefaultSprite");
        defaultGameObjectProp = serializedObject.FindProperty("m_DefaultGameObject");
        defaultColliderProp = serializedObject.FindProperty("m_DefaultColliderType"); // <<< CORRECTED PROPERTY NAME (Likely this)
        tilingRulesProp = serializedObject.FindProperty("m_TilingRules");
    }

    public override void OnInspectorGUI()
    {
        SmartRuleTile smartTile = (SmartRuleTile)target;
        serializedObject.Update();

        // Draw Default fields
        EditorGUILayout.LabelField("Defaults", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(defaultSpriteProp);
        EditorGUILayout.PropertyField(defaultGameObjectProp);
        EditorGUILayout.PropertyField(defaultColliderProp); // Draw the default collider selection field
        EditorGUILayout.Space();

        // Draw Smart Tiling Sprite fields
        EditorGUILayout.LabelField("Smart Tiling Sprites", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(centerSpriteProp);
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(topEdgeProp);
        EditorGUILayout.PropertyField(bottomEdgeProp);
        EditorGUILayout.PropertyField(leftEdgeProp);
        EditorGUILayout.PropertyField(rightEdgeProp);
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(topLeftCornerProp);
        EditorGUILayout.PropertyField(topRightCornerProp);
        EditorGUILayout.PropertyField(bottomLeftCornerProp);
        EditorGUILayout.PropertyField(bottomRightCornerProp);
        EditorGUILayout.Space();

        // Draw Blending Options
        EditorGUILayout.LabelField("Blending Options", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(blendingTagProp);
        EditorGUILayout.HelpBox("If Blending Tag is set, 'Not This' neighbours might require custom rule overrides or Rule Transforms for complex tag-based blending. Basic generation assumes 'Not This' means any tile *other* than this SmartRuleTile.", MessageType.Info);
        EditorGUILayout.Space();


        // Generate Rules Button
        if (GUILayout.Button("Generate Basic Rules", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("Confirm Rule Generation",
                "Clear existing rules and generate new basic 3x3 rules based on assigned sprites?",
                "Generate", "Cancel"))
            {
                GenerateRules(smartTile);
                Repaint(); // Force repaint
            }
        }
        EditorGUILayout.HelpBox("Assign sprites above first. Uses This/NotThis conditions.", MessageType.None);
        EditorGUILayout.Space();


        // Draw the Tiling Rules list itself
        EditorGUILayout.PropertyField(tilingRulesProp, true);


        serializedObject.ApplyModifiedProperties();
    }

    private void GenerateRules(SmartRuleTile tile)
    {
        const int T = RuleTile.TilingRule.Neighbor.This;
        const int N = RuleTile.TilingRule.Neighbor.NotThis;

        // Clear existing rules
        tilingRulesProp.ClearArray();

        // Define rules using specific neighbor indices for clarity
        // Indices: 0:TL, 1:T, 2:TR, 3:L, 4:R, 5:BL, 6:B, 7:BR

        // --- Add Rules (Order: Most specific to least specific) ---

        // Outer Corners (Two adjacent sides are NotThis, others This)
        AddRule(tile, tile.topLeftCorner,     new int[] { N, N, T, N, T, T, T, T }); // TL, T, L are N or don't exist implicitly
        AddRule(tile, tile.topRightCorner,    new int[] { T, N, N, T, N, T, T, T }); // T, TR, R are N
        AddRule(tile, tile.bottomLeftCorner,  new int[] { T, T, T, N, T, N, N, T }); // L, BL, B are N
        AddRule(tile, tile.bottomRightCorner, new int[] { T, T, T, T, N, T, N, N }); // R, B, BR are N

        // Edges (One adjacent side is NotThis, others This)
        AddRule(tile, tile.topEdge,           new int[] { T, N, T, T, T, T, T, T });
        AddRule(tile, tile.bottomEdge,        new int[] { T, T, T, T, T, T, N, T });
        AddRule(tile, tile.leftEdge,          new int[] { T, T, T, N, T, T, T, T });
        AddRule(tile, tile.rightEdge,         new int[] { T, T, T, T, N, T, T, T });

        // Center (All cardinal directions are This) - Acts as a fallback
        AddRule(tile, tile.centerSprite,      new int[] { T, T, T, T, T, T, T, T });

        // Apply changes via SerializedObject
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(tile);
    }

    // Helper method to add a new TilingRule via SerializedProperty
    private void AddRule(SmartRuleTile tile, Sprite sprite, int[] neighbors)
    {
        if (sprite == null) return; // Don't add rule if sprite is missing

        // Add new element to the rules list property
        tilingRulesProp.InsertArrayElementAtIndex(tilingRulesProp.arraySize);
        SerializedProperty newRuleProp = tilingRulesProp.GetArrayElementAtIndex(tilingRulesProp.arraySize - 1);

        // Get properties within the TilingRule element
        SerializedProperty neighborsProp = newRuleProp.FindPropertyRelative("m_Neighbors");
        SerializedProperty spritesProp = newRuleProp.FindPropertyRelative("m_Sprites");
        SerializedProperty outputProp = newRuleProp.FindPropertyRelative("m_Output");
        SerializedProperty colliderProp = newRuleProp.FindPropertyRelative("m_ColliderType");
        SerializedProperty transformProp = newRuleProp.FindPropertyRelative("m_RuleTransform");

        // Set neighbor conditions
        if (neighborsProp.arraySize != 8) neighborsProp.arraySize = 8;
        for (int i = 0; i < 8; i++) {
            neighborsProp.GetArrayElementAtIndex(i).intValue = neighbors[i];
        }

        // Set output sprite
        if (spritesProp.arraySize != 1) spritesProp.arraySize = 1;
        spritesProp.GetArrayElementAtIndex(0).objectReferenceValue = sprite;

        // Set defaults for other rule properties
        outputProp.enumValueIndex = (int)RuleTile.TilingRule.OutputSprite.Single;

        // <<< FIX: Get collider type directly from the target tile object's current default setting >>>
        colliderProp.enumValueIndex = (int)tile.m_DefaultColliderType;
        // If the above name is wrong, try getting it from the SerializedProperty we found in OnEnable:
        // colliderProp.enumValueIndex = defaultColliderProp.enumValueIndex;

        transformProp.enumValueIndex = (int)RuleTile.TilingRule.Transform.Fixed;
    }
}