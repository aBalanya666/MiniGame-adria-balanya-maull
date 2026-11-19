using UnityEngine;

// Simple smooth camera that follows the player from above.
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 11f, -8f);
    public float smoothSpeed = 6f;

    void LateUpdate()
    {
        Vector3 desired = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
        transform.LookAt(target.position);
    }
}
