using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KeybindButton : MonoBehaviour
{
    [Header("Keybind Settings")]
    public string actionKey;

    [Header("UI References")]
    public TMP_Text actionText;
    public TMP_Text keyText;
    private Transform arrow;
    public TMP_Text conflictWarningText;
    public Button changeKeyButton;

    private KeybindSettings keybindSettings;
    private string originalKeyText;

    void OnEnable()
    {
        keybindSettings = FindAnyObjectByType<KeybindSettings>();

        if (changeKeyButton != null)
        {
            changeKeyButton.onClick.RemoveListener(OnChangeKeyClicked);
            changeKeyButton.onClick.AddListener(OnChangeKeyClicked);
        }

        // Find the arrow transform
        arrow = transform.Find("Image");
        StartCoroutine(DelayedUpdateUI());
    }

    private System.Collections.IEnumerator DelayedUpdateUI()
    {
        yield return null;
        UpdateUI();
    }

    void OnDisable()
    {
        if (changeKeyButton != null)
        {
            changeKeyButton.onClick.RemoveListener(OnChangeKeyClicked);
        }
    }

    public void OnChangeKeyClicked()
    {
        if (keybindSettings != null)
        {
            keybindSettings.StartListeningForInput(actionKey, this);
        }
    }

    public void UpdateUI()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentSettings == null)
        {
            return;
        }

        // Update action text
        if (actionText != null)
        {
            actionText.text = FormatActionName(actionKey);
        }

        // Update key text and arrow
        if (keyText != null)
        {
            KeyCode currentKey = GetCurrentKey();
            string formattedKey = FormatKeyCode(currentKey);

            // Handle arrow keys - show arrow image instead of text
            if (formattedKey == "↑")
            {
                keyText.text = "";
                if (arrow != null)
                {
                    arrow.gameObject.SetActive(true);
                    arrow.rotation = Quaternion.Euler(0, 0, 0); // Up arrow
                }
            }
            else if (formattedKey == "→")
            {
                keyText.text = "";
                if (arrow != null)
                {
                    arrow.gameObject.SetActive(true);
                    arrow.rotation = Quaternion.Euler(0, 0, -90); // Right arrow
                }
            }
            else if (formattedKey == "↓")
            {
                keyText.text = "";
                if (arrow != null)
                {
                    arrow.gameObject.SetActive(true);
                    arrow.rotation = Quaternion.Euler(0, 0, 180); // Down arrow
                }
            }
            else if (formattedKey == "←")
            {
                keyText.text = "";
                if (arrow != null)
                {
                    arrow.gameObject.SetActive(true);
                    arrow.rotation = Quaternion.Euler(0, 0, 90); // Left arrow
                }
            }
            else
            {
                // Regular key - show text and hide arrow
                keyText.text = formattedKey;
                originalKeyText = formattedKey;
                if (arrow != null)
                {
                    arrow.gameObject.SetActive(false);
                }
            }
        }

        // Update conflict warning
        UpdateConflictWarning();
    }

    private void UpdateConflictWarning()
    {
        if (conflictWarningText == null) return;
        if (GameManager.Instance?.CurrentSettings == null) return;

        KeyCode currentKey = GetCurrentKey();
        var conflictingActions = GameManager.Instance.CurrentSettings.GetActionsForKey(currentKey);

        // Show warning if this key is used for multiple actions
        if (conflictingActions.Count > 1)
        {
            List<string> otherActions = new List<string>();
            foreach (string action in conflictingActions)
            {
                if (action != actionKey)
                {
                    otherActions.Add(action);
                }
            }

            if (otherActions.Count > 0)
            {
                conflictWarningText.text = $"Also: {string.Join(", ", otherActions)}";
                conflictWarningText.gameObject.SetActive(true);
                return;
            }
        }

        conflictWarningText.gameObject.SetActive(false);
    }

    public void StartListening()
    {
        if (keyText != null)
        {
            keyText.text = "Press any key...";
        }

        // Hide arrow while listening
        if (arrow != null)
        {
            arrow.gameObject.SetActive(false);
        }

        if (changeKeyButton != null)
        {
            changeKeyButton.interactable = false;
            var colors = changeKeyButton.colors;
            colors.normalColor = Color.yellow;
            changeKeyButton.colors = colors;
        }

        // Hide conflict warning while listening
        if (conflictWarningText != null)
        {
            conflictWarningText.gameObject.SetActive(false);
        }
    }

    public void StopListening()
    {
        UpdateUI(); // Refresh with the new key

        if (changeKeyButton != null)
        {
            changeKeyButton.interactable = true;
            var colors = changeKeyButton.colors;
            colors.normalColor = Color.white;
            changeKeyButton.colors = colors;
        }
    }

    private KeyCode GetCurrentKey()
    {
        try
        {
            var keybinds = GameManager.Instance.CurrentSettings.GetKeybindsDictionary();
            return keybinds.ContainsKey(actionKey) ? keybinds[actionKey] : KeyCode.None;
        }
        catch (System.Exception)
        {
            return KeyCode.None;
        }
    }

    private string FormatActionName(string actionName)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        bool firstChar = true;

        foreach (char c in actionName)
        {
            if (char.IsUpper(c) && !firstChar)
            {
                sb.Append(' ');
            }
            sb.Append(firstChar ? char.ToUpper(c) : c);
            firstChar = false;
        }

        return sb.ToString();
    }

    private string FormatKeyCode(KeyCode keyCode)
    {
        switch (keyCode)
        {
            case KeyCode.Mouse0: return "L Click";
            case KeyCode.Mouse1: return "R Click";
            case KeyCode.Mouse2: return "Middle Click";
            case KeyCode.LeftShift: return "L Shift";
            case KeyCode.RightShift: return "R Shift";
            case KeyCode.LeftControl: return "L Ctrl";
            case KeyCode.RightControl: return "R Ctrl";
            case KeyCode.LeftAlt: return "L Alt";
            case KeyCode.RightAlt: return "R Alt";
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
}