using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class KeybindPair
{
    public string action;
    public KeyCode key;
}

[Serializable]
public class SettingsData
{
    public List<KeybindPair> KeybindsList = new List<KeybindPair>();
    public float MusicVolume = 1f;
    public float SFXVolume = 1f;
    public float MasterVolume = 1f;

    public int ScreenWidth = 1920;
    public int ScreenHeight = 1080;
    public int TargetFrameRate = 60;
    public FullScreenMode FullscreenMode = FullScreenMode.FullScreenWindow;
    public bool VSync = true;

    public Action SettingsUpdated;

    public SettingsData()
    {
        KeybindsList.Add(new KeybindPair { action = "up", key = KeyCode.UpArrow });
        KeybindsList.Add(new KeybindPair { action = "down", key = KeyCode.DownArrow });
        KeybindsList.Add(new KeybindPair { action = "left", key = KeyCode.LeftArrow });
        KeybindsList.Add(new KeybindPair { action = "right", key = KeyCode.RightArrow });
        KeybindsList.Add(new KeybindPair { action = "jump", key = KeyCode.Z });
        KeybindsList.Add(new KeybindPair { action = "dash", key = KeyCode.C });
        KeybindsList.Add(new KeybindPair { action = "interact", key = KeyCode.UpArrow });
        KeybindsList.Add(new KeybindPair { action = "attack", key = KeyCode.X });
        KeybindsList.Add(new KeybindPair { action = "map", key = KeyCode.M }) ;

    }

    public void AddKey(string action, KeyCode key)
    {
        KeybindsList.Add(new KeybindPair { action = action, key = key });
        SettingsUpdated?.Invoke();
    }

    public void UpdateKeybind(string action, KeyCode newKey)
    {
        foreach (var keybind in KeybindsList)
        {
            if (keybind.action == action)
            {
                keybind.key = newKey;
                SettingsUpdated?.Invoke();
                return;
            }
        }

        AddKey(action, newKey);
    }

    public void SetMusicVolume(float volume)
    {
        MusicVolume = Mathf.Clamp01(volume);
        SettingsUpdated?.Invoke();
    }

    public void SetSFXVolume(float volume)
    {
        SFXVolume = Mathf.Clamp01(volume);
        SettingsUpdated?.Invoke();
    }

    public void SetMasterVolume(float volume)
    {
        MasterVolume = Mathf.Clamp01(volume);
        SettingsUpdated?.Invoke();
    }

    public Dictionary<string, KeyCode> GetKeybindsDictionary()
    {
        Dictionary<string, KeyCode> dict = new Dictionary<string, KeyCode>();
        foreach (var pair in KeybindsList)
            dict[pair.action] = pair.key;
        return dict;
    }

    public List<string> GetActionsForKey(KeyCode key)
    {
        List<string> actions = new List<string>();
        foreach (var keybind in KeybindsList)
        {
            if (keybind.key == key)
            {
                actions.Add(keybind.action);
            }
        }
        return actions;
    }

    public string GetKeyUsageDescription(KeyCode key)
    {
        List<string> actions = GetActionsForKey(key);
        if (actions.Count == 0) return "Not used";
        if (actions.Count == 1) return $"Used for {actions[0]}";
        return $"Used for {string.Join(", ", actions)}";
    }

    public void SetVSync(bool enabled)
    {
        VSync = enabled;
        ApplyGraphicsSettings();
    }

    public void ToggleVSync()
    {
        VSync = !VSync;
        ApplyGraphicsSettings();
    }

    public string GetVSyncDisplayName()
    {
        return VSync ? "On" : "Off";
    }

    public void ApplyGraphicsSettings()
    {
        Screen.SetResolution(ScreenWidth, ScreenHeight, FullscreenMode);
        QualitySettings.vSyncCount = VSync ? 1 : 0;

        if (!VSync)
        {
            Application.targetFrameRate = TargetFrameRate;
        }
        else
        {
            Application.targetFrameRate = -1;
        }

        SettingsUpdated?.Invoke();
    }

    public void SetResolution(int width, int height)
    {
        ScreenWidth = width;
        ScreenHeight = height;
        ApplyGraphicsSettings();
    }

    public void SetTargetFrameRate(int frameRate)
    {
        TargetFrameRate = frameRate;
        ApplyGraphicsSettings();
    }

    public void ApplyResolutionWithRefreshRate(Resolution resolution)
    {
        ScreenWidth = resolution.width;
        ScreenHeight = resolution.height;
        TargetFrameRate = Mathf.RoundToInt((float)resolution.refreshRateRatio.value);
        ApplyGraphicsSettings();
    }

    public void SetFullscreenMode(FullScreenMode mode)
    {
        FullscreenMode = mode;
        ApplyGraphicsSettings();
    }

    public string GetFullscreenModeDisplayName()
    {
        return FullscreenMode switch
        {
            FullScreenMode.ExclusiveFullScreen => "Fullscreen",
            FullScreenMode.FullScreenWindow => "Borderless",
            FullScreenMode.MaximizedWindow => "Maximized",
            FullScreenMode.Windowed => "Windowed",
            _ => "Borderless"
        };
    }

    public FullScreenMode GetNextFullscreenMode()
    {
        return FullscreenMode switch
        {
            FullScreenMode.ExclusiveFullScreen => FullScreenMode.FullScreenWindow,
            FullScreenMode.FullScreenWindow => FullScreenMode.Windowed,
            FullScreenMode.Windowed => FullScreenMode.ExclusiveFullScreen,
            _ => FullScreenMode.FullScreenWindow
        };
    }

