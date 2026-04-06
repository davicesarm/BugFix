using System.Collections;
using UnityEngine;

public class ScalePopIn : MonoBehaviour
{
    public Vector3 startScale = Vector3.zero;
    public Vector3 targetScale = Vector3.one;
    public float duration = 0.4f;

    void OnEnable()
    {
        StopAllCoroutines();
        StartCoroutine(AnimateScale());
    }

    IEnumerator AnimateScale()
    {
        float elapsed = 0f;
        transform.localScale = startScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            t = EaseOutBack(t);

            transform.localScale = Vector3.LerpUnclamped(startScale, targetScale, t);
            yield return null;
        }

        transform.localScale = targetScale;
    }

    float EaseOutBack(float x)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;

        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }
}