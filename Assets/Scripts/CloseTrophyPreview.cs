using UnityEngine;

public class CloseTrophyPreview : MonoBehaviour
{
    [SerializeField] private GameObject displayPanel;
    [SerializeField] private GameObject clickBlocker;
    [SerializeField] private GameObject exitButton;
    [SerializeField] private Transform displayPoint;

    public void CloseDisplay()
    {
        if (clickBlocker != null)
            clickBlocker.SetActive(false);

        if (exitButton != null)
            exitButton.SetActive(false);

        if (displayPanel != null)
            displayPanel.SetActive(false);

        if (displayPoint != null)
        {
            foreach (Transform child in displayPoint)
                Destroy(child.gameObject);
        }
    }
}