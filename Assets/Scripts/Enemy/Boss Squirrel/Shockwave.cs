using UnityEngine;

public class Shockwave : MonoBehaviour
{
    [Header("Shockwave Settings")]
    public float speed = 5f;
    public float damage = 1f;
    public float maxDistance = 8f;
    public float lifetime = 2f;
    public LayerMask collisionLayers;
    public LayerMask groundLayer;

    [Header("Visual Settings")]
    public AnimationCurve scaleCurve;
    public AnimationCurve alphaCurve;

    private Vector2 direction;
    private Vector3 startPosition;
    private float startTime;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        startPosition = transform.position;
        startTime = Time.time;
        spriteRenderer = GetComponent<SpriteRenderer>();

        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        // Move shockwave
        transform.position += (Vector3)direction * speed * Time.deltaTime;

        // Calculate distance traveled
        float distanceTraveled = Vector2.Distance(startPosition, transform.position);
        float progress = distanceTraveled / maxDistance;

        // Apply visual effects
        if (spriteRenderer != null)
        {
            // Scale
            float scale = scaleCurve.Evaluate(progress);
            transform.localScale = new Vector3(scale, scale, 1f);

            // Fade out
            Color color = spriteRenderer.color;
            color.a = alphaCurve.Evaluate(progress);
            spriteRenderer.color = color;
        }

        // Destroy if max distance reached
        if (distanceTraveled >= maxDistance)
        {
            Destroy(gameObject);
        }
    }

    public void SetDirection(Vector2 newDirection)
    {
        direction = newDirection.normalized;

        // Rotate to face direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // Damage player
        if (collision.CompareTag("Player"))
        {
            HealthModule health = collision.GetComponent<HealthModule>();
            if (health != null)
            {
                health.TakeDamage(damage);
            }
        }

        // Destroy when hitting walls (but not ground)
        if (((1 << collision.gameObject.layer) & collisionLayers) != 0 &&
            ((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            Destroy(gameObject);
        }
    }
}