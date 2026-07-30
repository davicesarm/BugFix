using System;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VumarkActionDatabase))]
public class VumarkActionDatabaseEditor : Editor
{
    private const string SourcePath = "Assets/StreamingAssets/vumark_actions.json";

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
            "Importa dados do arquivo: " + SourcePath + "\nFormato: { vumarkId: { acao, texto_traduzido, texto_sem_dicas, scene_name } }",
            MessageType.Info
        );

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Importar do JSON"))
            {
                ImportFromJson();
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

    private void ImportFromJson()
    {
        if (!File.Exists(SourcePath))
        {
            EditorUtility.DisplayDialog("Arquivo nao encontrado", "Nao foi encontrado: " + SourcePath, "OK");
            return;
        }

        string jsonText = File.ReadAllText(SourcePath);
        JObject root;

        try
        {
            root = JObject.Parse(jsonText);
        }
        catch (Exception ex)
        {
            EditorUtility.DisplayDialog("JSON invalido", "Falha ao ler JSON:\n" + ex.Message, "OK");
            return;
        }

        SerializedProperty actionsProp = serializedObject.FindProperty("actions");

        actionsProp.ClearArray();

        int imported = 0;
        foreach (var vumarkProperty in root.Properties())
        {
            string vumarkId = vumarkProperty.Name?.Trim();
            if (string.IsNullOrWhiteSpace(vumarkId))
                continue;

            if (vumarkProperty.Value is not JObject data)
                continue;

            string actionTypeRaw = ReadFirstNonEmpty(data, "acao", "actionType");
            if (string.IsNullOrWhiteSpace(actionTypeRaw))
                actionTypeRaw = VumarkActionType.ShowText.ToString();

            string text = ReadFirstNonEmpty(data, "texto_traduzido", "text", "texto");
            string textNoHints = ReadFirstNonEmpty(data, "texto_sem_dicas", "texto_sem_dica", "textNoHints");
            string sceneName = ReadFirstNonEmpty(data, "scene_name", "sceneName");

            if (!Enum.TryParse(actionTypeRaw, true, out VumarkActionType actionType))
                actionType = VumarkActionType.None;

            int index = actionsProp.arraySize;
            actionsProp.InsertArrayElementAtIndex(index);
            SerializedProperty entry = actionsProp.GetArrayElementAtIndex(index);

            entry.FindPropertyRelative("vumarkId").stringValue = vumarkId;
            entry.FindPropertyRelative("actionType").enumValueIndex = (int)actionType;
            entry.FindPropertyRelative("text").stringValue = text;
            entry.FindPropertyRelative("textNoHints").stringValue = textNoHints;
            entry.FindPropertyRelative("sceneName").stringValue = sceneName;
            entry.isExpanded = false;

            imported++;
        }

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Importacao concluida", $"{imported} entries importadas.", "OK");
    }

    private static string ReadFirstNonEmpty(JObject data, params string[] keys)
    {
        for (int i = 0; i < keys.Length; i++)
        {
            JToken token = data[keys[i]];
            if (token == null)
                continue;

            string value = token.Type == JTokenType.String
                ? token.Value<string>()
                : token.ToString();

            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        return string.Empty;
    }
}
