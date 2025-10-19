using TMPro;
using UnityEngine;

public class FullScreenSettings : MonoBehaviour
{
    public TMP_Text fullscreenText;
    public GameObject applyFullscreenButton;

    public FullScreenMode currentFullscreenMode;
    public FullScreenMode selectedFullscreenMode;
    private bool isInitialized = false;

    void Start()
    {
        currentFullscreenMode = Screen.fullScreenMode;
        selectedFullscreenMode = currentFullscreenMode;

        UpdateFullscreenText();
        UpdateApplyButtonVisibility();

        StartCoroutine(InitializeWithSavedSettings());
    }

    private System.Collections.IEnumerator InitializeWithSavedSettings()
    {
        int maxAttempts = 10;
        int attempts = 0;

        while (GameManager.Instance == null && attempts < maxAttempts)
        {
            attempts++;
            yield return new WaitForSeconds(0.2f);
        }

        if (GameManager.Instance != null && GameManager.Instance.CurrentSettings != null)
        {
            selectedFullscreenMode = GameManager.Instance.CurrentSettings.FullscreenMode;
            currentFullscreenMode = selectedFullscreenMode;

        }

        UpdateFullscreenText();
        UpdateApplyButtonVisibility();
        isInitialized = true;
    }

    public void RightArrowClicked()
    {
        if (!isInitialized) return;

        selectedFullscreenMode = GetNextFullscreenMode(selectedFullscreenMode);
        UpdateFullscreenText();
        UpdateApplyButtonVisibility();
    }

    public void LeftArrowClicked()
    {
        if (!isInitialized) return;

        selectedFullscreenMode = GetPreviousFullscreenMode(selectedFullscreenMode);
        UpdateFullscreenText();
        UpdateApplyButtonVisibility();
    }

    public void ApplyFullscreenMode()
    {
        if (!isInitialized) return;

        if (GameManager.Instance != null && GameManager.Instance.CurrentSettings != null)
        {
            GameManager.Instance.CurrentSettings.SetFullscreenMode(selectedFullscreenMode);
        }
        else
        {
            Screen.fullScreenMode = selectedFullscreenMode;
        }

        currentFullscreenMode = selectedFullscreenMode;
        UpdateFullscreenText();
        UpdateApplyButtonVisibility();
;
    }

    public void UpdateFullscreenText()
    {
        if (fullscreenText != null)
        {
            fullscreenText.text = GetFullscreenDisplayName(selectedFullscreenMode);

            if (selectedFullscreenMode == currentFullscreenMode)
            {
                fullscreenText.color = Color.white; 
            }
            else
            {
                fullscreenText.color = Color.yellow; 
            }
        }
    }

    public void UpdateApplyButtonVisibility()
    {
        if (applyFullscreenButton != null)
        {
            bool hasFullscreenChanged = selectedFullscreenMode != currentFullscreenMode;
            applyFullscreenButton.SetActive(hasFullscreenChanged);
        }
    }

    private FullScreenMode GetNextFullscreenMode(FullScreenMode currentMode)
    {
        return currentMode switch
        {
            FullScreenMode.ExclusiveFullScreen => FullScreenMode.FullScreenWindow,
            FullScreenMode.FullScreenWindow => FullScreenMode.Windowed,
            FullScreenMode.Windowed => FullScreenMode.MaximizedWindow,
            FullScreenMode.MaximizedWindow => FullScreenMode.ExclusiveFullScreen,
            _ => FullScreenMode.FullScreenWindow
        };
    }

    private FullScreenMode GetPreviousFullscreenMode(FullScreenMode currentMode)
    {
        return currentMode switch
        {
            FullScreenMode.ExclusiveFullScreen => FullScreenMode.MaximizedWindow,
            FullScreenMode.FullScreenWindow => FullScreenMode.ExclusiveFullScreen,
            FullScreenMode.Windowed => FullScreenMode.FullScreenWindow,
            FullScreenMode.MaximizedWindow => FullScreenMode.Windowed,
            _ => FullScreenMode.FullScreenWindow
        };
    }

    private string GetFullscreenDisplayName(FullScreenMode mode)
    {
        return mode switch
        {
            FullScreenMode.ExclusiveFullScreen => "Fullscreen",
            FullScreenMode.FullScreenWindow => "Borderless",
            FullScreenMode.MaximizedWindow => "Maximized",
            FullScreenMode.Windowed => "Windowed",
            _ => "Borderless"
        };
    }

    public bool IsReady()
    {
        return isInitialized;
    }

    public void RefreshSettings()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentSettings != null)
        {
            selectedFullscreenMode = GameManager.Instance.CurrentSettings.FullscreenMode;
            currentFullscreenMode = selectedFullscreenMode;
        }
        else
        {
            currentFullscreenMode = Screen.fullScreenMode;
            selectedFullscreenMode = currentFullscreenMode;
        }

        UpdateFullscreenText();
        UpdateApplyButtonVisibility();
    }

    public string GetCurrentFullscreenDescription()
    {
        return GetFullscreenDisplayName(currentFullscreenMode);
    }

    public string GetSelectedFullscreenDescription()
    {
        return GetFullscreenDisplayName(selectedFullscreenMode);
    }
}