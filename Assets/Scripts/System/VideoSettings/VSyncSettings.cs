using TMPro;
using UnityEngine;

public class VSyncSettings : MonoBehaviour
{
    public TMP_Text vsyncText;
    public GameObject applyVSyncButton;

    public bool currentVSync;
    public bool selectedVSync;
    private bool isInitialized = false;

    void Start()
    {
        currentVSync = QualitySettings.vSyncCount > 0;
        selectedVSync = currentVSync;

        UpdateVSyncText();
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
            selectedVSync = GameManager.Instance.CurrentSettings.VSync;
            currentVSync = selectedVSync;

        }

        UpdateVSyncText();
        UpdateApplyButtonVisibility();
        isInitialized = true;
    }

    public void RightArrowClicked()
    {
        if (!isInitialized) return;

        // Toggle to next VSync state
        selectedVSync = !selectedVSync;
        UpdateVSyncText();
        UpdateApplyButtonVisibility();
    }

    public void LeftArrowClicked()
    {
        if (!isInitialized) return;

        selectedVSync = !selectedVSync;
        UpdateVSyncText();
        UpdateApplyButtonVisibility();
    }

    public void ApplyVSync()
    {
        if (!isInitialized) return;

        if (GameManager.Instance != null && GameManager.Instance.CurrentSettings != null)
        {
            GameManager.Instance.CurrentSettings.SetVSync(selectedVSync);
        }
        else
        {
            QualitySettings.vSyncCount = selectedVSync ? 1 : 0;

            if (!selectedVSync)
            {
                Application.targetFrameRate = GameManager.Instance?.CurrentSettings?.TargetFrameRate ?? 60;
            }
        }

        currentVSync = selectedVSync;
        UpdateVSyncText();
        UpdateApplyButtonVisibility();

    }

    public void UpdateVSyncText()
    {
        if (vsyncText != null)
        {
            vsyncText.text = GetVSyncDisplayName(selectedVSync);

            if (selectedVSync == currentVSync)
            {
                vsyncText.color = Color.white; 
            }
            else
            {
                vsyncText.color = Color.yellow; 
            }
        }
    }

    public void UpdateApplyButtonVisibility()
    {
        if (applyVSyncButton != null)
        {
            bool hasVSyncChanged = selectedVSync != currentVSync;
            applyVSyncButton.SetActive(hasVSyncChanged);
        }
    }

    private string GetVSyncDisplayName(bool vsyncEnabled)
    {
        return vsyncEnabled ? "On" : "Off";
    }

    public bool IsReady()
    {
        return isInitialized;
    }

    public void RefreshSettings()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentSettings != null)
        {
            selectedVSync = GameManager.Instance.CurrentSettings.VSync;
            currentVSync = selectedVSync;
        }
        else
        {
            currentVSync = QualitySettings.vSyncCount > 0;
            selectedVSync = currentVSync;
        }

        UpdateVSyncText();
        UpdateApplyButtonVisibility();
    }

    public string GetCurrentVSyncDescription()
    {
        return GetVSyncDisplayName(currentVSync);
    }

    public string GetSelectedVSyncDescription()
    {
        return GetVSyncDisplayName(selectedVSync);
    }

    public bool GetCurrentVSyncState()
    {
        return currentVSync;
    }

    public bool GetSelectedVSyncState()
    {
        return selectedVSync;
    }
}