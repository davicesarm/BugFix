using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VumarkActionDatabase))]
public class VumarkActionDatabaseEditor : Editor
{
    private const string SourcePath = "Assets/StreamingAssets/vumark_ids.txt";

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawImportSection();
        EditorGUILayout.Space(8f);

        SerializedProperty actionsProp = serializedObject.FindProperty("actions");
        EditorGUILayout.PropertyField(actionsProp, true);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawImportSection()
    {
        EditorGUILayout.LabelField("Import", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Importa dados do arquivo: " + SourcePath + "\nFormato: vumarkId|actionType|text|sceneName",
            MessageType.Info
        );

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Importar do TXT"))
            {
                ImportFromTxt();
            }

            if (GUILayout.Button("Limpar Lista"))
            {
                ClearActions();
            }
        }
    }

    private void ClearActions()
    {
        if (!EditorUtility.DisplayDialog("Limpar lista", "Remover todas as actions do database?", "Sim", "Cancelar"))
            return;

        SerializedProperty actionsProp = serializedObject.FindProperty("actions");
        actionsProp.ClearArray();
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
        AssetDatabase.SaveAssets();
    }

    private void ImportFromTxt()
    {
        if (!File.Exists(SourcePath))
        {
            EditorUtility.DisplayDialog("Arquivo nao encontrado", "Nao foi encontrado: " + SourcePath, "OK");
            return;
        }

        string[] lines = File.ReadAllLines(SourcePath);
        SerializedProperty actionsProp = serializedObject.FindProperty("actions");

        actionsProp.ClearArray();

        int imported = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i]?.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                continue;

            string[] parts = line.Split('|');
            if (parts.Length < 4)
                continue;

            string vumarkId = parts[0].Trim();
            string actionTypeRaw = parts[1].Trim();
            string text = parts[2];
            string sceneName = parts[3].Trim();

            if (string.IsNullOrWhiteSpace(vumarkId))
                continue;

            if (!Enum.TryParse(actionTypeRaw, true, out VumarkActionType actionType))
                actionType = VumarkActionType.None;

            int index = actionsProp.arraySize;
            actionsProp.InsertArrayElementAtIndex(index);
            SerializedProperty entry = actionsProp.GetArrayElementAtIndex(index);

            entry.FindPropertyRelative("vumarkId").stringValue = vumarkId;
            entry.FindPropertyRelative("actionType").enumValueIndex = (int)actionType;
            entry.FindPropertyRelative("text").stringValue = text;
            entry.FindPropertyRelative("sceneName").stringValue = sceneName;
            entry.isExpanded = false;

            imported++;
        }

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Importacao concluida", $"{imported} entries importadas.", "OK");
    }
}
