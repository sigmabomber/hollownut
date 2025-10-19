using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AudioSettings : MonoBehaviour
{
    [Header("Music Settings")]
    public Slider musicSlider;
    public TMP_InputField musicInputField;
    public TMP_Text musicLabel;

    [Header("SFX Settings")]
    public Slider sfxSlider;
    public TMP_InputField sfxInputField;
    public TMP_Text sfxLabel;



    public BaseUI menuUI;
    private bool isUpdatingUI = false;

    void OnEnable()
    {
        StartCoroutine(DelayedInitialize());
    }

    private System.Collections.IEnumerator DelayedInitialize()
    {
        yield return null;
        InitializeUI();
    }

    private void InitializeUI()
    {
        if (GameManager.Instance?.CurrentSettings == null)
        {
            return;
        }

        if (musicSlider != null)
        {
            musicSlider.minValue = 0f;
            musicSlider.maxValue = 1f;
            musicSlider.value = GameManager.Instance.CurrentSettings.MusicVolume;
            musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
        }

        if (musicInputField != null)
        {
            musicInputField.text = Mathf.RoundToInt(GameManager.Instance.CurrentSettings.MusicVolume * 100).ToString();
            musicInputField.onValueChanged.AddListener(OnMusicInputFieldChanged);
            musicInputField.onEndEdit.AddListener(OnMusicInputFieldEndEdit);
        }

        if (musicLabel != null)
        {
            musicLabel.text = "Music Volume";
        }

        if (sfxSlider != null)
        {
            sfxSlider.minValue = 0f;
            sfxSlider.maxValue = 1f;
            sfxSlider.value = GameManager.Instance.CurrentSettings.SFXVolume;
            sfxSlider.onValueChanged.AddListener(OnSFXSliderChanged);
        }

        if (sfxInputField != null)
        {
            sfxInputField.text = Mathf.RoundToInt(GameManager.Instance.CurrentSettings.SFXVolume * 100).ToString();
            sfxInputField.onValueChanged.AddListener(OnSFXInputFieldChanged);
            sfxInputField.onEndEdit.AddListener(OnSFXInputFieldEndEdit);
        }

        if (sfxLabel != null)
        {
            sfxLabel.text = "SFX Volume";
        }

      
        GameManager.Instance.CurrentSettings.SettingsUpdated += OnSettingsUpdated;
    }

    void OnDisable()
    {
        if (GameManager.Instance?.CurrentSettings != null)
        {
            GameManager.Instance.CurrentSettings.SettingsUpdated -= OnSettingsUpdated;
        }

        if (musicSlider != null) musicSlider.onValueChanged.RemoveListener(OnMusicSliderChanged);
        if (musicInputField != null)
        {
            musicInputField.onValueChanged.RemoveListener(OnMusicInputFieldChanged);
            musicInputField.onEndEdit.RemoveListener(OnMusicInputFieldEndEdit);
        }

        if (sfxSlider != null) sfxSlider.onValueChanged.RemoveListener(OnSFXSliderChanged);
        if (sfxInputField != null)
        {
            sfxInputField.onValueChanged.RemoveListener(OnSFXInputFieldChanged);
            sfxInputField.onEndEdit.RemoveListener(OnSFXInputFieldEndEdit);
        }

     
    }

    private void OnMusicSliderChanged(float value)
    {
        if (isUpdatingUI) return;

        isUpdatingUI = true;
        GameManager.Instance.CurrentSettings.SetMusicVolume(value);
        if (musicInputField != null)
        {
            musicInputField.text = Mathf.RoundToInt(value * 100).ToString();
        }
        isUpdatingUI = false;
    }

    private void OnMusicInputFieldChanged(string value)
    {
        if (isUpdatingUI) return;

        if (int.TryParse(value, out int percent))
        {
            isUpdatingUI = true;
            float volume = Mathf.Clamp01(percent / 100f);
            if (musicSlider != null)
            {
                musicSlider.value = volume;
            }
            isUpdatingUI = false;
        }
    }

    private void OnMusicInputFieldEndEdit(string value)
    {
        if (isUpdatingUI) return;

        if (int.TryParse(value, out int percent))
        {
            float volume = Mathf.Clamp01(percent / 100f);
            GameManager.Instance.CurrentSettings.SetMusicVolume(volume);

            if (musicInputField != null)
            {
                musicInputField.text = Mathf.RoundToInt(volume * 100).ToString();
            }
        }
        else
        {
            if (musicInputField != null)
            {
                musicInputField.text = Mathf.RoundToInt(GameManager.Instance.CurrentSettings.MusicVolume * 100).ToString();
            }
        }
    }

    private void OnSFXSliderChanged(float value)
    {
        if (isUpdatingUI) return;

        isUpdatingUI = true;
        GameManager.Instance.CurrentSettings.SetSFXVolume(value);
        if (sfxInputField != null)
        {
            sfxInputField.text = Mathf.RoundToInt(value * 100).ToString();
        }
        isUpdatingUI = false;
    }

    private void OnSFXInputFieldChanged(string value)
    {
        if (isUpdatingUI) return;

        if (int.TryParse(value, out int percent))
        {
            isUpdatingUI = true;
            float volume = Mathf.Clamp01(percent / 100f);
            if (sfxSlider != null)
            {
                sfxSlider.value = volume;
            }
            isUpdatingUI = false;
        }
    }

    private void OnSFXInputFieldEndEdit(string value)
    {
        if (isUpdatingUI) return;

        if (int.TryParse(value, out int percent))
        {
            float volume = Mathf.Clamp01(percent / 100f);
            GameManager.Instance.CurrentSettings.SetSFXVolume(volume);

            if (sfxInputField != null)
            {
                sfxInputField.text = Mathf.RoundToInt(volume * 100).ToString();
            }
        }
        else
        {
            if (sfxInputField != null)
            {
                sfxInputField.text = Mathf.RoundToInt(GameManager.Instance.CurrentSettings.SFXVolume * 100).ToString();
            }
        }
    }

  

    

  
    private void OnSettingsUpdated()
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (GameManager.Instance?.CurrentSettings == null) return;

        isUpdatingUI = true;

        if (musicSlider != null)
        {
            musicSlider.value = GameManager.Instance.CurrentSettings.MusicVolume;
        }
        if (musicInputField != null)
        {
            musicInputField.text = Mathf.RoundToInt(GameManager.Instance.CurrentSettings.MusicVolume * 100).ToString();
        }

        if (sfxSlider != null)
        {
            sfxSlider.value = GameManager.Instance.CurrentSettings.SFXVolume;
        }
        if (sfxInputField != null)
        {
            sfxInputField.text = Mathf.RoundToInt(GameManager.Instance.CurrentSettings.SFXVolume * 100).ToString();
        }

     

        isUpdatingUI = false;
    }

    // Public methods for external control
    public void SetMusicVolume(float volume)
    {
        if (GameManager.Instance?.CurrentSettings != null)
        {
            GameManager.Instance.CurrentSettings.SetMusicVolume(volume);
        }
    }

    public void SetSFXVolume(float volume)
    {
        if (GameManager.Instance?.CurrentSettings != null)
        {
            GameManager.Instance.CurrentSettings.SetSFXVolume(volume);
        }
    }

    public void SetMasterVolume(float volume)
    {
        if (GameManager.Instance?.CurrentSettings != null)
        {
            GameManager.Instance.CurrentSettings.SetMasterVolume(volume);
        }
    }

    public void ResetClicked()
    {
        if (GameManager.Instance?.CurrentSettings != null)
        {
            GameManager.Instance.CurrentSettings.ResetAudioSettings();
        }
    }
    public void BackClicked()
    {
        UIManager.Instance.OpenUI(menuUI);
    }
}