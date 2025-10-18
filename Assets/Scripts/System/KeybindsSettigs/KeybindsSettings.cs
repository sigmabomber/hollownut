using System.Collections;
using TMPro;
using UnityEngine;

public class KeybindSettings : MonoBehaviour
{
    [Header("Conflict Warning")]
    public TMP_Text globalConflictText;

    private KeybindButton currentListeningButton;
    private bool isListeningForInput = false;

    void OnEnable()
    {
        Debug.Log("KeybindSettings enabled - setting up event listeners");

        // Subscribe to settings updates
        if (GameManager.Instance?.CurrentSettings != null)
        {
            GameManager.Instance.CurrentSettings.SettingsUpdated += OnSettingsUpdated;
            Debug.Log("Subscribed to SettingsUpdated event");
        }
        else
        {
            Debug.LogWarning("GameManager or CurrentSettings not available during OnEnable");
        }

        // Wait a frame then refresh to ensure GameManager is ready
        StartCoroutine(DelayedRefresh());
    }

    void OnDisable()
    {
        Debug.Log("KeybindSettings disabled - cleaning up event listeners");

        // Unsubscribe from settings updates
        if (GameManager.Instance?.CurrentSettings != null)
        {
            GameManager.Instance.CurrentSettings.SettingsUpdated -= OnSettingsUpdated;
            Debug.Log("Unsubscribed from SettingsUpdated event");
        }
    }

    private IEnumerator DelayedRefresh()
    {
        yield return null; // Wait one frame
        RefreshAllKeybinds();
    }

    private void OnSettingsUpdated()
    {
        Debug.Log("SettingsUpdated event received - refreshing keybind UI");
        // Refresh immediately when settings change
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
                // Check for mouse buttons
                for (int i = 0; i < 3; i++)
                {
                    if (Input.GetMouseButtonDown(i))
                    {
                        ApplyNewKeybind(KeyCode.Mouse0 + i);
                        yield break;
                    }
                }

                // Check for keyboard keys
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
            Debug.Log($"Keybind updated: {currentListeningButton.actionKey} -> {newKey}");
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

            // Auto-hide after 3 seconds
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
        Debug.Log("Keybind change cancelled");
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
        // Safe check
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

        KeybindButton[] keybindButtons = FindObjectsOfType<KeybindButton>();
        Debug.Log($"Refreshing {keybindButtons.Length} keybind buttons");

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
        Debug.Log("Reset button clicked");

        if (GameManager.Instance != null && GameManager.Instance.CurrentSettings != null)
        {
            // Get reference to KeybindSettings and force refresh
            KeybindSettings keybindSettings = this.GetComponent<KeybindSettings>();
            if (keybindSettings != null)
            {
                Debug.Log("Calling ResetAllKeybindsToDefault");
                keybindSettings.ResetAllKeybindsToDefault();
            }
            else
            {
                Debug.LogWarning("KeybindSettings not found in scene");
                // Fallback: reset directly
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
            Debug.Log("Resetting all keybinds to default");
            GameManager.Instance.CurrentSettings.ResetKeybindsToDefault();

            // MANUAL REFRESH - Force UI update immediately
            StartCoroutine(ForceRefreshAfterReset());
        }
        else
        {
            Debug.LogWarning("Cannot reset keybinds - GameManager not available");
        }
    }

    private IEnumerator ForceRefreshAfterReset()
    {
        // Wait a moment for the reset to complete
        yield return new WaitForSeconds(0.1f);
        Debug.Log("Forcing manual refresh after reset");
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