using UnityEngine;
using Vuforia;
using System.Collections.Generic;
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
    private string stabilizingCandidateId;

    private float lastScanTime;

    private bool isSceneLoading;
    private bool awaitingScanConfirmation;

    private int stabilizingFrameCount;

    private Canvas confirmationCanvas;
    private CanvasGroup confirmationCanvasGroup;

    private static readonly char[] EncryptionCharset =
    {
        '#',
        '@',
        '!',
        '%',
        '$',
        '&',
        '*',
        '¨'
    };

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
            Debug.LogError(
                "TranslateCards: objeto sem filho para cardModelPrefab."
            );
        }

        if (!TryGetComponent<VuMarkBehaviour>(out vuMarkBehaviour))
        {
            Debug.LogError(
                "TranslateCards: VuMarkBehaviour não encontrado no mesmo GameObject."
            );
        }

        mainGameController = FindAnyObjectByType<MainGameController>();

        ConfigureConfirmationButtons();
        CacheConfirmationUiReferences();
        EnsureUiInteractionSetup();

        ResetConfirmationState(
            hideCardModel: true,
            clearText: true
        );
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

        ResetConfirmationState(
            hideCardModel: true,
            clearText: true
        );
    }

    private void Update()
    {
        HandleContinuousTrackingCheck();
        HandleManualButtonFallbackClick();
    }

    private void HandleContinuousTrackingCheck()
    {
        bool hasValidPose =
            vuMarkBehaviour != null &&
            vuMarkBehaviour.TargetStatus.Status == Status.TRACKED;

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

        string activeId = awaitingScanConfirmation
            ? pendingConfirmationVumarkId
            : lastScannedVumarkId;

        if (currentId == activeId)
        {
            stabilizingCandidateId = null;
            stabilizingFrameCount = 0;
            return;
        }

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
        {
            return;
        }

        stabilizingCandidateId = null;
        stabilizingFrameCount = 0;

        ResetConfirmationState(
            hideCardModel: false,
            clearText: true
        );

        lastScannedVumarkId = null;

        TryStartScanConfirmation(currentId);
    }

    private void HandleCardNoLongerVisible()
    {
        stabilizingCandidateId = null;
        stabilizingFrameCount = 0;

        if (cardModelPrefab == null || !cardModelPrefab.activeSelf)
        {
            return;
        }

        ResetConfirmationState(
            hideCardModel: true,
            clearText: true
        );

        lastScannedVumarkId = null;
    }

    protected override void OnTrackingFound()
    {
        base.OnTrackingFound();

        SetCardModelVisible(true);
    }

    protected override void OnTrackingLost()
    {
        base.OnTrackingLost();

        stabilizingCandidateId = null;
        stabilizingFrameCount = 0;

        ResetConfirmationState(
            hideCardModel: true,
            clearText: true
        );

        isSceneLoading = false;
        lastScannedVumarkId = null;
    }
