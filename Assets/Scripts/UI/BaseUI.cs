using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public enum UIType { Exclusive, NonExclusive }

public class BaseUI : MonoBehaviour
{
    public UIType uiType;
    public CanvasGroup canvasGroup; // assign in inspector
    public float fadeInAlpha = 1f;
    public float fadeOutAlpha = 0f;
    public float fadeDuration = 0.5f; // duration of fade in/out

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
    }

    public void Open()
    {
        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(Fade(fadeInAlpha));
    }

    public void Close()
    {
        StopAllCoroutines();
        StartCoroutine(Fade(fadeOutAlpha, () => gameObject.SetActive(false)));
    }

    private IEnumerator Fade(float targetAlpha, System.Action onComplete = null)
    {
        float startAlpha = canvasGroup.alpha;
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        onComplete?.Invoke();
    }
}
