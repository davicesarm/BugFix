using UnityEngine;
using Vuforia;
using System.Collections;
using System.Collections.Generic;
using TMPro;


// [System.Serializable]
// public class VumarkToTranslatedText
// {
//     public string vumarkId;
//     public string translatedText;
// }

public class TranslateCards : DefaultObserverEventHandler
{
    public GameObject cardModelPrefab;
    // public List<VumarkToTranslatedText> vumarkIdTranslatedTexts = new();
    private GameObject currentDisplayedContent;
    private Dictionary<string, string> translatedTextsMap;

    // Variável para controlar a corrotina em execução
    private Coroutine runningCoroutine;

    protected override void Start()
    {
        base.Start();
        translatedTextsMap = new()
        {
            ["036d58e203274b32a837b082e54fb939"] = "Parece-se com uma soma",
            ["0869ea01e5df4cf3a9d63fcba244df87"] = "Parece-se com uma multiplicação",
            ["a4271545dcdf4a1da486c52324f09808"] = "Inverte o resultado da entrada",
            ["91eb507b040a401d81b321a10fe69e47"] = "Apenas um resultado de saída é igual a zero",
            ["8e74d26ceee746f8a58f977ed86a32a9"] = "Apenas um resultado de saída é igual a um",
            ["6cfef27692404e19a025dd24b60ddcf9"] = "O resultado da saída é sempre diferente do resultado da entrada",
            ["e981e1dcbb21463882ab061294ff1c03"] = "Função Principal: A saída é nível lógico alto (1) somente se TODAS as entradas estiverem em nível lógico alto (1)",
            ["f0f2da616a18428b91149ee9f8c56721"] = "Expressão Booleana: Q = A • B",
            ["e4f04b73566c45f582c5869dd0884df9"] = "Função Principal: A saída é sempre o oposto lógico da única entrada",
            ["ca0ee1c44c5e498b9da9484a4d9cff34"] = "Expressão Booleana: Q = Ā",
            ["bb4ade09e0ba4ec0ba6e711b32b1bbbc"] = "Função Principal: A saída é nível lógico ALTO (1) se QUALQUER uma das entradas estiver em nível lógico ALTO (1)",
            ["abac680e4b134a5e95882cec0d785a97"] = "Expressão Booleana: Q = A + B",
            ["d5636a0f96ae46839218078825859d25"] = " ▬ ▬ ▬ características que ▬ conseguir  ▬ ▬  porta lógica \"AND\"",
            ["ca84774151a547be8bb8109e373aecc3"] = "Carta resposta",
            ["51d72c71b1274b9aa8ed1be9c872e603"] = "Carta glitch"

        };

        // foreach (var mapping in vumarkIdTranslatedTexts)
        // {
        //     if (mapping != null && !string.IsNullOrEmpty(mapping.vumarkId) && !string.IsNullOrEmpty(mapping.translatedText))
        //     {
        //         string vuMarkId = mapping.vumarkId.Trim();
        //         translatedTextsMap[vuMarkId] = mapping.translatedText;
        //     }
        // }
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

    private string ChooseRandomDebuff()
    {
        var debuffs = new List<string>
        {
            "Positivo: Retire uma carta do tabuleiro (dá direito a mais um erro ou anula um erro anterior cometido, retornando a carta para a mão do jogador) ou ganhe mais um uso do VU Mark",
            "Neutro: Nenhuma ação é realizada. Uma mensagem é exibida, tipo: [você sabia que...] \"Did you know that...\" e uma informação interessante que pode ajudar ou já ter sido usada anteriormente",
            "Sem efeito: BugFix está corrigindo os erros de programação e tem feito um ótimo trabalho. Este Glitch já foi corrigido com sucesso!",
            "Negativo: Adicione uma carta ao tabuleiro ou perca uma chance de usar o VU Mark.",
        };
        int randomIndex = Random.Range(0, debuffs.Count);
        return debuffs[randomIndex];
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
        if (translatedTextsMap.ContainsKey(vumarkId))
        {
            string translatedText = translatedTextsMap[vumarkId];
            if (translatedText == "Carta glitch")
            {
                translatedText = ChooseRandomDebuff();
            }

            currentDisplayedContent = Instantiate(cardModelPrefab, transform);
            if (currentDisplayedContent != null)
            {
                var textComponent = currentDisplayedContent.GetComponentInChildren<TextMeshPro>();
                if (textComponent != null)
                {
                    textComponent.text = translatedText;
                }
                currentDisplayedContent.transform.localPosition = new Vector3(-0.95f, 0f, 0f);
                currentDisplayedContent.transform.localRotation = Quaternion.Euler(180f, -180f, 180f);
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