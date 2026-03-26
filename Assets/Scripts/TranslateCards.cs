using UnityEngine;
using Vuforia;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;

public class TranslateCards : DefaultObserverEventHandler
{
    private GameObject cardModelPrefab;

    [SerializeField]
    private VumarkActionDatabase actionDatabase;

    [SerializeField]
    private float repeatedScanCooldownSeconds = 0.4f;

    [SerializeField]
    private int stableIdFramesRequired = 2;

    [SerializeField]
    private int maxFramesToStabilize = 8;

    private TextMeshPro cardText;
    private VuMarkBehaviour vuMarkBehaviour;
    private string lastScannedVumarkId;
    private float lastScanTime;
    private bool isSceneLoading;
    private Coroutine trackingFoundRoutine;

    private void Awake()
    {
        if (transform.childCount > 0)
        {
            cardModelPrefab = transform.GetChild(0).gameObject;
            cardText = cardModelPrefab.GetComponentInChildren<TextMeshPro>();
        }
        else
        {
            Debug.LogError("TranslateCards: objeto sem filho para cardModelPrefab.");
        }

        if (!TryGetComponent<VuMarkBehaviour>(out vuMarkBehaviour))
        {
            Debug.LogError("TranslateCards: VuMarkBehaviour não encontrado no mesmo GameObject.");
        }
    }

    protected override void OnTrackingFound()
    {
        base.OnTrackingFound();

        // Limpa imediatamente para não mostrar o texto da carta anterior.
        SetCardText(string.Empty);

        if (trackingFoundRoutine != null)
            StopCoroutine(trackingFoundRoutine);

        // Aguarda o ID do VuMark estabilizar por alguns frames antes de agir.
        trackingFoundRoutine = StartCoroutine(ExecuteVumarkActionWhenStable());
    }

    protected override void OnTrackingLost()
    {
        base.OnTrackingLost();

        if (trackingFoundRoutine != null)
        {
            StopCoroutine(trackingFoundRoutine);
            trackingFoundRoutine = null;
        }

        isSceneLoading = false;
        lastScannedVumarkId = null;

        // Mantém a UI consistente quando o alvo é perdido.
        if (cardText != null)
        {
            cardText.text = string.Empty;
        }
    }

    private IEnumerator ExecuteVumarkActionWhenStable()
    {
        string candidateId = null;
        int stableCount = 0;

        for (int i = 0; i < maxFramesToStabilize; i++)
        {
            if (TryGetVumarkId(out string currentId))
            {
                if (currentId == candidateId)
                {
                    stableCount++;
                }
                else
                {
                    candidateId = currentId;
                    stableCount = 1;
                }

                if (stableCount >= stableIdFramesRequired)
                {
                    ExecuteVumarkAction(candidateId);
                    break;
                }
            }

            yield return null;
        }

        trackingFoundRoutine = null;
    }

    private void ExecuteVumarkAction(string vumarkId)
    {
        if (cardModelPrefab == null)
        {
            Debug.LogWarning("TranslateCards: cardModelPrefab não está definido.");
            return;
        }

        if (actionDatabase == null)
        {
            Debug.LogWarning("TranslateCards: actionDatabase não está definido no Inspector.");
            return;
        }

        if (IsRepeatedScan(vumarkId))
            return;

        if (!actionDatabase.TryGetAction(vumarkId, out var action))
        {
            Debug.LogWarning("TranslateCards: ID de VuMark não mapeado: " + vumarkId);
            RegisterScan(vumarkId);
            return;
        }

        switch (action.actionType)
        {
            case VumarkActionType.ShowText:
                SetCardText(action.text);
                break;

            case VumarkActionType.ShowRandomDebuff:
                SetCardText(ChooseRandomDebuff());
                break;

            case VumarkActionType.LoadScene:
                if (string.IsNullOrWhiteSpace(action.sceneName))
                {
                    Debug.LogWarning("TranslateCards: sceneName vazio para LoadScene no VuMark: " + vumarkId);
                    break;
                }

                if (isSceneLoading)
                    break;

                if (!Application.CanStreamedLevelBeLoaded(action.sceneName))
                {
                    Debug.LogError("TranslateCards: cena não está no Build Settings: " + action.sceneName);
                    break;
                }

                isSceneLoading = true;
                SceneManager.LoadScene(action.sceneName);
                break;

            case VumarkActionType.None:
            default:
                Debug.Log("VuMark sem ação: " + vumarkId);
                break;
        }

        RegisterScan(vumarkId);
    }

    private bool TryGetVumarkId(out string vumarkId)
    {
        vumarkId = null;

        if (vuMarkBehaviour == null)
        {
            Debug.LogWarning("TranslateCards: VuMarkBehaviour não disponível.");
            return false;
        }

        vumarkId = vuMarkBehaviour.InstanceId.StringValue;

        if (string.IsNullOrWhiteSpace(vumarkId))
        {
            return false;
        }

        return true;
    }

    private bool IsRepeatedScan(string vumarkId)
    {
        return lastScannedVumarkId == vumarkId &&
               Time.time - lastScanTime < repeatedScanCooldownSeconds;
    }

    private void RegisterScan(string vumarkId)
    {
        lastScannedVumarkId = vumarkId;
        lastScanTime = Time.time;
    }

    private void SetCardText(string text)
    {
        if (cardText == null)
        {
            Debug.LogWarning("TranslateCards: TextMeshPro não encontrado em cardModelPrefab.");
            return;
        }

        cardText.text = text;
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