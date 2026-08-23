using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

public enum VumarkActionType
{
    ShowText,
    ShowRandomDebuff,
    LoadScene,
    RedirectMinigame,
    ShowModel3D,
    None
}

[Serializable]
public class VumarkActionEntry
{
    public string vumarkId;
    public VumarkActionType actionType = VumarkActionType.ShowText;

    [TextArea(2, 5)]
    public string text;

    [TextArea(2, 5)]
    public string textNoHints;

    public string sceneName;

    public GameObject modelPrefab;
}

[CreateAssetMenu(
    fileName = "VumarkActionDatabase",
    menuName = "Vumark/Action Database"
)]
public class VumarkActionDatabase : ScriptableObject
{
    [Header("JSON das cartas")]
    [SerializeField]
    private TextAsset jsonFile;

    [Header("Ações manuais")]
    [SerializeField]
    private List<VumarkActionEntry> actions = new();

    private Dictionary<string, VumarkActionEntry> cachedMap;

    private string cachedJsonContent;

    private void OnEnable()
    {
        LimparCache();
    }

    public bool TryGetAction(
        string vumarkId,
        out VumarkActionEntry action
    )
    {
        action = null;

        if (string.IsNullOrWhiteSpace(vumarkId))
            return false;

        AtualizarCacheSeNecessario();

        string id = vumarkId.Trim();

        bool encontrou = cachedMap.TryGetValue(
            id,
            out action
        );

        if (!encontrou)
        {
            Debug.LogWarning(
                $"VumarkActionDatabase: VuMark '{id}' não encontrado."
            );

            return false;
        }

        Debug.Log(
            $"VumarkActionDatabase: VuMark '{id}' encontrado | " +
            $"text='{action.text}' | " +
            $"textNoHints='{action.textNoHints}'"
        );

        return true;
    }

    private void AtualizarCacheSeNecessario()
    {
        string jsonAtual =
            jsonFile != null
                ? jsonFile.text
                : string.Empty;

        if (
            cachedMap != null &&
            cachedJsonContent == jsonAtual
        )
        {
            return;
        }

        ReconstruirCache(jsonAtual);
    }

    private void ReconstruirCache(
        string jsonAtual
    )
    {
        cachedMap =
            new Dictionary<string, VumarkActionEntry>(
                StringComparer.OrdinalIgnoreCase
            );

        CarregarAcoesManuais();
        CarregarJson(jsonAtual);

        cachedJsonContent = jsonAtual;

        Debug.Log(
            $"VumarkActionDatabase: cache reconstruído. " +
            $"{cachedMap.Count} cartas carregadas."
        );
    }

    private void CarregarAcoesManuais()
    {
        if (actions == null)
            return;

        foreach (var entry in actions)
        {
            if (
                entry == null ||
                string.IsNullOrWhiteSpace(entry.vumarkId)
            )
            {
                continue;
            }

            string id =
                entry.vumarkId.Trim();

            cachedMap[id] =
                entry;
        }
    }

    private void CarregarJson(
        string jsonAtual
    )
    {
        if (jsonFile == null)
        {
            Debug.LogError(
                "VumarkActionDatabase: Json File NÃO foi configurado no Inspector."
            );

            return;
        }

        if (string.IsNullOrWhiteSpace(jsonAtual))
        {
            Debug.LogError(
                "VumarkActionDatabase: o arquivo JSON está vazio."
            );

            return;
        }

        try
        {
            JObject root =
                JObject.Parse(jsonAtual);

            foreach (
                JProperty property
                in root.Properties()
            )
            {
                string vumarkId =
                    property.Name.Trim();

                if (
                    property.Value
                    is not JObject dados
                )
                {
                    continue;
                }

                string acao =
                    dados.Value<string>("acao")
                    ?? string.Empty;

                string textoTraduzido =
                    dados.Value<string>("texto_traduzido")
                    ?? string.Empty;

                string textoCriptografado =
                    dados.Value<string>("texto_criptografado")
                    ?? string.Empty;

                VumarkActionType actionType =
                    ConverterActionType(
                        acao
                    );

                VumarkActionEntry entry =
                    new VumarkActionEntry
                    {
                        vumarkId =
                            vumarkId,

                        actionType =
                            actionType,

                        text =
                            textoTraduzido,

                        textNoHints =
                            textoCriptografado
                    };

                cachedMap[vumarkId] =
                    entry;

                Debug.Log(
                    $"JSON carregado: {vumarkId} | " +
                    $"traduzido='{textoTraduzido}' | " +
                    $"criptografado='{textoCriptografado}'"
                );
            }
        }
        catch (Exception ex)
        {
            Debug.LogError(
                $"VumarkActionDatabase: erro ao interpretar JSON: {ex}"
            );
        }
    }

    private VumarkActionType ConverterActionType(
        string acao
    )
    {
        if (
            Enum.TryParse(
                acao,
                true,
                out VumarkActionType actionType
            )
        )
        {
            return actionType;
        }

        Debug.LogWarning(
            $"VumarkActionDatabase: ação desconhecida '{acao}'."
        );

        return VumarkActionType.None;
    }

    private void LimparCache()
    {
        cachedMap = null;
        cachedJsonContent = null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        LimparCache();
    }
#endif
}