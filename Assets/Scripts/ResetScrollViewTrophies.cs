using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ResetScrollViewTrophies : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;

    private void OnEnable()
    {
        StartCoroutine(ResetarScroll());
    }

    private IEnumerator ResetarScroll()
    {
        // Aguarda o Unity recalcular o tamanho e a posição do Content
        yield return null;

        Canvas.ForceUpdateCanvases();

        // 1 representa o topo; 0 representa a parte inferior
        scrollRect.verticalNormalizedPosition = 1f;

        Canvas.ForceUpdateCanvases();
    }
}