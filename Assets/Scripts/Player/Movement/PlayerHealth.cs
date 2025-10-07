using System;
using System.Collections;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    private SpriteRenderer playerSprite;

    private HealthModule healthModule;

    private float currentHealth;


    
    void Start()
    {
        InitializeComponents();
        

        healthModule.Initialize(100f);

        currentHealth = healthModule.currentHealth;

        healthModule.onHealthChanged += OnHealthChanged;
    }

    void InitializeComponents()
    {
        healthModule = GetComponent<HealthModule>();
        playerSprite = GetComponent<SpriteRenderer>();

    }


    IEnumerator FlashColor(Color color)
    {
        playerSprite.color = color;
        
        yield return new WaitForSeconds(0.1f);

        playerSprite.color = Color.white;
    }

    void OnHealthChanged(float newCurrent, float max)
    {
        Debug.Log($"Current Health: {newCurrent}hp, Max Health: {max}hp");


        if (newCurrent < currentHealth)
        {
            Debug.Log("Player has taken damage!");

            StartCoroutine(FlashColor(Color.red));
            
        }
        else
        {
            Debug.Log("Player has healed");
            StartCoroutine(FlashColor(Color.green));


        }
    }
}
