using UnityEngine;

// The challenge: bombs fall from the sky, faster and faster as time passes.
// Some of them are aimed at the player's position so he can't just stand still.
public class BombSpawner : MonoBehaviour
{
    public GameObject bombPrefab;
    public Transform player;
    public Vector2 arenaHalfSize = new Vector2(9f, 9f);
    public float spawnHeight = 15f;

    [Header("Difficulty")]
    public float startInterval = 1.5f;
    public float minInterval = 0.5f;
    public float rampPerSecond = 0.01f; // interval shrinks over time
    [Range(0f, 1f)] public float aimedChance = 0.4f;

    float timer;

    void Update()
    {
        if (GameManager.Instance.GameOver) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        float interval = Mathf.Max(minInterval, startInterval - Time.timeSinceLevelLoad * rampPerSecond);
        timer = interval;
        SpawnBomb();
    }

    void SpawnBomb()
    {
        Vector3 pos;
        if (Random.value < aimedChance)
        {
            // Above the player (with a little random offset)
            pos = player.position + new Vector3(Random.Range(-1.5f, 1.5f), 0f, Random.Range(-1.5f, 1.5f));
        }
        else
        {
            pos = new Vector3(Random.Range(-arenaHalfSize.x, arenaHalfSize.x), 0f, Random.Range(-arenaHalfSize.y, arenaHalfSize.y));
        }
        pos.y = spawnHeight;
        Instantiate(bombPrefab, pos, Quaternion.identity);
    }
}
