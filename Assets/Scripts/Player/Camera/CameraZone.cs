using UnityEngine;

public class CameraZone : MonoBehaviour
{
    public Vector2 minBounds; 
    public Vector2 maxBounds;
    public CameraController cam;

    public new Transform camera;
    private void OnDrawGizmos()
    {
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


        if (cam != null && collision.CompareTag("Player"))
        {
            cam.SetZone(this);
        }
            
    }
}
