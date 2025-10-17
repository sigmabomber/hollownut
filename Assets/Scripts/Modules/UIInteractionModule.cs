using System;
using UnityEngine;

public class UIInteractionModule : MonoBehaviour
{


    public static UIInteractionModule Instance;

    public KeyCode interactionKey = Constants.PlayerData.PlayerControls.interact;

    public Action beginInteraction;

    private bool isInteracting = false;



    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
