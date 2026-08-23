using UnityEngine;
using Vuforia;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

public class TranslateCards : DefaultObserverEventHandler
{
    private GameObject cardModelPrefab;

    [SerializeField]
    private VumarkActionDatabase actionDatabase;

    [SerializeField]
    private float repeatedScanCooldownSeconds = 0.4f;

    [SerializeField]
    private int stableIdFramesRequired = 2;

    [Header("Modelo 3D (ShowModel3D)")]
    [SerializeField]
    private Transform modelDisplayAnchor;

    [Tooltip("Multiplica a escala original do prefab só nessa exibição (não altera o prefab).")]
    [SerializeField]
    private float modelDisplayScaleMultiplier = 1f;

    [Tooltip("Deslocamento local (em relação à âncora) só nessa exibição (não altera o prefab).")]
    [SerializeField]
    private Vector3 modelDisplayOffset = Vector3.zero;

    private GameObject instantiatedModelInstance;

    [Header("Scan Confirmation (Card Canvas)")]
    [SerializeField]
    private GameObject confirmationButtonsContainer;

    [SerializeField]
    private Button confirmScanButton;

    [SerializeField]
    private Button cancelScanButton;

    [SerializeField]
    private string confirmationMessage = "Deseja escanear esta carta?";

    [SerializeField]
    private TMP_Text cardMessageText;

    private TMP_Text cardText;
    private VuMarkBehaviour vuMarkBehaviour;
    private MainGameController mainGameController;
    private string lastScannedVumarkId;
    private string pendingConfirmationVumarkId;
    private float lastScanTime;
    private bool isSceneLoading;
    private bool awaitingScanConfirmation;
    private string stabilizingCandidateId;
    private int stabilizingFrameCount;
    private Canvas confirmationCanvas;
    private CanvasGroup confirmationCanvasGroup;

    private void Awake()
    {
        if (transform.childCount > 0)
        {
            cardModelPrefab = transform.GetChild(0).gameObject;
            cardText = cardMessageText != null
                ? cardMessageText
                : cardModelPrefab.GetComponentInChildren<TMP_Text>(true);
        }
        else
        {
            Debug.LogError("TranslateCards: objeto sem filho para cardModelPrefab.");
        }

        if (!TryGetComponent<VuMarkBehaviour>(out vuMarkBehaviour))
        {
            Debug.LogError("TranslateCards: VuMarkBehaviour não encontrado no mesmo GameObject.");
        }

        mainGameController = FindAnyObjectByType<MainGameController>();
        ConfigureConfirmationButtons();
        CacheConfirmationUiReferences();
        EnsureUiInteractionSetup();
        ResetConfirmationState(hideCardModel: true, clearText: true);
    }

    protected override void OnDestroy()
    {
        UnregisterConfirmationButtons();
        base.OnDestroy();
    }

    private void OnDisable()
    {
        stabilizingCandidateId = null;
        stabilizingFrameCount = 0;

        ResetConfirmationState(hideCardModel: true, clearText: true);
    }

    private void Update()
    {
        HandleContinuousTrackingCheck();
        HandleManualButtonFallbackClick();
    }

    // O Vuforia nem sempre dispara OnTrackingFound/OnTrackingLost de novo ao trocar de carta
    // (todas usam o mesmo VuMark trackable) ou ao remover a carta com Extended Tracking ativo,
    // e às vezes o InstanceId demora alguns frames pra realmente atualizar. Por isso tudo é
    // resolvido aqui, num único lugar, frame a frame — sem coroutines correndo em paralelo
    // com os callbacks do SDK, que era a causa da carta "grudar" na tradução anterior.
    private void HandleContinuousTrackingCheck()
    {
        bool hasValidPose = vuMarkBehaviour != null
            && vuMarkBehaviour.TargetStatus.Status == Status.TRACKED;

        if (!hasValidPose)
        {
            HandleCardNoLongerVisible();
            return;
        }

        if (cardModelPrefab != null && !cardModelPrefab.activeSelf)
        {
            SetCardModelVisible(true);
        }

        if (!TryGetVumarkId(out string currentId))
        {
            stabilizingCandidateId = null;
            stabilizingFrameCount = 0;
            return;
        }

        string activeId = awaitingScanConfirmation ? pendingConfirmationVumarkId : lastScannedVumarkId;

        if (currentId == activeId)
        {
            // Já é a carta que está em tela (ou sendo confirmada); nada a fazer.
            stabilizingCandidateId = null;
            stabilizingFrameCount = 0;
            return;
        }

        // O ID lido é diferente do que está em tela. Só reage depois de ver o MESMO id
        // se repetir por alguns frames seguidos, pra não confundir com leitura atrasada/instável.
        if (currentId == stabilizingCandidateId)
        {
            stabilizingFrameCount++;
        }
        else
        {
            stabilizingCandidateId = currentId;
            stabilizingFrameCount = 1;
        }

        if (stabilizingFrameCount < stableIdFramesRequired)
            return;

        // ID estabilizado: é de fato uma carta nova.
        stabilizingCandidateId = null;
        stabilizingFrameCount = 0;

        ResetConfirmationState(hideCardModel: false, clearText: true);
        lastScannedVumarkId = null;
        TryStartScanConfirmation(currentId);
    }

