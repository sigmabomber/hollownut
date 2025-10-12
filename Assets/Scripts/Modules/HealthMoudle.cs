using System;
using UnityEngine;

public class HealthModule : MonoBehaviour
{
    
    public float currentHealth;
    protected float maxHealth;


    public Action<float, float> onHealthChanged;
    public Action onInvincDamage;
    public Action onDeath;

    public bool invincible = false;

    // Setup the system
    public virtual void Initialize(float health = 67)
    {
        maxHealth = health;

        currentHealth = maxHealth;

        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    // Takes damage
    public virtual void TakeDamage(float amount)
    {
        if (invincible) 
        {
            onInvincDamage?.Invoke();
            return;
        }
        if (amount <= 0 || currentHealth <= 0) return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        onHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
            Die();
    }
    // Heals
    public virtual void Heal(float amount)
    {
        if (amount <= 0 || currentHealth <= 0) return;
        currentHealth += amount;

        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }


    // For dying
    protected virtual void Die()
    {
        onDeath?.Invoke();
    }


    public bool CanBeHit() => !invincible;

}
