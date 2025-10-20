using UnityEngine;

public class ResetVideoSettings : MonoBehaviour
{

    public ResolutionSettings videoSettings;
    public FullScreenSettings fullScreenSettings;
    public VSyncSettings vSyncSettings;


    public BaseUI menuUI;
    public void ResetClicked()
    {

        SoundManager.Instance.PlaySFX("Click");
        if (GameManager.Instance?.CurrentSettings != null)
        {
            GameManager.Instance.CurrentSettings.ResetVideoSettings();

            RefreshAllVideoUI();
        }
    }


    private void RefreshAllVideoUI()
    {
        if (GameManager.Instance?.CurrentSettings == null) return;

        SettingsData settings = GameManager.Instance.CurrentSettings;

        if (videoSettings != null)
        {
            Resolution currentResolution = new Resolution();
            currentResolution.width = settings.ScreenWidth;
            currentResolution.height = settings.ScreenHeight;

            RefreshRate refreshRate = new RefreshRate();
            refreshRate.numerator = (uint)settings.TargetFrameRate;
            refreshRate.denominator = 1;
            currentResolution.refreshRateRatio = refreshRate;

            videoSettings.selectedRes = currentResolution;
            videoSettings.currentRes = currentResolution;

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

        SoundManager.Instance.PlaySFX("Click");
        UIManager.Instance.OpenUI(menuUI);
    }
}
