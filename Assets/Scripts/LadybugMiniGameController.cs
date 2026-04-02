using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

public class LadybugMiniGameController : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Transform ladybugTransform;

    [SerializeField]
    private TMP_Text questionText;

    [SerializeField]
    private TMP_Text[] answerTexts = new TMP_Text[3];

    [SerializeField]
    private Button[] answerButtons = new Button[3];

    [SerializeField]
    private Image[] lifeHearts;

    [SerializeField]
    private GameObject correctFeedbackObject;

    [SerializeField]
    private GameObject wrongFeedbackObject;

    [SerializeField]
    private GameObject gameOverObject;

    [SerializeField]
    private TMP_Text gameOverText;

    [Header("Content")]
    [SerializeField]
    private MiniGameQuestionBank questionBank;

    [SerializeField]
    private string[] sharedAnswers = new string[3]
    {
        "Alternativa A",
        "Alternativa B",
        "Alternativa C"
    };

    [Header("Round Rules")]
    [SerializeField]
    private int maxLives = 3;

    [SerializeField]
    private string gameOverMessage = "Fim de rodada!";

    [Header("Ladybug Movement")]
    [SerializeField]
    private bool useLocalPosition = true;

    [SerializeField]
    [FormerlySerializedAs("topY")]
    private float topYOffset = 0f;

    [SerializeField]
    [FormerlySerializedAs("bottomY")]
    private float bottomYOffset = -2.2f;

    [SerializeField]
    private float startFallDurationSeconds = 12f;

    [SerializeField]
    private bool reduceFallDurationPerQuestion = true;

    [SerializeField]
    private float fallDurationDecreasePerQuestion = 0.1f;

    [SerializeField]
    private float minFallDurationSeconds = 6f;

    [SerializeField]
    private float riseAmountNormalizedOnCorrect = 0.25f;

    [SerializeField]
    private float riseAnimationSeconds = 0.35f;

    [SerializeField]
    private float resetToTopAnimationSeconds = 0.45f;

    [Header("Feedback")]
    [SerializeField]
    private float feedbackDurationSeconds = 0.6f;

    private int currentLives;
    private float currentFallDuration;
    private float fallElapsed;
    private float movementStartY;
    private float movementTopY;
    private float movementBottomY;
    private bool isPaused;
    private bool isGameOver;
    private bool questionLoaded;

    private MiniGameQuestionEntry currentQuestion;
    private readonly List<int> questionBag = new();

    private void Start()
    {
        ConfigureAnswerButtons();
        InitializeRound();
    }

    public void OnAnswerSelected(int answerIndex)
    {
        if (isGameOver || isPaused || !questionLoaded)
        {
            Debug.LogWarning($"LadybugMiniGameController: clique ignorado (gameOver={isGameOver}, paused={isPaused}, questionLoaded={questionLoaded}).");
            return;
        }

        bool isCorrect = currentQuestion != null && answerIndex == currentQuestion.correctAnswerIndex;
        StartCoroutine(HandleAnswerRoutine(isCorrect));
    }

    private void Update()
    {
        if (isGameOver || isPaused || !questionLoaded)
            return;

        if (currentFallDuration <= 0f)
            return;

        fallElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(fallElapsed / currentFallDuration);
        SetLadybugY(GetLadybugYAtNormalized(t));

        if (t >= 1f)
        {
            LoseLife();
        }
    }

    private void InitializeRound()
    {
        ConfigureMovementBounds();

        currentLives = Mathf.Max(1, maxLives);
        currentFallDuration = Mathf.Max(0.5f, startFallDurationSeconds);
        fallElapsed = 0f;
        isPaused = false;
        isGameOver = false;
        questionLoaded = false;

        SetFeedback(false, false);
        SetGameOver(false);
        SetLadybugY(movementTopY);
        UpdateLifeUI();
        UpdateAnswerLabels();

        BuildQuestionBag();
        LoadNextQuestionRandom();
    }

    private IEnumerator HandleAnswerRoutine(bool isCorrect)
    {
        isPaused = true;
        SetAnswerButtonsInteractable(false);
        SetFeedback(isCorrect, !isCorrect);

        yield return new WaitForSeconds(feedbackDurationSeconds);

        SetFeedback(false, false);

        if (isCorrect)
        {
            yield return RaiseLadybugAfterCorrect();
            LoadNextQuestionRandom();
            UpdateFallDurationAfterQuestion();
        }
        else
        {
            // Errou: continua caindo na mesma pergunta/posição.
        }

        SetAnswerButtonsInteractable(true);
        isPaused = false;
    }

    private IEnumerator RaiseLadybugAfterCorrect()
    {
        float currentNormalized = Mathf.Clamp01(fallElapsed / currentFallDuration);
        float targetNormalized = Mathf.Clamp01(currentNormalized - riseAmountNormalizedOnCorrect);

        float fromY = GetLadybugYAtNormalized(currentNormalized);
        float toY = GetLadybugYAtNormalized(targetNormalized);

        float elapsed = 0f;
        while (elapsed < riseAnimationSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / riseAnimationSeconds);
            SetLadybugY(Mathf.Lerp(fromY, toY, t));
            yield return null;
        }

        fallElapsed = targetNormalized * currentFallDuration;
    }

    private void LoseLife()
    {
        if (isGameOver)
            return;

        currentLives--;
        UpdateLifeUI();

        if (currentLives <= 0)
        {
            EndRound();
            return;
        }

        StartCoroutine(ResetAfterLifeLossRoutine());
    }

    private IEnumerator ResetAfterLifeLossRoutine()
    {
        isPaused = true;
        SetAnswerButtonsInteractable(false);

        float fromY = GetLadybugCurrentY();
        float elapsed = 0f;
        while (elapsed < resetToTopAnimationSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / resetToTopAnimationSeconds);
            SetLadybugY(Mathf.Lerp(fromY, movementTopY, t));
            yield return null;
        }

        fallElapsed = 0f;
        LoadNextQuestionRandom();

        SetAnswerButtonsInteractable(true);
        isPaused = false;
    }

    private void EndRound()
    {
        isGameOver = true;
        isPaused = true;
        SetAnswerButtonsInteractable(false);
        SetGameOver(true);
    }

    private void SetGameOver(bool enabled)
    {
        if (gameOverObject != null)
            gameOverObject.SetActive(enabled);

        if (enabled && gameOverText != null)
            gameOverText.text = gameOverMessage;
    }

    private void SetFeedback(bool showCorrect, bool showWrong)
    {
        if (correctFeedbackObject != null)
            correctFeedbackObject.SetActive(showCorrect);

        if (wrongFeedbackObject != null)
            wrongFeedbackObject.SetActive(showWrong);
    }

    private void SetAnswerButtonsInteractable(bool enabled)
    {
        if (answerButtons == null)
            return;

        foreach (var button in answerButtons)
        {
            if (button != null)
                button.interactable = enabled;
        }
    }

    private void UpdateLifeUI()
    {
        if (lifeHearts == null)
            return;

        for (int i = 0; i < lifeHearts.Length; i++)
        {
            if (lifeHearts[i] != null)
                lifeHearts[i].enabled = i < currentLives;
        }
    }

    private void UpdateAnswerLabels(MiniGameQuestionEntry question = null)
    {
        if (sharedAnswers == null)
            return;

        int count = Mathf.Min(sharedAnswers.Length, GetAnswerSlotCount());
        for (int i = 0; i < count; i++)
        {
            TMP_Text label = GetAnswerLabelForIndex(i);
            if (label != null)
                label.text = question != null
                    ? question.GetAnswerText(i, sharedAnswers[i])
                    : sharedAnswers[i];
        }
    }

    private int GetAnswerSlotCount()
    {
        int textCount = answerTexts != null ? answerTexts.Length : 0;
        int buttonCount = answerButtons != null ? answerButtons.Length : 0;
        return Mathf.Max(textCount, buttonCount);
    }

    private TMP_Text GetAnswerLabelForIndex(int index)
    {
        if (answerTexts != null && index >= 0 && index < answerTexts.Length && answerTexts[index] != null)
            return answerTexts[index];

        if (answerButtons != null && index >= 0 && index < answerButtons.Length && answerButtons[index] != null)
            return answerButtons[index].GetComponentInChildren<TMP_Text>(true);

        return null;
    }

    private TMP_Text[] BuildLabelsFromButtons()
    {
        if (answerButtons == null)
            return null;

        TMP_Text[] labels = new TMP_Text[answerButtons.Length];
        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (answerButtons[i] != null)
                labels[i] = answerButtons[i].GetComponentInChildren<TMP_Text>(true);
        }

        return labels;
    }

    private void ConfigureAnswerButtons()
    {
        if (answerButtons == null)
            return;

        for (int i = 0; i < answerButtons.Length; i++)
        {
            Button button = answerButtons[i];
            if (button == null)
                continue;

            // Se o botão já tem OnClick configurado no Inspector,
            // evitamos adicionar outro listener para não processar resposta em duplicidade.
            if (button.onClick.GetPersistentEventCount() > 0)
                continue;

            int answerIndex = i;
            button.onClick.AddListener(() => OnAnswerSelected(answerIndex));
        }
    }

    private void BuildQuestionBag()
    {
        questionBag.Clear();

        if (questionBank == null || questionBank.Questions == null)
            return;

        for (int i = 0; i < questionBank.Questions.Count; i++)
        {
            questionBag.Add(i);
        }

        ShuffleQuestionBag();
    }

    private void ShuffleQuestionBag()
    {
        for (int i = questionBag.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (questionBag[i], questionBag[j]) = (questionBag[j], questionBag[i]);
        }
    }

    private void LoadNextQuestionRandom()
    {
        if (questionBank == null || questionBank.Questions == null || questionBank.Questions.Count == 0)
        {
            questionLoaded = false;
            SetAnswerButtonsInteractable(false);
            if (questionText != null)
                questionText.text = "Sem perguntas no banco.";
            Debug.LogWarning("LadybugMiniGameController: questionBank está vazio ou não definido.");
            return;
        }

        if (questionBag.Count == 0)
        {
            BuildQuestionBag();
        }

        int nextQuestionIndex = questionBag[0];
        questionBag.RemoveAt(0);

        currentQuestion = questionBank.Questions[nextQuestionIndex];
        questionLoaded = currentQuestion != null;
        SetAnswerButtonsInteractable(questionLoaded);
        UpdateAnswerLabels(currentQuestion);

        if (questionText != null)
        {
            questionText.text = questionLoaded
                ? currentQuestion.questionText
                : "Pergunta inválida.";
        }
    }

    private void UpdateFallDurationAfterQuestion()
    {
        if (!reduceFallDurationPerQuestion)
            return;

        currentFallDuration = Mathf.Max(
            minFallDurationSeconds,
            currentFallDuration - Mathf.Abs(fallDurationDecreasePerQuestion)
        );
    }

    private void SetLadybugY(float y)
    {
        if (ladybugTransform == null)
            return;

        Vector3 pos = useLocalPosition ? ladybugTransform.localPosition : ladybugTransform.position;
        pos.y = y;
        if (useLocalPosition)
            ladybugTransform.localPosition = pos;
        else
            ladybugTransform.position = pos;
    }

    private float GetLadybugCurrentY()
    {
        if (ladybugTransform == null)
            return movementTopY;

        return useLocalPosition ? ladybugTransform.localPosition.y : ladybugTransform.position.y;
    }

    private float GetLadybugYAtNormalized(float normalized)
    {
        return Mathf.Lerp(movementTopY, movementBottomY, Mathf.Clamp01(normalized));
    }

    private void ConfigureMovementBounds()
    {
        movementStartY = GetLadybugCurrentY();
        movementTopY = movementStartY + topYOffset;
        movementBottomY = movementStartY + bottomYOffset;

        if (movementBottomY > movementTopY)
        {
            (movementTopY, movementBottomY) = (movementBottomY, movementTopY);
        }

        float distance = Mathf.Abs(movementTopY - movementBottomY);
        if (distance > 20f)
        {
            movementTopY = movementStartY;
            movementBottomY = movementStartY - 2.2f;
            Debug.LogWarning("LadybugMiniGameController: distância de queda muito alta; ajustado automaticamente para modo 3D.");
        }
    }
}
