using UnityEngine;

public class CloseTrophyPreview : MonoBehaviour
{
    public GameObject displayPanel;
    public Transform displayPoint;

    public void CloseDisplay()
    {
        if (displayPanel != null)
            displayPanel.SetActive(false);

        if (displayPoint != null)
        {
            foreach (Transform child in displayPoint)
            {
                Destroy(child.gameObject);
            }
        }
    }
}