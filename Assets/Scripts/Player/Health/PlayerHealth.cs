using System;
using System.Collections;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    private SpriteRenderer playerSprite;
    private HealthModule healthModule;
    private float currentHealth;
    private Rigidbody2D rb;


    [SerializeField] private float hitFreezeDuration = 0.1f;
    [SerializeField] private float timeScaleDuringFreeze = 0.01f;

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
        rb = GetComponent<Rigidbody2D>();
    }

    IEnumerator FlashColor(Color color)
    {
        playerSprite.color = color;
        yield return new WaitForSeconds(0.1f);
        playerSprite.color = Color.white;
    }

    IEnumerator HitFreeze()
    {
        Time.timeScale = timeScaleDuringFreeze;

        yield return new WaitForSecondsRealtime(hitFreezeDuration);

        // Restore normal time
        Time.timeScale = 1f;
    }

    void OnHealthChanged(float newCurrent, float max)
    {
        Debug.Log($"Current Health: {newCurrent}hp, Max Health: {max}hp");

        if (newCurrent < currentHealth)
        {
            Debug.Log("Player has taken damage!");

            StartCoroutine(FlashColor(Color.red));
            StartCoroutine(HitFreeze()); 

            if (gameObject.TryGetComponent(out IKnockback knockbackable))
            {
                Vector2 knockbackDir = new Vector2(-transform.localScale.x, 0f);
            }
        }
        else
        {
            Debug.Log("Player has healed");
            StartCoroutine(FlashColor(Color.green));
        }

        currentHealth = newCurrent;
    }
}