    private void HandleCardNoLongerVisible()
    {
        stabilizingCandidateId = null;
        stabilizingFrameCount = 0;

        if (cardModelPrefab == null || !cardModelPrefab.activeSelf)
            return;

        ResetConfirmationState(hideCardModel: true, clearText: true);
        lastScannedVumarkId = null;
    }

    protected override void OnTrackingFound()
    {
        base.OnTrackingFound();

        // A detecção e a ação em si ficam por conta do HandleContinuousTrackingCheck (Update),
        // que já roda todo frame e evita duas rotinas competindo pelo mesmo ID.
        SetCardModelVisible(true);
    }

    protected override void OnTrackingLost()
    {
        base.OnTrackingLost();

        stabilizingCandidateId = null;
        stabilizingFrameCount = 0;

        ResetConfirmationState(hideCardModel: true, clearText: true);
        isSceneLoading = false;
        lastScannedVumarkId = null;
    }

    private void TryStartScanConfirmation(string vumarkId)
    {
        if (awaitingScanConfirmation)
            return;

        if (IsRepeatedScan(vumarkId))
            return;

        if (mainGameController == null)
        {
            mainGameController = FindAnyObjectByType<MainGameController>();
        }

        bool isAlreadyScanned = IsVumarkAlreadyScanned(vumarkId);

        // Se já foi escaneado, executa direto sem confirmação
        if (isAlreadyScanned)
        {
            Debug.Log($"TranslateCards: VuMark '{vumarkId}' já foi escaneado. Executando direto.");
            MarkVumarkAsScanned(vumarkId);
            ExecuteVumarkAction(vumarkId, forceNoHintsText: false);
            return;
        }

        // Cartas de debuff aleatório e de modelo 3D não custam dica: leem direto, sem confirmação.
        if (actionDatabase != null &&
            actionDatabase.TryGetAction(vumarkId, out var pendingAction) &&
            (pendingAction.actionType == VumarkActionType.ShowRandomDebuff ||
             pendingAction.actionType == VumarkActionType.ShowModel3D))
        {
            Debug.Log($"TranslateCards: VuMark '{vumarkId}' é {pendingAction.actionType}. Executando sem gastar dica.");
            MarkVumarkAsScanned(vumarkId);
            ExecuteVumarkAction(vumarkId, forceNoHintsText: false);
            return;
        }

        // Se não foi escaneado e não tem dicas, não permite
        if (!HasHintsAvailable())
        {
            ShowTextWithoutHintsIfPossible(vumarkId);
            RegisterScan(vumarkId);
            ShowConfirmationButtons(false);
            return;
        }

        if (confirmScanButton == null || cancelScanButton == null)
        {
            Debug.LogWarning("TranslateCards: Botões de confirmação não configurados no Inspector.");
            return;
        }

        // Mostra confirmação para novo scan
        pendingConfirmationVumarkId = vumarkId;
        awaitingScanConfirmation = true;
        SetCardText(confirmationMessage);
        ShowConfirmationButtons(true);
    }

