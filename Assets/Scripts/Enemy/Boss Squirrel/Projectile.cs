using UnityEngine;

public class NutProjectile : MonoBehaviour
{
    public float speed = 8f;
    public int damage = 15;
    public bool leavesDebris = false;
    public GameObject debrisPrefab;

    private Vector2 direction;
    private Rigidbody2D rb;
    private bool hasBounced = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Initialize(Vector2 dir, bool canBounce)
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }
        direction = dir;
        rb.linearVelocity = direction * speed;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            collision.gameObject.GetComponent<HealthModule>().TakeDamage(damage, transform.position);
            Destroy(gameObject);
        }
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            if (leavesDebris && !hasBounced)
            {
                // First bounce
                hasBounced = true;
                // Reverse Y velocity for bounce
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -rb.linearVelocity.y * 0.7f);

                if (debrisPrefab != null)
                {
                    Instantiate(debrisPrefab, transform.position, Quaternion.identity);
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}