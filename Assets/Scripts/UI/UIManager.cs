using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public BaseUI currentExclusiveUI; 
    private List<BaseUI> activeNonExclusiveUIs = new List<BaseUI>(); 
    public List<BaseUI> defaultUIs = new List<BaseUI>();
    private bool isSwitchingExclusive = false; 

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        FindAllDefaultUIs();
        ToggleDefaultUIs(true);
    }

    public void OpenUI(BaseUI ui)
    {
        if (ui == null) return;

        if (ui.uiType == UIType.Exclusive)
        {
            if (currentExclusiveUI != null && currentExclusiveUI != ui)
            {
                StartCoroutine(SwitchExclusiveUI(ui));
            }
            else if (currentExclusiveUI == null)
            {
                currentExclusiveUI = ui;
                StartCoroutine(FadeInUI(ui));
                ToggleDefaultUIs(false);
            }
        }
        else
        {
            if (!activeNonExclusiveUIs.Contains(ui))
            {
                activeNonExclusiveUIs.Add(ui);
            }
            StartCoroutine(FadeInUI(ui));
        }
    }

    private IEnumerator SwitchExclusiveUI(BaseUI newUI)
    {
        if (isSwitchingExclusive) yield break; 

        isSwitchingExclusive = true;
        BaseUI oldUI = currentExclusiveUI;
        yield return StartCoroutine(FadeOutUI(oldUI));

        currentExclusiveUI = newUI;

        yield return StartCoroutine(FadeInUI(newUI));

        isSwitchingExclusive = false;
    }

    public void CloseUI(BaseUI ui)
    {
        if (ui == null) return;

        if (ui.uiType == UIType.Exclusive)
        {
            if (currentExclusiveUI == ui && !isSwitchingExclusive)
            {
                StartCoroutine(CloseExclusiveUI(ui));
            }
        }
        else
        {
            if (activeNonExclusiveUIs.Contains(ui))
            {
                StartCoroutine(FadeOutUI(ui));
                activeNonExclusiveUIs.Remove(ui);
            }
        }
    }

    private IEnumerator CloseExclusiveUI(BaseUI ui)
    {
        yield return StartCoroutine(FadeOutUI(ui));
        currentExclusiveUI = null;
        ToggleDefaultUIs(true);
    }

    private void FindAllDefaultUIs()
    {
        BaseUI[] allUIs = FindObjectsByType<BaseUI>(FindObjectsSortMode.None);
        foreach (BaseUI ui in allUIs)
        {
            if (ui.isDefaultUI)
            {
                defaultUIs.Add(ui);
            }
        }
    }

    public void ToggleDefaultUIs(bool toggle)
    {
        foreach (BaseUI defaultUI in defaultUIs)
        {
            if (defaultUI == currentExclusiveUI) continue;

            if (toggle)
            {
                StartCoroutine(FadeInUI(defaultUI));
            }
            else
            {
                StartCoroutine(FadeOutUI(defaultUI));
            }
        }
    }

    private IEnumerator FadeInUI(BaseUI ui)
    {
        if (ui == null) yield break;

        ui.gameObject.SetActive(true);

        if (ui.canvasGroup == null)
            ui.canvasGroup = ui.GetComponent<CanvasGroup>();

        float timer = 0f;
        float startAlpha = ui.canvasGroup.alpha;
        float targetAlpha = ui.fadeInAlpha;

        while (timer < ui.fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            ui.canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / ui.fadeDuration);
            yield return null;
        }

        ui.canvasGroup.alpha = targetAlpha;
    }

    private IEnumerator FadeOutUI(BaseUI ui)
    {
        if (ui == null) yield break;

        if (ui.canvasGroup == null)
            ui.canvasGroup = ui.GetComponent<CanvasGroup>();

        float timer = 0f;
        float startAlpha = ui.canvasGroup.alpha;
        float targetAlpha = ui.fadeOutAlpha;

        while (timer < ui.fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            ui.canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / ui.fadeDuration);
            yield return null;
        }

        ui.canvasGroup.alpha = targetAlpha;

     
        
            ui.gameObject.SetActive(false);
        
    }

    public bool IsUIOpen(BaseUI ui)
    {
        if (ui.uiType == UIType.Exclusive)
            return currentExclusiveUI == ui;
        else
            return activeNonExclusiveUIs.Contains(ui);
    }

    public BaseUI GetCurrentExclusiveUI()
    {
        return currentExclusiveUI;
    }

    public List<BaseUI> GetActiveNonExclusiveUIs()
    {
        return new List<BaseUI>(activeNonExclusiveUIs);
    }

    public bool IsSwitchingExclusive()
    {
        return isSwitchingExclusive;
    }
}