// TODO: Refatorar esta lógica depois. Padronizar o JSON de todas as cartas com tipo/ação explícitos para 
//  melhorar a performance do fluxo de leitura.
   private void TryStartScanConfirmation(string vumarkId)
{
    if (awaitingScanConfirmation)
    {
        return;
    }

    if (IsRepeatedScan(vumarkId))
    {
        return;
    }

    if (mainGameController == null)
    {
        mainGameController =
            FindAnyObjectByType<MainGameController>();
    }

    if (
        actionDatabase != null &&
        actionDatabase.TryGetAction(
            vumarkId,
            out var pendingAction
        )
    )
    {
        if (
            pendingAction.actionType ==
            VumarkActionType.ShowRandomDebuff
        )
        {
            MarkVumarkAsScanned(vumarkId);

            ExecuteVumarkAction(
                vumarkId,
                forceNoHintsText: false
            );

            return;
        }

        if (
            pendingAction.actionType ==
            VumarkActionType.RedirectMinigame
        )
        {
            MarkVumarkAsScanned(vumarkId);

            ExecuteVumarkAction(
                vumarkId,
                forceNoHintsText: false
            );

            return;
        }
    }

    if (!HasHintsAvailable())
    {
        ShowTextWithoutHintsIfPossible(vumarkId);

        RegisterScan(vumarkId);

        ShowConfirmationButtons(false);

        return;
    }

    bool isAlreadyScanned =
        IsVumarkAlreadyScanned(vumarkId);

    if (isAlreadyScanned)
    {
        MarkVumarkAsScanned(vumarkId);

        ExecuteVumarkAction(
            vumarkId,
            forceNoHintsText: false
        );

        return;
    }

    if (
        confirmScanButton == null ||
        cancelScanButton == null
    )
    {
        Debug.LogWarning(
            "TranslateCards: Botões de confirmação não configurados no Inspector."
        );

        return;
    }

    pendingConfirmationVumarkId = vumarkId;
    awaitingScanConfirmation = true;

    SetCardText(confirmationMessage);
    ShowConfirmationButtons(true);
}
    private void OnConfirmScanClicked()
    {
        Debug.Log(
            "TranslateCards: botão Confirmar clicado."
        );

        if (
            !awaitingScanConfirmation ||
            string.IsNullOrWhiteSpace(
                pendingConfirmationVumarkId
            )
        )
        {
            return;
        }

        string vumarkId =
            pendingConfirmationVumarkId;

        if (!TryConsumeHint())
        {
            ShowTextWithoutHintsIfPossible(vumarkId);

            CancelPendingConfirmationLocally(
                clearText: false,
                hideCardModel: false
            );

            RegisterScan(vumarkId);

            return;
        }

        Debug.Log(
            $"TranslateCards: Dica consumida para o VuMark '{vumarkId}'."
        );

        MarkVumarkAsScanned(vumarkId);

        CancelPendingConfirmationLocally(
            clearText: true,
            hideCardModel: false
        );

        bool ficaramSemDicas =
            !HasHintsAvailable();

        ExecuteVumarkAction(
            vumarkId,
            forceNoHintsText: ficaramSemDicas
        );
    }

    private void OnCancelScanClicked()
    {
        Debug.Log(
            "TranslateCards: botão Cancelar clicado."
        );

        CancelPendingConfirmationLocally(
            clearText: true,
            hideCardModel: true
        );
    }

    private void CancelPendingConfirmationLocally(
        bool clearText,
        bool hideCardModel
    )
    {
        ResetConfirmationState(
            hideCardModel,
            clearText
        );
    }

    private void ConfigureConfirmationButtons()
    {
        if (confirmScanButton != null)
        {
            confirmScanButton.onClick.RemoveListener(
                OnConfirmScanClicked
            );

            confirmScanButton.onClick.AddListener(
                OnConfirmScanClicked
            );
        }

        if (cancelScanButton != null)
        {
            cancelScanButton.onClick.RemoveListener(
                OnCancelScanClicked
            );

            cancelScanButton.onClick.AddListener(
                OnCancelScanClicked
            );
        }
    }

    private void UnregisterConfirmationButtons()
    {
        if (confirmScanButton != null)
        {
            confirmScanButton.onClick.RemoveListener(
                OnConfirmScanClicked
            );
        }

        if (cancelScanButton != null)
        {
            cancelScanButton.onClick.RemoveListener(
                OnCancelScanClicked
            );
        }
    }

    private void ShowConfirmationButtons(bool visible)
    {
        EnsureCanvasEventCamera();

        if (confirmationCanvasGroup != null)
        {
            confirmationCanvasGroup.interactable =
                visible;

            confirmationCanvasGroup.blocksRaycasts =
                visible;
        }

        if (confirmScanButton != null)
        {
            confirmScanButton.interactable =
                visible;
        }

        if (cancelScanButton != null)
        {
            cancelScanButton.interactable =
                visible;
        }

        if (confirmationButtonsContainer != null)
        {
            confirmationButtonsContainer.SetActive(
                visible
            );

            return;
        }

        if (confirmScanButton != null)
        {
            confirmScanButton.gameObject.SetActive(
                visible
            );
        }

        if (cancelScanButton != null)
        {
            cancelScanButton.gameObject.SetActive(
                visible
            );
        }
    }

    private void CacheConfirmationUiReferences()
    {
        if (confirmationButtonsContainer != null)
        {
            confirmationCanvas =
                confirmationButtonsContainer
                    .GetComponentInParent<Canvas>(true);

            confirmationCanvasGroup =
                confirmationButtonsContainer
                    .GetComponent<CanvasGroup>();

            return;
        }

        if (confirmScanButton != null)
        {
            confirmationCanvas =
                confirmScanButton
                    .GetComponentInParent<Canvas>(true);
        }
        else if (cancelScanButton != null)
        {
            confirmationCanvas =
                cancelScanButton
                    .GetComponentInParent<Canvas>(true);
        }
    }

    private void EnsureUiInteractionSetup()
    {
        if (EventSystem.current == null)
        {
            GameObject eventSystemObject =
                new GameObject(
                    "EventSystem",
                    typeof(EventSystem)
                );

#if ENABLE_INPUT_SYSTEM
            eventSystemObject.AddComponent<
                InputSystemUIInputModule
            >();
#else
            eventSystemObject.AddComponent<
                StandaloneInputModule
            >();
#endif

            DontDestroyOnLoad(eventSystemObject);

            Debug.LogWarning(
                "TranslateCards: EventSystem não encontrado na cena. Um EventSystem foi criado automaticamente."
            );
        }

        if (
            confirmationCanvas != null &&
            confirmationCanvas
                .GetComponent<GraphicRaycaster>() == null
        )
        {
            confirmationCanvas.gameObject
                .AddComponent<GraphicRaycaster>();

            Debug.LogWarning(
                "TranslateCards: GraphicRaycaster ausente no Canvas da carta. Foi adicionado automaticamente."
            );
        }

        EnsureCanvasEventCamera();
    }

    private void EnsureCanvasEventCamera()
    {
        if (
            confirmationCanvas == null ||
            confirmationCanvas.renderMode !=
            RenderMode.WorldSpace
        )
        {
            return;
        }

        Camera eventCamera =
            GetUiEventCamera();

        if (
            eventCamera != null &&
            confirmationCanvas.worldCamera !=
            eventCamera
        )
        {
            confirmationCanvas.worldCamera =
                eventCamera;
        }
    }

    private void HandleManualButtonFallbackClick()
    {
        if (!awaitingScanConfirmation)
        {
            return;
        }

        if (
            confirmScanButton == null ||
            cancelScanButton == null
        )
        {
            return;
        }

        if (
            !TryGetPointerDownPosition(
                out Vector2 screenPoint
            )
        )
        {
            return;
        }

        if (
            IsScreenPointOverButton(
                confirmScanButton,
                screenPoint
            )
        )
        {
            OnConfirmScanClicked();
            return;
        }

        if (
            IsScreenPointOverButton(
                cancelScanButton,
                screenPoint
            )
        )
        {
            OnCancelScanClicked();
        }
    }

    private bool TryGetPointerDownPosition(
        out Vector2 screenPoint
    )
    {
        screenPoint = default;

#if ENABLE_INPUT_SYSTEM
        if (Touchscreen.current != null)
        {
            var primaryTouch =
                Touchscreen.current.primaryTouch;

            if (
                primaryTouch.press
                    .wasPressedThisFrame
            )
            {
                screenPoint =
                    primaryTouch.position
                        .ReadValue();

                return true;
            }
        }

        if (
            Mouse.current != null &&
            Mouse.current.leftButton
                .wasPressedThisFrame
        )
        {
            screenPoint =
                Mouse.current.position
                    .ReadValue();

            return true;
        }
#else
        if (Input.touchCount > 0)
        {
            Touch touch =
                Input.GetTouch(0);

            if (
                touch.phase ==
                TouchPhase.Began
            )
            {
                screenPoint =
                    touch.position;

                return true;
            }

            return false;
        }

        if (Input.GetMouseButtonDown(0))
        {
            screenPoint =
                Input.mousePosition;

            return true;
        }
#endif

        return false;
    }

    private bool IsScreenPointOverButton(
        Button button,
        Vector2 screenPoint
    )
    {
        if (
            button == null ||
            !button.isActiveAndEnabled ||
            !button.gameObject.activeInHierarchy ||
            !button.interactable
        )
        {
            return false;
        }

        RectTransform buttonRectTransform =
            button.transform as RectTransform;

        if (buttonRectTransform == null)
        {
            return false;
        }

        return RectTransformUtility
            .RectangleContainsScreenPoint(
                buttonRectTransform,
                screenPoint,
                GetUiEventCamera()
            );
    }

    private Camera GetUiEventCamera()
    {
        if (
            confirmationCanvas != null &&
            confirmationCanvas.renderMode ==
            RenderMode.ScreenSpaceOverlay
        )
        {
            return null;
        }

        if (
            confirmationCanvas != null &&
            confirmationCanvas.worldCamera != null &&
            confirmationCanvas.worldCamera
                .isActiveAndEnabled
        )
        {
            return confirmationCanvas.worldCamera;
        }

        if (
            Camera.main != null &&
            Camera.main.isActiveAndEnabled
        )
        {
            return Camera.main;
        }

        Camera[] cameras =
            Camera.allCameras;

        for (int i = 0; i < cameras.Length; i++)
        {
            if (
                cameras[i] != null &&
                cameras[i].isActiveAndEnabled
            )
            {
                return cameras[i];
            }
        }

        return null;
    }

    private void ExecuteVumarkAction(
        string vumarkId,
        bool forceNoHintsText = false
    )
    {
        if (cardModelPrefab == null)
        {
            Debug.LogWarning(
                "TranslateCards: cardModelPrefab não está definido."
            );

            return;
        }

        if (actionDatabase == null)
        {
            Debug.LogWarning(
                "TranslateCards: actionDatabase não está definido no Inspector."
            );

            return;
        }

        if (
            !actionDatabase.TryGetAction(
                vumarkId,
                out var action
            )
        )
        {
            Debug.LogWarning(
                "TranslateCards: ID de VuMark não mapeado: " +
                vumarkId
            );

            RegisterScan(vumarkId);

            return;
        }

        switch (action.actionType)
        {
            case VumarkActionType.ShowText:
            {
                bool deveMostrarTextoSemDica =
                    forceNoHintsText ||
                    !HasHintsAvailable();

                string texto =
                    GetShowText(
                        action,
                        deveMostrarTextoSemDica
                    );

                SetCardText(texto);

                break;
            }

            case VumarkActionType.ShowRandomDebuff:
            {
                SetCardText(
                    ChooseRandomDebuff()
                );

                break;
            }

            case VumarkActionType.RedirectMinigame:
{
    if (string.IsNullOrWhiteSpace(action.sceneName))
    {
        Debug.LogError(
            $"TranslateCards: sceneName não configurado para o minigame. VuMark: {vumarkId}"
        );

        break;
    }

    if (!Application.CanStreamedLevelBeLoaded(action.sceneName))
    {
        Debug.LogError(
            $"TranslateCards: cena '{action.sceneName}' não está no Build Settings."
        );

        break;
    }

    if (isSceneLoading)
    {
        break;
    }

    isSceneLoading = true;

    ResetConfirmationState(
        hideCardModel: true,
        clearText: true
    );

    SceneManager.LoadScene(action.sceneName);

    break;
}

            case VumarkActionType.LoadScene:
            {
                if (
                    string.IsNullOrWhiteSpace(
                        action.sceneName
                    )
                )
                {
                    Debug.LogWarning(
                        "TranslateCards: sceneName vazio para LoadScene no VuMark: " +
                        vumarkId
                    );

                    break;
                }

                if (isSceneLoading)
                {
                    break;
                }

                if (
                    !Application
                        .CanStreamedLevelBeLoaded(
                            action.sceneName
                        )
                )
                {
                    Debug.LogError(
                        "TranslateCards: cena não está no Build Settings: " +
                        action.sceneName
                    );

                    break;
                }

                isSceneLoading = true;

                ResetConfirmationState(
                    hideCardModel: true,
                    clearText: true
                );

                SceneManager.LoadScene(
                    action.sceneName
                );

                break;
            }

            case VumarkActionType.None:
            default:
            {
                Debug.Log(
                    "VuMark sem ação: " +
                    vumarkId
                );

                break;
            }
        }

        RegisterScan(vumarkId);
    }

    private bool TryGetVumarkId(
        out string vumarkId
    )
    {
        vumarkId = null;

        if (vuMarkBehaviour == null)
        {
            Debug.LogWarning(
                "TranslateCards: VuMarkBehaviour não disponível."
            );

            return false;
        }

        vumarkId =
            vuMarkBehaviour.InstanceId.StringValue;

        if (
            string.IsNullOrWhiteSpace(
                vumarkId
            )
        )
        {
            return false;
        }

        return true;
    }

    private bool IsRepeatedScan(
        string vumarkId
    )
    {
        return
            lastScannedVumarkId == vumarkId &&
            Time.time - lastScanTime <
            repeatedScanCooldownSeconds;
    }

    private void RegisterScan(
        string vumarkId
    )
    {
        lastScannedVumarkId =
            vumarkId;

        lastScanTime =
            Time.time;
    }

    private void SetCardText(
        string text
    )
    {
        if (cardText == null)
        {
            Debug.LogWarning(
                "TranslateCards: TMP_Text não encontrado em cardModelPrefab."
            );

            return;
        }

        cardText.text =
            text;
    }

    private void ResetConfirmationState(
        bool hideCardModel,
        bool clearText
    )
    {
        awaitingScanConfirmation =
            false;

        pendingConfirmationVumarkId =
            null;

        ShowConfirmationButtons(false);

        if (clearText)
        {
            SetCardText(
                string.Empty
            );
        }

        if (hideCardModel)
        {
            SetCardModelVisible(false);
        }
    }

    private void SetCardModelVisible(
        bool visible
    )
    {
        if (cardModelPrefab == null)
        {
            return;
        }

        if (
            cardModelPrefab.activeSelf !=
            visible
        )
        {
            cardModelPrefab.SetActive(
                visible
            );
        }
    }

    private bool HasHintsAvailable()
    {
        if (mainGameController != null)
        {
            return mainGameController.HasHints;
        }

        return GameProgressStore.HasHints;
    }

    private bool TryConsumeHint()
    {
        if (mainGameController != null)
        {
            return mainGameController
                .TryConsumeHint();
        }

        return GameProgressStore
            .TryConsumeHint();
    }

    private bool IsVumarkAlreadyScanned(
        string vumarkId
    )
    {
        if (mainGameController != null)
        {
            return mainGameController
                .IsVumarkAlreadyScanned(
                    vumarkId
                );
        }

        return GameProgressStore
            .IsVumarkAlreadyScanned(
                vumarkId
            );
    }

    private void MarkVumarkAsScanned(
        string vumarkId
    )
    {
        if (mainGameController != null)
        {
            mainGameController
                .MarkVumarkAsScanned(
                    vumarkId
                );

            return;
        }

        GameProgressStore
            .MarkVumarkAsScanned(
                vumarkId
            );
    }

    private string GetNoHintsMessage()
    {
        return mainGameController != null
            ? mainGameController.NoHintsMessage
            : "Você não tem mais dicas disponíveis.";
    }

    private void ShowTextWithoutHintsIfPossible(
        string vumarkId
    )
    {
        if (
            actionDatabase != null &&
            actionDatabase.TryGetAction(
                vumarkId,
                out var action
            ) &&
            action.actionType ==
            VumarkActionType.ShowText
        )
        {
            string texto =
                GetShowText(
                    action,
                    true
                );

            SetCardText(texto);

            return;
        }

        SetCardText(
            GetNoHintsMessage()
        );
    }

    private string GetShowText(
    VumarkActionEntry action,
    bool forceNoHintsText
)
{
    if (!forceNoHintsText)
    {
        return action.text;
    }

    if (!string.IsNullOrWhiteSpace(action.textNoHints))
    {
        return action.textNoHints;
    }

    Debug.LogError(
        $"TranslateCards: texto_criptografado não encontrado para '{action.vumarkId}'."
    );

    return "ERRO: TEXTO CRIPTOGRAFADO NÃO CONFIGURADO";
}

    private string GenerateEncryptedText(
        string source
    )
    {
        if (string.IsNullOrEmpty(source))
        {
            return source;
        }

        var sb =
            new System.Text.StringBuilder(
                source.Length
            );

        foreach (char c in source)
        {
            if (char.IsWhiteSpace(c))
            {
                sb.Append(c);
                continue;
            }

            int index =
                UnityEngine.Random.Range(
                    0,
                    EncryptionCharset.Length
                );

            sb.Append(
                EncryptionCharset[index]
            );
        }

        return sb.ToString();
    }

    private string ChooseRandomDebuff()
    {
        var debuffs =
            new List<string>
            {
                "Congratulatuons! The battery has one more help point!",
                "Bug detected! Um glitch impediu a ativacao do efeito desta vez...",
                "You destroyed one bug! Remove one bug from The board!",
                "You destroyed one bug! Remove one bug from The board!"
            };

        int randomIndex =
            UnityEngine.Random.Range(
                0,
                debuffs.Count
            );

        string chosenDebuff =
            debuffs[randomIndex];

        if (
            chosenDebuff ==
            "Congratulatuons! The battery has one more help point!"
        )
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