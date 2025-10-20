using System.Collections;
using UnityEditor.SearchService;
using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    public BaseUI optionsUI;
    public BaseUI menuUI;
    public BaseUI AudioUI;
    public BaseUI KeybindsUI;
    public BaseUI VideoUI;
    bool open = false;

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            open = !open;
           TogglePauseMenu(open);
        }
    }

    void TogglePauseMenu(bool toggle)
    {
        if (toggle)
        {
            UIManager.Instance.OpenUI(optionsUI);
            UIManager.Instance.OpenUI(menuUI);

            Time.timeScale = 0f;
        }
        else
        {
            UIManager.Instance.CloseUI(menuUI);
            UIManager.Instance.CloseUI(optionsUI);
            Time.timeScale = 1f;
        }

    }

    public void VideoButtonClicked()
    {
        if (!open)
        {
            print(":(");
            return;
        }

        SoundManager.Instance.PlaySFX("Click");
        UIManager.Instance.OpenUI(VideoUI);

    }
    public void KeybindsButtonClicked()
    {
        if (!open)
        {
            print(":(");
            return;
        }

        SoundManager.Instance.PlaySFX("Click");
        UIManager.Instance.OpenUI(KeybindsUI);

    }
    public void BackButtonClicked()
    {
        if (!open) return;

        SoundManager.Instance.PlaySFX("Click");
        open = false;
        TogglePauseMenu(false);
    }
}