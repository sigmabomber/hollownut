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


    // Hit freeze settings
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

    // New method for hit freeze effect
    IEnumerator HitFreeze()
    {
        // Freeze time
        Time.timeScale = timeScaleDuringFreeze;

        // Wait for real seconds (not affected by time scale)
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

            // Start both effects
            StartCoroutine(FlashColor(Color.red));
            StartCoroutine(HitFreeze()); // Add the hit freeze

            if (gameObject.TryGetComponent(out IKnockback knockbackable))
            {
                Vector2 knockbackDir = new Vector2(-transform.localScale.x, 0f);
                // knockbackable.ApplyKnockback(new KnockbackData(knockbackDir, 100f, 0.1f));
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