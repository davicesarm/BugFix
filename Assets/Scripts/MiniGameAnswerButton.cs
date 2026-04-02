using UnityEngine;

public class MiniGameAnswerButton : MonoBehaviour
{
    [SerializeField]
    private LadybugMiniGameController controller;

    [SerializeField]
    [Range(0, 2)]
    private int answerIndex;

    public void SubmitAnswer()
    {
        if (controller == null)
        {
            Debug.LogWarning("MiniGameAnswerButton: controller não definido.");
            return;
        }

        controller.OnAnswerSelected(answerIndex);
    }
}
