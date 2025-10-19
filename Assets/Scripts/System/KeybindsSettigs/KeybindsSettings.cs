using System.Collections;
using TMPro;
using UnityEngine;

public class KeybindSettings : MonoBehaviour
{
    [Header("Conflict Warning")]
    public TMP_Text globalConflictText;

    private KeybindButton currentListeningButton;
    private bool isListeningForInput = false;
    public BaseUI menuUI;
    void OnEnable()
    {

        // Subscribe to settings updates
        if (GameManager.Instance?.CurrentSettings != null)
        {
            GameManager.Instance.CurrentSettings.SettingsUpdated += OnSettingsUpdated;
        }
        else
        {
            Debug.LogWarning("GameManager or CurrentSettings not available during OnEnable");
        }

        StartCoroutine(DelayedRefresh());
    }

    void OnDisable()
    {

        // Unsubscribe from settings updates
        if (GameManager.Instance?.CurrentSettings != null)
        {
            GameManager.Instance.CurrentSettings.SettingsUpdated -= OnSettingsUpdated;
        }
    }

    private IEnumerator DelayedRefresh()
    {
        yield return null; 
        RefreshAllKeybinds();
    }

    private void OnSettingsUpdated()
    {
        RefreshAllKeybinds();
    }

    public void StartListeningForInput(string actionKey, KeybindButton button)
    {
        if (isListeningForInput) return;

        currentListeningButton = button;
        isListeningForInput = true;

        button.StartListening();
        StartCoroutine(ListenForInputCoroutine());
    }

    private IEnumerator ListenForInputCoroutine()
    {
        while (isListeningForInput)
        {
            if (Input.anyKeyDown)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (Input.GetMouseButtonDown(i))
                    {
                        ApplyNewKeybind(KeyCode.Mouse0 + i);
                        yield break;
                    }
                }

                foreach (KeyCode keyCode in System.Enum.GetValues(typeof(KeyCode)))
                {
                    if (Input.GetKeyDown(keyCode))
                    {
                        if (keyCode == KeyCode.Escape)
                        {
                            CancelListening();
                            yield break;
                        }

                        ApplyNewKeybind(keyCode);
                        yield break;
                    }
                }
            }
            yield return null;
        }
    }

    private void ApplyNewKeybind(KeyCode newKey)
    {
        if (GameManager.Instance?.CurrentSettings != null && currentListeningButton != null)
        {
            GameManager.Instance.CurrentSettings.UpdateKeybind(currentListeningButton.actionKey, newKey);
            ShowConflictWarning(newKey);
        }

        StopListening();
    }

    private void ShowConflictWarning(KeyCode newKey)
    {
        if (globalConflictText == null) return;

        var conflictingActions = GameManager.Instance.CurrentSettings.GetActionsForKey(newKey);

        if (conflictingActions.Count > 1)
        {
            globalConflictText.text = $"Note: {FormatKeyCode(newKey)} is used for {string.Join(", ", conflictingActions)}";
            globalConflictText.gameObject.SetActive(true);

            StartCoroutine(HideConflictWarningAfterDelay(3f));
        }
    }

    private IEnumerator HideConflictWarningAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (globalConflictText != null)
        {
            globalConflictText.gameObject.SetActive(false);
        }
    }

    private string FormatKeyCode(KeyCode keyCode)
    {
        switch (keyCode)
        {
            case KeyCode.Mouse0: return "Left Click";
            case KeyCode.Mouse1: return "Right Click";
            case KeyCode.Mouse2: return "Middle Click";
            case KeyCode.LeftShift: return "Left Shift";
            case KeyCode.RightShift: return "Right Shift";
            case KeyCode.LeftControl: return "Left Ctrl";
            case KeyCode.RightControl: return "Right Ctrl";
            case KeyCode.LeftAlt: return "Left Alt";
            case KeyCode.RightAlt: return "Right Alt";
            case KeyCode.UpArrow: return "↑";
            case KeyCode.DownArrow: return "↓";
            case KeyCode.LeftArrow: return "←";
            case KeyCode.RightArrow: return "→";
            case KeyCode.Return: return "Enter";
            case KeyCode.Escape: return "Esc";
            case KeyCode.Space: return "Space";
            case KeyCode.Tab: return "Tab";
            case KeyCode.CapsLock: return "Caps Lock";
            case KeyCode.Backspace: return "Backspace";
            default:
                string keyString = keyCode.ToString();
                if (keyString.StartsWith("Alpha"))
                    return keyString.Substring(5);
                if (keyString.StartsWith("Keypad"))
                    return "Num " + keyString.Substring(6);
                return keyString;
        }
    }

    private void CancelListening()
    {
        StopListening();
    }

    private void StopListening()
    {
        if (currentListeningButton != null)
        {
            currentListeningButton.StopListening();
            currentListeningButton = null;
        }

        isListeningForInput = false;
    }

    public void RefreshAllKeybinds()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("Cannot refresh keybinds - GameManager is null");
            return;
        }

        if (GameManager.Instance.CurrentSettings == null)
        {
            Debug.LogWarning("Cannot refresh keybinds - CurrentSettings is null");
            return;
        }

        KeybindButton[] keybindButtons = FindObjectsByType<KeybindButton>(FindObjectsSortMode.None);

        foreach (KeybindButton button in keybindButtons)
        {
            if (button != null)
            {
                button.UpdateUI();
            }
        }
    }
    public void ResetClicked()
    {

        if (GameManager.Instance != null && GameManager.Instance.CurrentSettings != null)
        {
            KeybindSettings keybindSettings = this.GetComponent<KeybindSettings>();
            if (keybindSettings != null)
            {
                keybindSettings.ResetAllKeybindsToDefault();
            }
            else
            {
                Debug.LogWarning("KeybindSettings not found in scene");
                GameManager.Instance.CurrentSettings.ResetKeybindsToDefault();
            }
        }
        else
        {
            Debug.LogWarning("GameManager or CurrentSettings not available");
        }
    }
    public void ResetAllKeybindsToDefault()
    {
        if (GameManager.Instance?.CurrentSettings != null)
        {
            GameManager.Instance.CurrentSettings.ResetKeybindsToDefault();

            StartCoroutine(ForceRefreshAfterReset());
        }
        else
        {
            Debug.LogWarning("Cannot reset keybinds - GameManager not available");
        }
    }

    public void BackClicked()
    {
        UIManager.Instance.OpenUI(menuUI);
    }
    private IEnumerator ForceRefreshAfterReset()
    {
        yield return new WaitForSeconds(0.1f);
        RefreshAllKeybinds();
    }

    void Update()
    {
        if (isListeningForInput && Input.GetKeyDown(KeyCode.Escape))
        {
            CancelListening();
        }
    }
}