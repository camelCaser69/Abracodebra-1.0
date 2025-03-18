using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(WizardController))]
public class WizardControllerStatusEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        WizardController wizard = (WizardController)target;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Active Status Effects", EditorStyles.boldLabel);
        if (wizard.activeStatusEffects != null && wizard.activeStatusEffects.Count > 0)
        {
            foreach (var effect in wizard.activeStatusEffects)
            {
                EditorGUILayout.LabelField($"{effect.GetType().Name}: {effect.damagePerSecond} DPS, {effect.duration - effect.Elapsed:F1}s remaining");
            }
        }
        else
        {
            EditorGUILayout.LabelField("None");
        }
    }
}