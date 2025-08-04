using UnityEngine;
using Vuforia;
using System.Collections;
using System.Collections.Generic;


[System.Serializable]
public class VumarkToPrefab
{
    public string vumarkId;
    public GameObject prefab;
}

public class ShowTrophies : DefaultObserverEventHandler
{
    public List<VumarkToPrefab> vumarkPrefabMappings = new();

    private GameObject currentDisplayedContent;
    private Dictionary<string, GameObject> vumarkContentMap;

    // Variável para controlar a corrotina em execução
    private Coroutine runningCoroutine;

    protected override void Start()
    {
        base.Start();
        vumarkContentMap = new Dictionary<string, GameObject>();

        foreach (var mapping in vumarkPrefabMappings)
        {
            if (mapping != null && !string.IsNullOrEmpty(mapping.vumarkId) && mapping.prefab != null)
            {
                string cleanedId = mapping.vumarkId.Trim();
                vumarkContentMap[cleanedId] = mapping.prefab;
            }
        }
    }

    protected override void OnTrackingFound()
    {
        // base.OnTrackingFound();

        if (runningCoroutine != null)
        {
            StopCoroutine(runningCoroutine);
        }
        runningCoroutine = StartCoroutine(HandleTrackingFound());
    }

    protected override void OnTrackingLost()
    {
        // base.OnTrackingLost();

        if (runningCoroutine != null)
        {
            StopCoroutine(runningCoroutine);
        }
        runningCoroutine = StartCoroutine(HandleTrackingLost());
    }


    private IEnumerator HandleTrackingFound()
    {
        if (!TryGetComponent<VuMarkBehaviour>(out var vumarkBehaviour))
        {
            yield break; // Sai da corrotina se não encontrar o componente
        }

        var vumarkId = vumarkBehaviour.InstanceId.StringValue;

        Debug.Log("VuMark detectado com ID: " + vumarkId);

        // Se já existe um conteúdo sendo exibido, destrua-o primeiro
        if (currentDisplayedContent != null)
        {
            Destroy(currentDisplayedContent);
        }

        // ESPERA ATÉ O FINAL DO FRAME. Isso garante que o Destroy() foi concluído.
        yield return new WaitForEndOfFrame();

        // Agora que o estado está limpo, crie o novo objeto
        if (vumarkContentMap.ContainsKey(vumarkId))
        {
            GameObject contentToDisplay = vumarkContentMap[vumarkId];
            if (contentToDisplay != null)
            {
                currentDisplayedContent = Instantiate(contentToDisplay, transform);
                currentDisplayedContent.transform.SetLocalPositionAndRotation(new Vector3(0f, 0.05f, 0f), Quaternion.Euler(0f, -90f, -90f));
                currentDisplayedContent.transform.localScale = new Vector3(2f, 2f, 2f);
            }
        }
        else
        {
            Debug.LogWarning("ID de VuMark não mapeado: " + vumarkId);
        }

        runningCoroutine = null; // Libera a variável de controle
    }

    private IEnumerator HandleTrackingLost()
    {
        Debug.Log("VuMark perdido.");
        if (currentDisplayedContent != null)
        {
            Destroy(currentDisplayedContent);

            // ESPERA ATÉ O FINAL DO FRAME para garantir a destruição
            yield return new WaitForEndOfFrame();

            // Somente depois de garantir a destruição, limpe a referência
            currentDisplayedContent = null;
        }

        runningCoroutine = null; // Libera a variável de controle
    }
}