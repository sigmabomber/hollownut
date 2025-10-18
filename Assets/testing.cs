using UnityEngine;

public class testing : MonoBehaviour
{
    public BaseUI optionsUI;
    public BaseUI menuUI;
    bool open = false;
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            open = !open;
            if (open)
            {
                UIManager.Instance.OpenUI(optionsUI);
                UIManager.Instance.OpenUI(menuUI);
            }
            else
            {

                UIManager.Instance.CloseUI(menuUI);
                UIManager.Instance.CloseUI(optionsUI);
            }
            
        }
    }
}
