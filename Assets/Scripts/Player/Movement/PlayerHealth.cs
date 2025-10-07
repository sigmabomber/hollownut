using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{


    private HealthModule healthModule;

    private float currentHealth;
    void Start()
    {
        healthModule = this.GetComponent<HealthModule>();

        healthModule.Initialize(100f);

        currentHealth = healthModule.currentHealth;

        healthModule.onHealthChanged += OnHealthChanged;
    }


    void OnHealthChanged(float newCurrent, float max)
    {
        Debug.Log($"Current Health: {newCurrent}hp, Max Health: {max}hp");


        if (newCurrent < currentHealth)
        {
            Debug.Log("Player has taken damage!");
            
        }
        else
        {
            Debug.Log("Player has healed");
        }
    }
}
