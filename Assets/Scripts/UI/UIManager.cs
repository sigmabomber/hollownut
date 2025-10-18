using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public BaseUI currentExclusiveUI;
    private List<BaseUI> activeNonExclusiveUIs = new List<BaseUI>();

    public BaseUI defaultUI;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        if (defaultUI != null && currentExclusiveUI == null)
        {
            currentExclusiveUI = defaultUI;
            if (defaultUI.canvasGroup == null)
                defaultUI.canvasGroup = defaultUI.GetComponent<CanvasGroup>();
            defaultUI.canvasGroup.alpha = defaultUI.fadeInAlpha;
        }
    }

    public void OpenUI(BaseUI ui)
    {
        if (ui == null) return;

        if (ui.uiType == UIType.Exclusive)
        {
            if (currentExclusiveUI != null && currentExclusiveUI != ui)
            {
                StartCoroutine(FadeOutAndDisable(currentExclusiveUI));
            }

            currentExclusiveUI = ui;
            StartCoroutine(FadeInUI(ui));
        }
        else
        {
            if (!activeNonExclusiveUIs.Contains(ui))
                activeNonExclusiveUIs.Add(ui);

            StartCoroutine(FadeInUI(ui));
        }
    }

    public void CloseUI(BaseUI ui)
    {
        if (ui == null) return;

        StartCoroutine(FadeOutAndDisable(ui));

        if (ui.uiType == UIType.Exclusive && currentExclusiveUI == ui)
        {
            currentExclusiveUI = null;

            if (defaultUI != null)
            {
                StartCoroutine(ShowDefaultUIDelayed(0.1f));
            }
        }

        if (ui.uiType == UIType.NonExclusive)
            activeNonExclusiveUIs.Remove(ui);
    }

    private IEnumerator ShowDefaultUIDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (currentExclusiveUI == null && defaultUI != null)
        {
            currentExclusiveUI = defaultUI;
            StartCoroutine(FadeInUI(defaultUI));
        }
    }

    public void CloseAllExclusive()
    {
        if (currentExclusiveUI != null && currentExclusiveUI != defaultUI)
        {
            StartCoroutine(FadeOutAndDisable(currentExclusiveUI));
            currentExclusiveUI = null;

            if (defaultUI != null)
            {
                StartCoroutine(ShowDefaultUIDelayed(0.1f));
            }
        }
    }

    private IEnumerator FadeInUI(BaseUI ui)
    {
        if (ui == null) yield break;


        if (ui.canvasGroup == null)
            ui.canvasGroup = ui.GetComponent<CanvasGroup>();

        float timer = 0f;
        float startAlpha = ui.canvasGroup.alpha;
        float targetAlpha = ui.fadeInAlpha;

        while (timer < ui.fadeDuration)
        {
            timer += Time.deltaTime;
            ui.canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / ui.fadeDuration);
            yield return null;
        }

        ui.canvasGroup.alpha = targetAlpha;
    }

    private IEnumerator FadeOutAndDisable(BaseUI ui)
    {
        if (ui == null) yield break;

        if (ui.canvasGroup == null)
            ui.canvasGroup = ui.GetComponent<CanvasGroup>();

        float timer = 0f;
        float startAlpha = ui.canvasGroup.alpha;
        float targetAlpha = ui.fadeOutAlpha;

        while (timer < ui.fadeDuration)
        {
            timer += Time.deltaTime;
            ui.canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / ui.fadeDuration);
            yield return null;
        }

        ui.canvasGroup.alpha = targetAlpha;

    }
}