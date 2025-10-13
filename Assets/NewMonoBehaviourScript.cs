using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    public LayerMask layerMask;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HealthModule healthModule = collision.gameObject.GetComponent<HealthModule>();

        // Check if healthModule exists and if the collided object's layer is in the layerMask
        if (healthModule == null || ((1 << collision.gameObject.layer) & layerMask) == 0)
        {
            print(collision.gameObject.name);
            return;
        }

        healthModule.TakeDamage(1);
    }
}
