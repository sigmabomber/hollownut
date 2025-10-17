using UnityEngine;
using System.Collections;

public class RespawnUI : MonoBehaviour
{
    public Transform respawnUI;
    public float fadeInDuration = 0.35f;
    public float fadeOutDuration = 0.4f;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = respawnUI.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = respawnUI.gameObject.AddComponent<CanvasGroup>();
    }

    public IEnumerator FadeIn()
    {
        float elapsedTime = 0f;

        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsedTime / fadeInDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;

        yield return true;
    }

    public IEnumerator FadeOut()
    {
        float elapsedTime = 0f;

        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(elapsedTime / fadeOutDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;

        yield return true;
    }
}
