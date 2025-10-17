using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public Transform currentUISelected = null;
    public Transform PlayerHUD;

    public void requestUI(Transform ui)
    {
        // close the current ui and open the requested ui
    }

    public void releaseUI(Transform ui)
    {
        // close ui and make hud visisble
    }

    public void ToggleDefaultUI()
    {
        if (currentUISelected != null)
            releaseUI(currentUISelected);


        requestUI(PlayerHUD);
    }

}
