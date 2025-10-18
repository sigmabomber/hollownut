using UnityEngine;

public class ResetVideoSettings : MonoBehaviour
{

    [Header("Optional Component References")]
    public ResolutionSettings videoSettings;
    public FullScreenSettings fullScreenSettings;
    public VSyncSettings vSyncSettings;
    public void ResetClicked()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentSettings != null)
        {
            GameManager.Instance.CurrentSettings.ResetVideoSettings();
            RefreshAllVideoUI();
        }
        else
        {
            Debug.LogWarning("GameManager or CurrentSettings not available");
        }
    }

    private void RefreshAllVideoUI()
    {
        // Refresh video settings UI
        if (videoSettings != null)
        {
            // Get the default resolution
            Resolution defaultResolution = new Resolution();
            defaultResolution.width = 1920;
            defaultResolution.height = 1080;

            // Create refresh rate for default
            RefreshRate refreshRate = new RefreshRate();
            refreshRate.numerator = 60;
            refreshRate.denominator = 1;
            defaultResolution.refreshRateRatio = refreshRate;
            videoSettings.currentResolutionIndex = 15;
            videoSettings.selectedRes = defaultResolution;
            videoSettings.currentRes = defaultResolution;
            videoSettings.UpdateResolutionText();
            videoSettings.UpdateApplyButtonVisibility();
        }

        // Refresh fullscreen settings UI
        if (fullScreenSettings != null)
        {
            
            fullScreenSettings.selectedFullscreenMode = FullScreenMode.FullScreenWindow;
            fullScreenSettings.currentFullscreenMode = FullScreenMode.FullScreenWindow;
            fullScreenSettings.UpdateFullscreenText();
            fullScreenSettings.UpdateApplyButtonVisibility();
        }

        // Refresh VSync settings UI
        if (vSyncSettings != null)
        {
            vSyncSettings.selectedVSync = true;
            vSyncSettings.currentVSync = true;
            vSyncSettings.UpdateVSyncText();
            vSyncSettings.UpdateApplyButtonVisibility();
        }

       
    }
}