    public Resolution GetResolutionFromInts()
    {
        Resolution result = new Resolution();
        result.width = ScreenWidth;
        result.height = ScreenHeight;

        RefreshRate refreshRate = new RefreshRate();
        refreshRate.numerator = (uint)TargetFrameRate;
        refreshRate.denominator = 1;
        result.refreshRateRatio = refreshRate;

        return result;
    }

    public Resolution FindClosestResolution(Resolution[] availableResolutions = null)
    {
        if (availableResolutions == null)
            availableResolutions = Screen.resolutions;

        Resolution closest = availableResolutions[0];
        int smallestDifference = int.MaxValue;

        foreach (var res in availableResolutions)
        {
            int widthDiff = Mathf.Abs(res.width - ScreenWidth);
            int heightDiff = Mathf.Abs(res.height - ScreenHeight);
            int totalDiff = widthDiff + heightDiff;

            if (totalDiff == 0)
                return res;

            if (totalDiff < smallestDifference)
            {
                smallestDifference = totalDiff;
                closest = res;
            }
        }

        return closest;
    }

    public bool MatchesResolution(Resolution resolution)
    {
        return ScreenWidth == resolution.width &&
               ScreenHeight == resolution.height &&
               Mathf.Abs(TargetFrameRate - Mathf.RoundToInt((float)resolution.refreshRateRatio.value)) <= 1;
    }

    public void ValidateResolution(Resolution[] availableResolutions = null)
    {
        if (availableResolutions == null)
            availableResolutions = Screen.resolutions;

        bool isValid = false;
        foreach (var res in availableResolutions)
        {
            if (res.width == ScreenWidth && res.height == ScreenHeight)
            {
                isValid = true;
                break;
            }
        }

        if (!isValid)
        {
            Resolution closest = FindClosestResolution(availableResolutions);
            ScreenWidth = closest.width;
            ScreenHeight = closest.height;
            TargetFrameRate = Mathf.RoundToInt((float)closest.refreshRateRatio.value);
            SettingsUpdated?.Invoke();
        }
    }

    public void SetFrameRateCap(bool enabled)
    {
        if (!enabled)
        {
            TargetFrameRate = -1;
        }
        else
        {
            if (TargetFrameRate <= 0)
            {
                TargetFrameRate = 60;
            }
        }
        ApplyGraphicsSettings();
    }

    public bool IsFrameRateCapEnabled()
    {
        return TargetFrameRate > 0 && !VSync;
    }

    public void ResetKeybindsToDefault()
    {
        KeybindsList.Clear();

        KeybindsList.Add(new KeybindPair { action = "up", key = KeyCode.UpArrow });
        KeybindsList.Add(new KeybindPair { action = "down", key = KeyCode.DownArrow });
        KeybindsList.Add(new KeybindPair { action = "left", key = KeyCode.LeftArrow });
        KeybindsList.Add(new KeybindPair { action = "right", key = KeyCode.RightArrow });
        KeybindsList.Add(new KeybindPair { action = "jump", key = KeyCode.Z });
        KeybindsList.Add(new KeybindPair { action = "dash", key = KeyCode.C });
        KeybindsList.Add(new KeybindPair { action = "interact", key = KeyCode.UpArrow });
        KeybindsList.Add(new KeybindPair { action = "attack", key = KeyCode.X });
        KeybindsList.Add(new KeybindPair { action = "map", key = KeyCode.M });

        SettingsUpdated?.Invoke();
    }

    public void ResetVideoSettings()
    {
        Resolution nativeResolution = GetNativeResolution();

        ScreenWidth = nativeResolution.width;
        ScreenHeight = nativeResolution.height;

        TargetFrameRate = Mathf.RoundToInt((float)nativeResolution.refreshRateRatio.value);

        FullscreenMode = FullScreenMode.FullScreenWindow;

        VSync = true;

        ApplyGraphicsSettings();

    }


    private Resolution GetNativeResolution()
    {
        Resolution[] resolutions = Screen.resolutions;

        if (resolutions.Length == 0)
        {
            return Screen.currentResolution;
        }

        foreach (Resolution res in resolutions)
        {
            int refreshRate = Mathf.RoundToInt((float)res.refreshRateRatio.value);
            Debug.Log($"  {res.width}x{res.height} @ {refreshRate}Hz");
        }

        List<Resolution> highestResolutions = new List<Resolution>();
        int maxWidth = 0;
        int maxHeight = 0;

        foreach (Resolution res in resolutions)
        {
            if (res.width > maxWidth || (res.width == maxWidth && res.height > maxHeight))
            {
                maxWidth = res.width;
                maxHeight = res.height;
                highestResolutions.Clear();
                highestResolutions.Add(res);
            }
            else if (res.width == maxWidth && res.height == maxHeight)
            {
                highestResolutions.Add(res);
            }
        }

        Resolution bestResolution = highestResolutions[0];
        float maxRefreshRate = (float)bestResolution.refreshRateRatio.value;

        foreach (Resolution res in highestResolutions)
        {
            float refreshRate = (float)res.refreshRateRatio.value;
            if (refreshRate > maxRefreshRate)
            {
                bestResolution = res;
                maxRefreshRate = refreshRate;
            }
        }

     
        return bestResolution;
    }

    public void ResetAudioSettings()
    {
        MusicVolume = 1f;
        SFXVolume = 1f;
        MasterVolume = 1f;
        SettingsUpdated?.Invoke();
    }

    public void ResetAllToDefault()
    {
        ResetKeybindsToDefault();
        ResetVideoSettings();
        ResetAudioSettings();
    }
}