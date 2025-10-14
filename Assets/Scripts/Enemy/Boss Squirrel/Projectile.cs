using UnityEngine;

public class AcornProjectile : MonoBehaviour
{
    public int damage = 1;
    public float lifetime = 3f;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            HealthModule health = other.GetComponent<HealthModule>();
            if (health != null)
            {
                health.TakeDamage(damage);
            }
            Destroy(gameObject);
        }
        else if (!other.isTrigger) // Destroy on collision with walls/ground
        {
            Destroy(gameObject);
        }
    }
}