    private void OnConfirmScanClicked()
    {
        Debug.Log("TranslateCards: botão Confirmar clicado.");

        if (!awaitingScanConfirmation || string.IsNullOrWhiteSpace(pendingConfirmationVumarkId))
            return;

        string vumarkId = pendingConfirmationVumarkId;

        // Tenta consumir um dica (apenas para novos scans, pois repetidos já executam direto)
        if (!TryConsumeHint())
        {
            SetCardText(GetNoHintsMessage());
            CancelPendingConfirmationLocally(clearText: false, hideCardModel: false);
            return;
        }

        Debug.Log($"TranslateCards: Dica consumida para o VuMark '{vumarkId}'.");

        // Marca como escaneado
        MarkVumarkAsScanned(vumarkId);

        CancelPendingConfirmationLocally(clearText: true, hideCardModel: false);
        ExecuteVumarkAction(vumarkId, forceNoHintsText: false);
    }

    private void OnCancelScanClicked()
    {
        Debug.Log("TranslateCards: botão Cancelar clicado.");
        CancelPendingConfirmationLocally(clearText: true, hideCardModel: true);
    }

    private void CancelPendingConfirmationLocally(bool clearText, bool hideCardModel)
    {
        ResetConfirmationState(hideCardModel, clearText);
    }

    private void ConfigureConfirmationButtons()
    {
        if (confirmScanButton != null)
        {
            confirmScanButton.onClick.RemoveListener(OnConfirmScanClicked);
            confirmScanButton.onClick.AddListener(OnConfirmScanClicked);
        }

        if (cancelScanButton != null)
        {
            cancelScanButton.onClick.RemoveListener(OnCancelScanClicked);
            cancelScanButton.onClick.AddListener(OnCancelScanClicked);
        }
    }

    private void UnregisterConfirmationButtons()
    {
        if (confirmScanButton != null)
        {
            confirmScanButton.onClick.RemoveListener(OnConfirmScanClicked);
        }

        if (cancelScanButton != null)
        {
            cancelScanButton.onClick.RemoveListener(OnCancelScanClicked);
        }
    }

    private void ShowConfirmationButtons(bool visible)
    {
        EnsureCanvasEventCamera();

        if (confirmationCanvasGroup != null)
        {
            confirmationCanvasGroup.interactable = visible;
            confirmationCanvasGroup.blocksRaycasts = visible;
        }

        if (confirmScanButton != null)
        {
            confirmScanButton.interactable = visible;
        }

        if (cancelScanButton != null)
        {
            cancelScanButton.interactable = visible;
        }

        if (confirmationButtonsContainer != null)
        {
            confirmationButtonsContainer.SetActive(visible);
            return;
        }

        if (confirmScanButton != null)
        {
            confirmScanButton.gameObject.SetActive(visible);
        }

        if (cancelScanButton != null)
        {
            cancelScanButton.gameObject.SetActive(visible);
        }
    }

    private void CacheConfirmationUiReferences()
    {
        if (confirmationButtonsContainer != null)
        {
            confirmationCanvas = confirmationButtonsContainer.GetComponentInParent<Canvas>(true);
            confirmationCanvasGroup = confirmationButtonsContainer.GetComponent<CanvasGroup>();
            return;
        }

        if (confirmScanButton != null)
        {
            confirmationCanvas = confirmScanButton.GetComponentInParent<Canvas>(true);
        }
        else if (cancelScanButton != null)
        {
            confirmationCanvas = cancelScanButton.GetComponentInParent<Canvas>(true);
        }
    }

    private void EnsureUiInteractionSetup()
    {
        if (EventSystem.current == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
#else
            eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
            DontDestroyOnLoad(eventSystemObject);
            Debug.LogWarning("TranslateCards: EventSystem não encontrado na cena. Um EventSystem foi criado automaticamente.");
        }

        if (confirmationCanvas != null && confirmationCanvas.GetComponent<GraphicRaycaster>() == null)
        {
            confirmationCanvas.gameObject.AddComponent<GraphicRaycaster>();
            Debug.LogWarning("TranslateCards: GraphicRaycaster ausente no Canvas da carta. Foi adicionado automaticamente.");
        }

        EnsureCanvasEventCamera();
    }

    private void EnsureCanvasEventCamera()
    {
        if (confirmationCanvas == null || confirmationCanvas.renderMode != RenderMode.WorldSpace)
            return;

        Camera eventCamera = GetUiEventCamera();
        if (eventCamera != null && confirmationCanvas.worldCamera != eventCamera)
        {
            confirmationCanvas.worldCamera = eventCamera;
        }
    }

