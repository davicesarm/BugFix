using TMPro;
using UnityEngine;

public class MainGameController : MonoBehaviour
{
    [Header("Hints")]
    [SerializeField]
    private int maxHints = 3;

    [SerializeField]
    private TMP_Text hintsText;

    [SerializeField]
    private string hintsTextFormat = "Dicas restantes: {0}";

    [SerializeField]
    private string noHintsMessage = "Você não tem mais dicas disponíveis.";

    public int RemainingHints => GameProgressStore.RemainingHints;

    public bool HasHints => GameProgressStore.HasHints;

    public string NoHintsMessage => noHintsMessage;

    private void Awake()
    {
        GameProgressStore.Initialize(maxHints);
        RefreshHintsText();
    }

    private void OnEnable()
    {
        RefreshHintsText();
    }

    public bool TryConsumeHint()
    {
        if (!GameProgressStore.TryConsumeHint())
        {
            Debug.LogWarning(noHintsMessage);
            RefreshHintsText();
            return false;
        }

        RefreshHintsText();

        return true;
    }

    public void AddHint(int amount = 1)
    {
        GameProgressStore.AddHint(amount);
        RefreshHintsText();
    }

    public bool IsVumarkAlreadyScanned(string vumarkId)
    {
        return GameProgressStore.IsVumarkAlreadyScanned(vumarkId);
    }

    public void MarkVumarkAsScanned(string vumarkId)
    {
        GameProgressStore.MarkVumarkAsScanned(vumarkId);
    }

    public void ResetProgressForTests()
    {
        GameProgressStore.ResetProgress(maxHints);
        RefreshHintsText();
        Debug.Log("MainGameController: progresso resetado para testes.");
    }

    private void RefreshHintsText()
    {
        if (hintsText == null)
            return;

        hintsText.text = string.Format(hintsTextFormat, RemainingHints);
    }
}