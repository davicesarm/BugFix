using UnityEngine;
using UnityEngine.UI;

public class ShowTrophy : MonoBehaviour
{
    public Image icon;
    public Transform displayPoint;
    public GameObject displayPanel;

    public GameObject exitButton;

    [Header("Configuração visual do troféu")]
    public Vector3 modelRotation = new Vector3(0f, 180f, 0f);
    public float rotationSpeed = 50f;
    public float zoomDuration = 0.35f;

    private Trophy currentTrophy;
    private GameObject currentModel;

    public void SetTrophy(Trophy trophy)
    {
        currentTrophy = trophy;
    }

    public void OnClick()
    {
        if (currentTrophy == null || currentTrophy.model == null) return;

        if (displayPanel != null)
            displayPanel.SetActive(true);

        if (currentModel != null)
            Destroy(currentModel);

        if (exitButton != null)
            exitButton.SetActive(true);

        currentModel = Instantiate(currentTrophy.model, displayPoint);
        currentModel.transform.localPosition = Vector3.zero;
        currentModel.transform.localRotation = Quaternion.Euler(modelRotation);
        currentModel.transform.localScale = Vector3.zero;

        RotateTrophy rotate = currentModel.AddComponent<RotateTrophy>();
        rotate.rotationSpeed = rotationSpeed;

        ScalePopIn zoom = currentModel.AddComponent<ScalePopIn>();
        zoom.startScale = Vector3.zero;
        zoom.targetScale = new Vector3(1f, 1f, 1f);
        zoom.duration = zoomDuration;
    }
}