using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class QuckMap : MonoBehaviour
{

    KeyCode mapKey;
    bool hasMapUnlocked = true;
    bool toggled = false;
    public BaseUI map;
    private void Start()
    {
        StartCoroutine(GetKeybind());
    }
    IEnumerator GetKeybind()
    {
        yield return new WaitForSeconds(1f);


        GameManager.Instance.CurrentSettings.SettingsUpdated += UpdateKeyBinds;


        Dictionary<string, KeyCode> keybinds = GameManager.Instance.CurrentSettings.GetKeybindsDictionary();


        if(keybinds != null)
        {
            mapKey = keybinds["map"];
        }
    }

    void UpdateKeyBinds()
    {


    }

    private void Update()
    {
     if (Input.GetKeyUp(mapKey) && hasMapUnlocked)
        {
            toggled = !toggled;
            ToggleMap();
        }   
    }
    void ToggleMap()
    {

        if (toggled)
        {
            UIManager.Instance.OpenUI(map);
        }
        else
        {
            UIManager.Instance.CloseUI(map);
        }

    }

}
