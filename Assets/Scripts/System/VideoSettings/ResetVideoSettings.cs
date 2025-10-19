using UnityEngine;

public class ResetVideoSettings : MonoBehaviour
{

    public ResolutionSettings videoSettings;
    public FullScreenSettings fullScreenSettings;
    public VSyncSettings vSyncSettings;


    public BaseUI menuUI;
    public void ResetClicked()
    {
        if (GameManager.Instance?.CurrentSettings != null)
        {
            // Reset the video settings
            GameManager.Instance.CurrentSettings.ResetVideoSettings();

            // MANUALLY refresh all video-related UI components
            RefreshAllVideoUI();
        }
    }


    private void RefreshAllVideoUI()
    {
        if (GameManager.Instance?.CurrentSettings == null) return;

        // Get the current settings
        SettingsData settings = GameManager.Instance.CurrentSettings;

        if (videoSettings != null)
        {
            // Create resolution from current settings
            Resolution currentResolution = new Resolution();
            currentResolution.width = settings.ScreenWidth;
            currentResolution.height = settings.ScreenHeight;

            RefreshRate refreshRate = new RefreshRate();
            refreshRate.numerator = (uint)settings.TargetFrameRate;
            refreshRate.denominator = 1;
            currentResolution.refreshRateRatio = refreshRate;

            // Update video settings with actual values
            videoSettings.selectedRes = currentResolution;
            videoSettings.currentRes = currentResolution;

            // Find the correct index in the available resolutions
            videoSettings.currentResolutionIndex = videoSettings.FindResolutionIndex(currentResolution);

            videoSettings.UpdateResolutionText();
            videoSettings.UpdateApplyButtonVisibility();

        }

        if (fullScreenSettings != null)
        {
            fullScreenSettings.selectedFullscreenMode = settings.FullscreenMode;
            fullScreenSettings.currentFullscreenMode = settings.FullscreenMode;
            fullScreenSettings.UpdateFullscreenText();
            fullScreenSettings.UpdateApplyButtonVisibility();

        }

        if (vSyncSettings != null)
        {
            vSyncSettings.selectedVSync = settings.VSync;
            vSyncSettings.currentVSync = settings.VSync;
            vSyncSettings.UpdateVSyncText();
            vSyncSettings.UpdateApplyButtonVisibility();

        }

    }


    public void BackClicked()
    {
        UIManager.Instance.OpenUI(menuUI);
    }
}
