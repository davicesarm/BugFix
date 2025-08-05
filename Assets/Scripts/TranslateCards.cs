using UnityEngine;
using Vuforia;
using System.Collections.Generic;
using TMPro;

public class TranslateCards : DefaultObserverEventHandler
{
    private GameObject cardModelPrefab;
    private Dictionary<string, string> translatedTextsMap;

    private void Awake()
    {
        cardModelPrefab = transform.GetChild(0).gameObject;
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
    }

    protected override void OnTrackingFound()
    {
        ChangeCardText();
        base.OnTrackingFound();
    }

    private void ChangeCardText()
    {
        if (cardModelPrefab == null)
        {
            throw new System.Exception("cardModelPrefab não está definido.");
        }
        string vumarkId = GetVumarkId();
        string translatedText = GetTextByVumarkId(vumarkId);
        cardModelPrefab.GetComponentInChildren<TextMeshPro>().text = translatedText;
    }

    private string GetVumarkId()
    {
       if (!TryGetComponent<VuMarkBehaviour>(out var vumarkBehaviour))
       {
            throw new System.Exception("VumarkBehavior not found.");
       }
       return vumarkBehaviour.InstanceId.StringValue;
    }

    private string GetTextByVumarkId(string vumarkId)
    {
        if (translatedTextsMap.ContainsKey(vumarkId))
        {
            string translatedText = translatedTextsMap[vumarkId];
            if (translatedText == "Carta glitch")
            {
                translatedText = ChooseRandomDebuff();
            }
            return translatedText;
        }
        else
        {
            throw new System.Exception("ID de VuMark não mapeado: " + vumarkId);
        }
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
        int randomIndex = UnityEngine.Random.Range(0, debuffs.Count);
        return debuffs[randomIndex];
    }
}