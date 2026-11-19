using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

// Game rules + CANVAS TEXT (HUD).
// Goal: collect 'coinsToWin' records before the time runs out, then reach the door.
// Challenge: falling bombs (lives) + countdown timer.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Rules")]
    public int coinsToWin = 10;
    public int startLives = 3;
    public float timeLimit = 90f;

    [Header("References")]
    public PlayerController player;
    public Door door;
    public GameObject coinPrefab;
    public Vector2 arenaHalfSize = new Vector2(9f, 9f);

    [Header("UI (TextMeshPro)")]
    public TMP_Text coinsText;
    public TMP_Text livesText;
    public TMP_Text timerText;
    public TMP_Text dashText;
    public TMP_Text messageText;

    int coins;
    int lives;
    float timeLeft;
    float messageTimer; // seconds left before the message hides (0 = stays)

    public bool GameOver { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        coins = 0;
        lives = startLives;
        timeLeft = timeLimit;
        GameOver = false;
        ShowMessage("Collect " + coinsToWin + " records!", 3f);
        SpawnCoins();
        UpdateUI();
    }

    void Update()
    {
        if (GameOver)
        {
            // INPUT: restart with R
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return;
        }

        // Hide temporary messages after a few seconds
        if (messageTimer > 0f)
        {
            messageTimer -= Time.deltaTime;
            if (messageTimer <= 0f) messageText.text = "";
        }

        timeLeft -= Time.deltaTime;
        if (timeLeft <= 0f)
        {
            timeLeft = 0f;
            Lose("Time's up!");
        }
        UpdateUI();
    }

    void SpawnCoins()
    {
        for (int i = 0; i < coinsToWin; i++)
        {
            Vector3 pos = new Vector3(
                Random.Range(-arenaHalfSize.x, arenaHalfSize.x),
                0.8f,
                Random.Range(-arenaHalfSize.y, arenaHalfSize.y));
            Instantiate(coinPrefab, pos, Quaternion.identity);
        }
    }

    public void AddCoin()
    {
        if (GameOver) return;
        coins++;
        if (coins >= coinsToWin)
        {
            door.Open();
            ShowMessage("The door is open! Get out!", 4f);
        }
        UpdateUI();
    }

    public void LoseLife()
    {
        if (GameOver) return;
        lives--;
        if (lives <= 0) Lose("You got blown up!");
        UpdateUI();
    }

    public void Win()
    {
        if (GameOver) return;
        GameOver = true;
        messageTimer = 0f;
        messageText.text = "YOU WIN!\nPress R to restart";
    }

    void Lose(string reason)
    {
        GameOver = true;
        messageTimer = 0f;
        messageText.text = reason + "\nPress R to restart";
    }

    void ShowMessage(string text, float seconds)
    {
        messageText.text = text;
        messageTimer = seconds;
    }

    void UpdateUI()
    {
        coinsText.text = "Records: " + coins + "/" + coinsToWin;
        livesText.text = "Lives: " + lives;
        timerText.text = "Time: " + Mathf.CeilToInt(timeLeft);
        dashText.text = player.DashReady ? "Dash: READY (click)" : "Dash: ...";
    }
}
