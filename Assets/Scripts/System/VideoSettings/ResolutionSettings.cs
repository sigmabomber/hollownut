using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ResolutionSettings : MonoBehaviour
{
    public List<Resolution> SystemRes = new();
    public Resolution selectedRes;
    public Resolution currentRes;

    public int currentResolutionIndex = 0;
    public TMP_Text resolutionText;

    public GameObject applyButton;

    private bool isInitialized = false;

    void Start()
    {
        Resolution[] availableResolutions = Screen.resolutions;
        SystemRes = FilterResolutions(availableResolutions);
        currentRes = Screen.currentResolution;

        currentResolutionIndex = FindResolutionIndex(currentRes);
        selectedRes = currentRes;
        UpdateResolutionText();

        StartCoroutine(InitializeWithSavedSettings());
    }

    IEnumerator InitializeWithSavedSettings()
    {
        int maxAttempts = 10;
        int attempts = 0;

        while (GameManager.Instance == null && attempts < maxAttempts)
        {
            attempts++;
            yield return new WaitForSeconds(0.2f);
        }

        if (GameManager.Instance == null)
        {
            FinishInitialization();
            yield break;
        }

        attempts = 0;
        while (GameManager.Instance.CurrentSettings == null && attempts < maxAttempts)
        {
            attempts++;
            yield return new WaitForSeconds(0.2f);
        }

        if (GameManager.Instance.CurrentSettings == null)
        {
            FinishInitialization();
            yield break;
        }

        try
        {
            Resolution[] availableResolutions = Screen.resolutions;
            GameManager.Instance.CurrentSettings.ValidateResolution(availableResolutions);

            Resolution savedRes = GameManager.Instance.CurrentSettings.GetResolutionFromInts();

            if (FindResolutionIndex(savedRes) != -1)
            {
                currentRes = savedRes;
                currentResolutionIndex = FindResolutionIndex(currentRes);
                selectedRes = currentRes;

            }
            else
            {
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading saved resolution: {e.Message}");
        }

        FinishInitialization();
    }

    private void FinishInitialization()
    {
        UpdateResolutionText();
        UpdateApplyButtonVisibility();
        isInitialized = true;

    }

    public void RightArrowClicked()
    {

        SoundManager.Instance.PlaySFX("Click");
        if (!isInitialized) return;

        currentResolutionIndex++;
        if (currentResolutionIndex >= SystemRes.Count)
        {
            currentResolutionIndex = 0;
        }

        selectedRes = SystemRes[currentResolutionIndex];
        UpdateResolutionText();
        UpdateApplyButtonVisibility();
    }

    public void LeftArrowClicked()
    {

        SoundManager.Instance.PlaySFX("Click");
        if (!isInitialized) return;

        currentResolutionIndex--;
        if (currentResolutionIndex < 0)
        {
            currentResolutionIndex = SystemRes.Count - 1;
        }

        selectedRes = SystemRes[currentResolutionIndex];
        UpdateResolutionText();
        UpdateApplyButtonVisibility();
    }

    public void ApplyResolution()
    {
        if (!isInitialized) return;

        Screen.SetResolution(selectedRes.width, selectedRes.height, Screen.fullScreen);
        currentRes = selectedRes;
        currentResolutionIndex = FindResolutionIndex(selectedRes);

        if (GameManager.Instance != null && GameManager.Instance.CurrentSettings != null)
        {
            GameManager.Instance.CurrentSettings.ApplyResolutionWithRefreshRate(selectedRes);
        }

        UpdateResolutionText();
        UpdateApplyButtonVisibility();
    }

    public void UpdateResolutionText()
    {
        if (resolutionText != null)
        {
            int refreshRate = Mathf.RoundToInt((float)selectedRes.refreshRateRatio.value);
            resolutionText.text = $"{selectedRes.width} X {selectedRes.height} @ {refreshRate}Hz";

            if (AreResolutionsEqual(selectedRes, currentRes))
            {
                resolutionText.color = Color.white; 
            }
            else
            {
                resolutionText.color = Color.yellow; 
            }
        }
    }

    public void UpdateApplyButtonVisibility()
    {
        if (applyButton != null)
        {
            bool hasResolutionChanged = !AreResolutionsEqual(selectedRes, currentRes);
            applyButton.SetActive(hasResolutionChanged);
        }
    }

    private bool AreResolutionsEqual(Resolution res1, Resolution res2)
    {
        return res1.width == res2.width &&
               res1.height == res2.height &&
               RefreshRatesEqual(res1.refreshRateRatio, res2.refreshRateRatio);
    }

    private bool RefreshRatesEqual(RefreshRate a, RefreshRate b)
    {
        return Mathf.Abs((float)a.value - (float)b.value) < 0.01f;
    }

    public int FindResolutionIndex(Resolution resolution)
    {
        for (int i = 0; i < SystemRes.Count; i++)
        {
            if (SystemRes[i].width == resolution.width &&
                SystemRes[i].height == resolution.height &&
                RefreshRatesEqual(SystemRes[i].refreshRateRatio, resolution.refreshRateRatio))
            {
                return i;
            }
        }
        return 0; 
    }

    private List<Resolution> FilterResolutions(Resolution[] resolutions)
    {
        List<Resolution> filtered = new List<Resolution>();
        HashSet<string> seen = new HashSet<string>();

        foreach (var res in resolutions)
        {
            int refreshRate = Mathf.RoundToInt((float)res.refreshRateRatio.value);
            string key = $"{res.width}x{res.height}@{refreshRate}";

            if (!seen.Contains(key))
            {
                seen.Add(key);
                filtered.Add(res);
            }
        }

        return filtered;
    }
    public bool IsReady()
    {
        return isInitialized;
    }

    public void RefreshSettings()
    {
        if (!isInitialized)
        {
            StartCoroutine(InitializeWithSavedSettings());
        }
    }
}