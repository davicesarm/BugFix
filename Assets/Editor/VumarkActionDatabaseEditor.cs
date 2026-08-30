using System;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VumarkActionDatabase))]
public class VumarkActionDatabaseEditor : Editor
{
    private const string SourcePath =
        "Assets/StreamingAssets/vumark_actions.json";

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawImportSection();

        EditorGUILayout.Space(8f);

        SerializedProperty actionsProp =
            serializedObject.FindProperty("actions");

        EditorGUILayout.PropertyField(
            actionsProp,
            true
        );

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawImportSection()
    {
        EditorGUILayout.LabelField(
            "Import",
            EditorStyles.boldLabel
        );

        EditorGUILayout.HelpBox(
            "Importa dados do arquivo: " +
            SourcePath +
            "\nFormato: { vumarkId: { acao, texto_traduzido, texto_criptografado, scene_name } }",
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
        bool confirmou =
            EditorUtility.DisplayDialog(
                "Limpar lista",
                "Remover todas as actions do database?",
                "Sim",
                "Cancelar"
            );

        if (!confirmou)
        {
            return;
        }

        serializedObject.Update();

        SerializedProperty actionsProp =
            serializedObject.FindProperty("actions");

        actionsProp.ClearArray();

        serializedObject.ApplyModifiedProperties();

        EditorUtility.SetDirty(target);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private void ImportFromJson()
    {
        if (!File.Exists(SourcePath))
        {
            EditorUtility.DisplayDialog(
                "Arquivo nao encontrado",
                "Nao foi encontrado:\n" +
                SourcePath,
                "OK"
            );

            return;
        }

        string jsonText;

        try
        {
            jsonText =
                File.ReadAllText(SourcePath);
        }
        catch (Exception ex)
        {
            EditorUtility.DisplayDialog(
                "Erro ao ler arquivo",
                "Falha ao ler o JSON:\n" +
                ex.Message,
                "OK"
            );

            return;
        }

        JObject root;

        try
        {
            root =
                JObject.Parse(jsonText);
        }
        catch (Exception ex)
        {
            EditorUtility.DisplayDialog(
                "JSON invalido",
                "Falha ao interpretar JSON:\n" +
                ex.Message,
                "OK"
            );

            return;
        }

        serializedObject.Update();

        SerializedProperty actionsProp =
            serializedObject.FindProperty("actions");

        actionsProp.ClearArray();

        int imported = 0;
        int showTextSemCriptografia = 0;

        foreach (
            JProperty vumarkProperty
            in root.Properties()
        )
        {
            string vumarkId =
                vumarkProperty.Name?.Trim();

            if (
                string.IsNullOrWhiteSpace(
                    vumarkId
                )
            )
            {
                continue;
            }

            if (
                vumarkProperty.Value
                is not JObject data
            )
            {
                continue;
            }

            string actionTypeRaw =
                ReadFirstNonEmpty(
                    data,
                    "acao",
                    "actionType"
                );

            if (
                string.IsNullOrWhiteSpace(
                    actionTypeRaw
                )
            )
            {
                actionTypeRaw =
                    VumarkActionType
                        .ShowText
                        .ToString();
            }

            string text =
                ReadFirstNonEmpty(
                    data,
                    "texto_traduzido",
                    "text",
                    "texto"
                );

            string textNoHints =
                ReadFirstNonEmpty(
                    data,
                    "texto_criptografado",
                    "texto_sem_dicas",
                    "texto_sem_dica",
                    "textNoHints"
                );

            string sceneName =
                ReadFirstNonEmpty(
                    data,
                    "scene_name",
                    "sceneName"
                );

            if (
                !Enum.TryParse(
                    actionTypeRaw,
                    true,
                    out VumarkActionType actionType
                )
            )
            {
                actionType =
                    VumarkActionType.None;
            }

            if (
                actionType ==
                    VumarkActionType.ShowText &&
                string.IsNullOrWhiteSpace(
                    textNoHints
                )
            )
            {
                showTextSemCriptografia++;

                Debug.LogWarning(
                    $"VumarkActionDatabaseEditor: VuMark '{vumarkId}' é ShowText, mas não possui texto_criptografado."
                );
            }

            int index =
                actionsProp.arraySize;

            actionsProp
                .InsertArrayElementAtIndex(
                    index
                );

            SerializedProperty entry =
                actionsProp
                    .GetArrayElementAtIndex(
                        index
                    );

            entry
                .FindPropertyRelative(
                    "vumarkId"
                )
                .stringValue =
                vumarkId;

            entry
                .FindPropertyRelative(
                    "actionType"
                )
                .enumValueIndex =
                (int)actionType;

            entry
                .FindPropertyRelative(
                    "text"
                )
                .stringValue =
                text;

            entry
                .FindPropertyRelative(
                    "textNoHints"
                )
                .stringValue =
                textNoHints;

            entry
                .FindPropertyRelative(
                    "sceneName"
                )
                .stringValue =
                sceneName;

            entry.isExpanded =
                false;

            imported++;

            Debug.Log(
                $"VuMark importado: {vumarkId} | " +
                $"acao={actionType} | " +
                $"texto='{text}' | " +
                $"criptografado='{textNoHints}'"
            );
        }

        serializedObject
            .ApplyModifiedProperties();

        EditorUtility.SetDirty(
            target
        );

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string mensagem =
            $"{imported} entries importadas.";

        if (showTextSemCriptografia > 0)
        {
            mensagem +=
                $"\n\nATENCAO: {showTextSemCriptografia} cartas ShowText " +
                "nao possuem texto_criptografado.";
        }

        EditorUtility.DisplayDialog(
            "Importacao concluida",
            mensagem,
            "OK"
        );
    }

    private static string ReadFirstNonEmpty(
        JObject data,
        params string[] keys
    )
    {
        for (
            int i = 0;
            i < keys.Length;
            i++
        )
        {
            JToken token =
                data[keys[i]];

            if (token == null)
            {
                continue;
            }

            string value =
                token.Type ==
                JTokenType.String
                    ? token.Value<string>()
                    : token.ToString();

            if (
                !string.IsNullOrWhiteSpace(
                    value
                )
            )
            {
                return value.Trim();
            }
        }

        return string.Empty;
    }
}