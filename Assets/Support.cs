// CrackedNut.cs
using UnityEngine;

public class CrackedNut : MonoBehaviour
{
    private GameObject healingSeedPrefab;
    private GameObject energyOrbPrefab;

    public void Initialize(GameObject healPrefab, GameObject energyPrefab)
    {
        healingSeedPrefab = healPrefab;
        energyOrbPrefab = energyPrefab;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            BreakNut();
        }
    }

    void BreakNut()
    {
        // 70% chance for healing, 30% for energy
        if (Random.Range(0, 100) < 70)
        {
            Instantiate(healingSeedPrefab, transform.position, Quaternion.identity);
        }
        else
        {
            Instantiate(energyOrbPrefab, transform.position, Quaternion.identity);
        }
        Destroy(gameObject);
    }
}

// NovaEffect.cs
public class NovaEffect : MonoBehaviour
{
    private float maxRadius;
    private float expandTime;
    private float currentRadius = 0f;
    private float timer = 0f;

    public void Initialize(Vector2 position, float radius, float time)
    {
        transform.position = position;
        maxRadius = radius;
        expandTime = time;
    }

    void Update()
    {
        timer += Time.deltaTime;
        currentRadius = Mathf.Lerp(0f, maxRadius, timer / expandTime);

        // Check for player collision
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, currentRadius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                hit.GetComponent<HealthModule>().TakeDamage(20, transform.position);
            }
        }

        if (timer >= expandTime)
        {
            Destroy(gameObject);
        }
    }
}