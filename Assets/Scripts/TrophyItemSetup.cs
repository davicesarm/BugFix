using UnityEngine;
using UnityEngine.UI;

public class TrophyItemSetup : MonoBehaviour
{
    public ShowTrophy showTrophy;
    public Trophy trophy;

    [Header("Estado bloqueado")]
    [Tooltip("Se vazio, tenta pegar automaticamente o Image do mesmo objeto.")]
    public Image iconImage;

    [Tooltip("Sprite mostrada no lugar do ícone quando o troféu ainda não foi desbloqueado (ex: silhueta, cadeado).")]
    public Sprite lockedSprite;

    [Tooltip("Cor aplicada ao ícone quando bloqueado (além/no lugar da lockedSprite).")]
    public Color lockedTint = new Color(0.35f, 0.35f, 0.35f, 1f);

    [Tooltip("Se vazio, tenta pegar automaticamente o Button do mesmo objeto.")]
    public Button button;

    private Sprite originalSprite;
    private Color originalColor = Color.white;

    void Start()
    {
        if (iconImage == null)
            iconImage = GetComponent<Image>();

        if (button == null)
            button = GetComponent<Button>();

        if (iconImage != null)
        {
            originalSprite = iconImage.sprite;
            originalColor = iconImage.color;
        }

        if (showTrophy != null && trophy != null)
        {
            showTrophy.SetTrophy(trophy);
        }

        RefreshLockState();
    }

    void OnEnable()
    {
        // Reavalia toda vez que a tela de troféus é reaberta, caso algo tenha sido
        // desbloqueado enquanto o painel estava fechado.
        if (iconImage != null || button != null)
        {
            RefreshLockState();
        }
    }

    private void RefreshLockState()
    {
        if (trophy == null || string.IsNullOrWhiteSpace(trophy.trophyId))
            return;

        bool isUnlocked = PlayerTrophies.Load().HasTrophy(trophy.trophyId);

        if (iconImage != null)
        {
            if (isUnlocked)
            {
                if (originalSprite != null)
                    iconImage.sprite = originalSprite;

                iconImage.color = originalColor;
            }
            else
            {
                if (lockedSprite != null)
                    iconImage.sprite = lockedSprite;

                iconImage.color = lockedTint;
            }
        }

        if (button != null)
        {
            button.interactable = isUnlocked;
        }
    }
}
