using UnityEngine;

public class TelegraphIndicator : MonoBehaviour
{
    [Header("Telegraph Settings")]
    public float lifeTime = 1f;
    public AnimationCurve alphaCurve;
    public AnimationCurve scaleCurve;

    private float spawnTime;
    private SpriteRenderer spriteRenderer;
    private Vector3 originalScale;

    void Start()
    {
        spawnTime = Time.time;
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalScale = transform.localScale;

        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        float progress = (Time.time - spawnTime) / lifeTime;

        // Update alpha
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = alphaCurve.Evaluate(progress);
            spriteRenderer.color = color;
        }

        // Update scale
        float scaleMultiplier = scaleCurve.Evaluate(progress);
        transform.localScale = originalScale * scaleMultiplier;
    }
}