using UnityEngine;

public class CameraZone : MonoBehaviour
{
    public Vector2 minBounds; // Bottom-left corner of zone
    public Vector2 maxBounds; // Top-right corner of zone
    public Transform camera;
    private void OnDrawGizmos()
    {
        // Visualize the zone in editor
        Gizmos.color = Color.green;
        Vector3 center = new Vector3(
            (minBounds.x + maxBounds.x) / 2,
            (minBounds.y + maxBounds.y) / 2,
            0
        );
        Vector3 size = new Vector3(
            maxBounds.x - minBounds.x,
            maxBounds.y - minBounds.y,
            0
        );
        Gizmos.DrawWireCube(center, size);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        print(collision.gameObject.name);
        CameraController cam = camera.GetComponent<CameraController>();

        if (cam == null)
            print("null");
        if (cam != null && collision.CompareTag("Player"))
        {
            print("setting zone");
            cam.SetZone(this);
        }
    }
}
