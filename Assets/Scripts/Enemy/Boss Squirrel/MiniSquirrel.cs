using UnityEngine;

public class MiniSquirrel : MonoBehaviour
{
    public int damage = 1;
    public float moveSpeed = 3f;

    private Transform player;
    private HealthModule healthModule;
    private EnemyModule enemyModule;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        healthModule = GetComponent<HealthModule>();
        enemyModule = GetComponent<EnemyModule>();

        // Initialize modules
        if (healthModule != null)
        {
            healthModule.Initialize(10f); // Low health for mini squirrels
            healthModule.onDeath += Die;
        }

        if (enemyModule != null)
        {
            enemyModule.Initialize(
                cooldown: 1f,
                detectionRange: 8f,
                attackRange: 1f,
                layerMask: LayerMask.GetMask("Player")
            );
            enemyModule.OnStartAttack += OnAttackPlayer;
        }
    }

    void Update()
    {
        if (healthModule != null && healthModule.currentHealth <= 0) return;

        // Simple movement towards player
        if (player != null)
        {
            Vector2 direction = (player.position - transform.position).normalized;
            transform.Translate(direction * moveSpeed * Time.deltaTime);

            // Face player
            transform.localScale = new Vector3(
                Mathf.Sign(direction.x),
                1, 1
            );
        }
    }

    void OnAttackPlayer()
    {
        if (player != null)
        {
            HealthModule playerHealth = player.GetComponent<HealthModule>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
        }
    }

    void Die()
    {
        // Death animation/effects can go here
        Destroy(gameObject, 0.1f);
    }

    void OnDestroy()
    {
        if (healthModule != null)
            healthModule.onDeath -= Die;

        if (enemyModule != null)
            enemyModule.OnStartAttack -= OnAttackPlayer;
    }
}