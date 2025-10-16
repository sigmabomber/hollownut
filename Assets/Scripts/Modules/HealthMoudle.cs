using System;
using System.Collections;
using UnityEngine;

public class HealthModule : MonoBehaviour
{
    public float currentHealth;
    protected float maxHealth;

    public Action<float, float> onHealthChanged;
    public Action onInvincDamage;
    public Action onDeath;

    public Material whiteMaterial;
    public Material defaultMaterial;
    public bool invincible = false;
    private Renderer rendererrr;

    private void Awake()
    {
        rendererrr = GetComponent<Renderer>();

        if (whiteMaterial == null)
            whiteMaterial = Resources.Load<Material>("Materials/Additiv");

        if (defaultMaterial == null)
            defaultMaterial = Resources.Load<Material>("Materials/New Material");

        if (whiteMaterial == null)
            Debug.LogWarning("White material 'Additiv' not found in Resources/Materials!");
        if (defaultMaterial == null)
            Debug.LogWarning("Default material 'New Material' not found in Resources/Materials!");
    }

    public virtual void Initialize(float health = 67)
    {
        maxHealth = health;
        currentHealth = maxHealth;

        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    // Takes damage
    public virtual void TakeDamage(float amount, Vector2? point = null)
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

        StartCoroutine(FlashMaterial());

        if (currentHealth <= 0)
            Die();
    }

    private IEnumerator FlashMaterial()
    {
        if (rendererrr == null) yield break;

        rendererrr.material = whiteMaterial;
        yield return new WaitForSeconds(0.15f);
        rendererrr.material = defaultMaterial;
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

    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public bool CanBeHit() => !invincible;
}
