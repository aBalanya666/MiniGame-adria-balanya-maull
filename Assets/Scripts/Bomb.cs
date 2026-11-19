using UnityEngine;

// COLLISION (physics): the bomb falls with gravity (Rigidbody) and explodes
// when it hits anything (floor or player). The explosion damages the player
// if he is inside the radius.
// Needs: Rigidbody (Use Gravity) + Collider (NOT trigger).
public class Bomb : MonoBehaviour
{
    public float explosionRadius = 2.5f;
    public GameObject explosionFx; // optional particle prefab

    bool exploded;
    GameObject marker;                 // red circle on the floor = where the bomb will land
    static Material markerMaterial;    // shared by all bombs

    void Start()
    {
        if (markerMaterial == null)
        {
            markerMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            markerMaterial.color = new Color(0.6f, 0.05f, 0.05f);
        }
        marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Destroy(marker.GetComponent<Collider>());
        marker.transform.position = new Vector3(transform.position.x, 0.03f, transform.position.z);
        marker.transform.localScale = new Vector3(explosionRadius * 2f, 0.01f, explosionRadius * 2f);
        marker.GetComponent<Renderer>().sharedMaterial = markerMaterial;
    }

    void OnDestroy()
    {
        if (marker != null) Destroy(marker);
    }

    void OnCollisionEnter(Collision collision)
    {
        Explode();
    }

    void Explode()
    {
        if (exploded) return;
        exploded = true;

        // Find everything inside the explosion radius
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider hit in hits)
        {
            PlayerController player = hit.GetComponent<PlayerController>();
            if (player != null) player.TakeDamage();
        }

        if (explosionFx != null)
        {
            Instantiate(explosionFx, transform.position, Quaternion.identity);
        }
        else
        {
            // Fallback visual: a quick orange sphere
            GameObject fx = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(fx.GetComponent<Collider>());
            fx.transform.position = transform.position;
            fx.transform.localScale = Vector3.one * explosionRadius * 2f;
            fx.GetComponent<Renderer>().material.color = new Color(1f, 0.4f, 0f);
            Destroy(fx, 0.2f);
        }

        Destroy(gameObject);
    }
}