    private void HandleManualButtonFallbackClick()
    {
        if (!awaitingScanConfirmation)
            return;

        if (confirmScanButton == null || cancelScanButton == null)
            return;

        if (!TryGetPointerDownPosition(out Vector2 screenPoint))
            return;

        if (IsScreenPointOverButton(confirmScanButton, screenPoint))
        {
            OnConfirmScanClicked();
            return;
        }

        if (IsScreenPointOverButton(cancelScanButton, screenPoint))
        {
            OnCancelScanClicked();
        }
    }

    private bool TryGetPointerDownPosition(out Vector2 screenPoint)
    {
        screenPoint = default;

#if ENABLE_INPUT_SYSTEM
        if (Touchscreen.current != null)
        {
            var primaryTouch = Touchscreen.current.primaryTouch;
            if (primaryTouch.press.wasPressedThisFrame)
            {
                screenPoint = primaryTouch.position.ReadValue();
                return true;
            }
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            screenPoint = Mouse.current.position.ReadValue();
            return true;
        }
#else
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                screenPoint = touch.position;
                return true;
            }

            return false;
        }

        if (Input.GetMouseButtonDown(0))
        {
            screenPoint = Input.mousePosition;
            return true;
        }
#endif

        return false;
    }

    private bool IsScreenPointOverButton(Button button, Vector2 screenPoint)
    {
        if (button == null || !button.isActiveAndEnabled || !button.gameObject.activeInHierarchy || !button.interactable)
            return false;

        RectTransform buttonRectTransform = button.transform as RectTransform;
        if (buttonRectTransform == null)
            return false;

        return RectTransformUtility.RectangleContainsScreenPoint(buttonRectTransform, screenPoint, GetUiEventCamera());
    }

    private Camera GetUiEventCamera()
    {
        if (confirmationCanvas != null && confirmationCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        if (confirmationCanvas != null && confirmationCanvas.worldCamera != null && confirmationCanvas.worldCamera.isActiveAndEnabled)
            return confirmationCanvas.worldCamera;

        if (Camera.main != null && Camera.main.isActiveAndEnabled)
            return Camera.main;

        Camera[] cameras = Camera.allCameras;
        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] != null && cameras[i].isActiveAndEnabled)
                return cameras[i];
        }

        return null;
    }

    private void ShowModel3D(VumarkActionEntry action)
    {
        ClearInstantiatedModel();

        if (action.modelPrefab == null)
        {
            Debug.LogWarning($"TranslateCards: ação ShowModel3D sem modelPrefab configurado para '{action.vumarkId}'.");
            return;
        }

        Transform anchor = modelDisplayAnchor != null ? modelDisplayAnchor : transform;

        instantiatedModelInstance = Instantiate(action.modelPrefab, anchor.position, anchor.rotation, anchor);

        // Aplica escala/deslocamento só nessa instância, sem tocar no prefab original
        // (que continua com os valores dele pra usar em outros lugares do jogo).
        instantiatedModelInstance.transform.localScale *= modelDisplayScaleMultiplier;
        instantiatedModelInstance.transform.localPosition += modelDisplayOffset;

        if (instantiatedModelInstance.GetComponent<ModelDragRotate>() == null)
        {
            instantiatedModelInstance.AddComponent<ModelDragRotate>();
        }

        // Carta sem texto: o modelo 3D é o conteúdo em si.
        SetCardText(string.Empty);
    }

    private void ClearInstantiatedModel()
    {
        if (instantiatedModelInstance == null)
            return;

        Destroy(instantiatedModelInstance);
        instantiatedModelInstance = null;
    }

    private void ExecuteVumarkAction(string vumarkId, bool forceNoHintsText = false)
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

        if (!actionDatabase.TryGetAction(vumarkId, out var action))
        {
            Debug.LogWarning("TranslateCards: ID de VuMark não mapeado: " + vumarkId);
            RegisterScan(vumarkId);
            return;
        }

        switch (action.actionType)
        {
            case VumarkActionType.ShowText:
                SetCardText(GetShowText(action, forceNoHintsText));
                break;

            case VumarkActionType.ShowRandomDebuff:
                SetCardText(ChooseRandomDebuff());
                break;

            case VumarkActionType.ShowModel3D:
                ShowModel3D(action);
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
                ResetConfirmationState(hideCardModel: true, clearText: true);
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
            Debug.LogWarning("TranslateCards: TMP_Text não encontrado em cardModelPrefab.");
            return;
        }

        cardText.text = text;
    }

    private void ResetConfirmationState(bool hideCardModel, bool clearText)
    {
        awaitingScanConfirmation = false;
        pendingConfirmationVumarkId = null;
        ShowConfirmationButtons(false);
        ClearInstantiatedModel();

        if (clearText)
        {
            SetCardText(string.Empty);
        }

        if (hideCardModel)
        {
            SetCardModelVisible(false);
        }
    }

    private void SetCardModelVisible(bool visible)
    {
        if (cardModelPrefab == null)
            return;

        if (cardModelPrefab.activeSelf != visible)
        {
            cardModelPrefab.SetActive(visible);
        }
    }

    private bool HasHintsAvailable()
    {
        if (mainGameController != null)
            return mainGameController.HasHints;

        return GameProgressStore.HasHints;
    }

    private bool TryConsumeHint()
    {
        if (mainGameController != null)
            return mainGameController.TryConsumeHint();

        return GameProgressStore.TryConsumeHint();
    }

    private bool IsVumarkAlreadyScanned(string vumarkId)
    {
        if (mainGameController != null)
            return mainGameController.IsVumarkAlreadyScanned(vumarkId);

        return GameProgressStore.IsVumarkAlreadyScanned(vumarkId);
    }

    private void MarkVumarkAsScanned(string vumarkId)
    {
        if (mainGameController != null)
        {
            mainGameController.MarkVumarkAsScanned(vumarkId);
            return;
        }

        GameProgressStore.MarkVumarkAsScanned(vumarkId);
    }

    private string GetNoHintsMessage()
    {
        return mainGameController != null
            ? mainGameController.NoHintsMessage
            : "Você não tem mais dicas disponíveis.";
    }

    private void ShowTextWithoutHintsIfPossible(string vumarkId)
    {
        if (actionDatabase != null &&
            actionDatabase.TryGetAction(vumarkId, out var action) &&
            action.actionType == VumarkActionType.ShowText)
        {
            string originalText = GetShowText(action, forceNoHintsText: true);
            SetCardText(GenerateEncryptedText(originalText));
            return;
        }

        SetCardText(GetNoHintsMessage());
    }

    // Conjunto de caracteres usados para "criptografar" o texto quando as dicas acabam.
    private static readonly char[] EncryptionCharset = { '#', '@', '!', '%', '$', '&', '*', '¨' };

    private string GenerateEncryptedText(string source)
    {
        if (string.IsNullOrEmpty(source))
            return source;

        var sb = new System.Text.StringBuilder(source.Length);

        foreach (char c in source)
        {
            // Preserva espaços para manter a "forma" do texto original, mas embaralha o resto.
            sb.Append(char.IsWhiteSpace(c)
                ? c
                : EncryptionCharset[UnityEngine.Random.Range(0, EncryptionCharset.Length)]);
        }

        return sb.ToString();
    }

    private string GetShowText(VumarkActionEntry action, bool forceNoHintsText)
    {
        if (forceNoHintsText && !string.IsNullOrWhiteSpace(action.textNoHints))
            return action.textNoHints;

        return action.text;
    }

    private string ChooseRandomDebuff()
    {
        var debuffs = new List<string>
        {
            "Congratulatuons! The battery has one more help point!",
            "Bug detected! Um glitch impediu a ativacao do efeito desta vez...",
            "You destroyed one bug! Remove one bug from The board!",
            "You destroyed one bug! Remove one bug from The board!",
        };
        int randomIndex = UnityEngine.Random.Range(0, debuffs.Count);
        string chosenDebuff = debuffs[randomIndex];

        if (chosenDebuff == "Congratulatuons! The battery has one more help point!")
        {
            AddHint();
        }

        return chosenDebuff;
    }

    private void AddHint()
    {
        if (mainGameController != null)
        {
            mainGameController.AddHint();
            return;
        }

        GameProgressStore.AddHint();
    }
}