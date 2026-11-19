using UnityEngine;

// COLLISION (trigger): the record is collected when the player touches it.
// Needs: Collider with "Is Trigger" checked.
public class Coin : MonoBehaviour
{
    public float rotateSpeed = 120f;
    public float bobHeight = 0.25f;
    public float bobSpeed = 2f;

    Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    // Simple animation by code: spin + float up and down
    void Update()
    {
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
        float y = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = startPos + Vector3.up * y;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.Instance.AddCoin();
            Destroy(gameObject);
        }
    }
}
