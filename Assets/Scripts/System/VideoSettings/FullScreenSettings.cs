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
        // Initialize with current screen settings
        currentFullscreenMode = Screen.fullScreenMode;
        selectedFullscreenMode = currentFullscreenMode;

        UpdateFullscreenText();
        UpdateApplyButtonVisibility();

        // Start coroutine to load saved settings
        StartCoroutine(InitializeWithSavedSettings());
    }

    private System.Collections.IEnumerator InitializeWithSavedSettings()
    {
        // Wait for GameManager to be ready
        int maxAttempts = 10;
        int attempts = 0;

        while (GameManager.Instance == null && attempts < maxAttempts)
        {
            attempts++;
            yield return new WaitForSeconds(0.2f);
        }

        if (GameManager.Instance != null && GameManager.Instance.CurrentSettings != null)
        {
            // Load saved fullscreen mode
            selectedFullscreenMode = GameManager.Instance.CurrentSettings.FullscreenMode;
            currentFullscreenMode = selectedFullscreenMode;

            Debug.Log($"Loaded saved fullscreen mode: {GetFullscreenDisplayName(selectedFullscreenMode)}");
        }

        UpdateFullscreenText();
        UpdateApplyButtonVisibility();
        isInitialized = true;
    }

    public void RightArrowClicked()
    {
        if (!isInitialized) return;

        // Cycle to next fullscreen mode
        selectedFullscreenMode = GetNextFullscreenMode(selectedFullscreenMode);
        UpdateFullscreenText();
        UpdateApplyButtonVisibility();
    }

    public void LeftArrowClicked()
    {
        if (!isInitialized) return;

        // Cycle to previous fullscreen mode
        selectedFullscreenMode = GetPreviousFullscreenMode(selectedFullscreenMode);
        UpdateFullscreenText();
        UpdateApplyButtonVisibility();
    }

    public void ApplyFullscreenMode()
    {
        if (!isInitialized) return;

        // Apply the selected fullscreen mode
        if (GameManager.Instance != null && GameManager.Instance.CurrentSettings != null)
        {
            GameManager.Instance.CurrentSettings.SetFullscreenMode(selectedFullscreenMode);
        }
        else
        {
            // Fallback: apply directly without GameManager
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

            // Visual feedback for current vs selected mode
            if (selectedFullscreenMode == currentFullscreenMode)
            {
                fullscreenText.color = Color.white; // Current mode
            }
            else
            {
                fullscreenText.color = Color.yellow; // Different mode selected
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