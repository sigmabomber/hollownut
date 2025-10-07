using UnityEngine;


public interface IKnockbackable
{
    void ApplyKnockback(KnockbackData data);
}


[System.Serializable]
public struct KnockbackData
{
    public Vector2 Direction;
    public float Force;
    public float Duration;

public KnockbackData(Vector2 direction, float force, float duration)
    {
        Direction = direction.normalized;
        Force = force;
        Duration = duration;
    }

}


public class KnockbackManager : MonoBehaviour
{
    public static KnockbackManager Instance { get; private set; }


private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void ApplyKnockback(Rigidbody2D rb, KnockbackData data)
    {
        if (rb == null) return;

        rb.linearVelocity = Vector2.zero;
        rb.AddForce(data.Direction * data.Force, ForceMode2D.Impulse);
        StartCoroutine(ResetVelocity(rb, data.Duration));
    }

    private System.Collections.IEnumerator ResetVelocity(Rigidbody2D rb, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }


}


[RequireComponent(typeof(Rigidbody2D))]
public class KnockbackReceiver : MonoBehaviour, IKnockbackable
{
    private Rigidbody2D rb;


private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void ApplyKnockback(KnockbackData data)
    {
        if (KnockbackManager.Instance != null)
            KnockbackManager.Instance.ApplyKnockback(rb, data);
    }


}
