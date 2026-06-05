using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public float gameDuration = 30f;

    bool gameStarted = false;
    float timeRemaining;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        StartGame();
    }

    void Update()
    {
        if (!gameStarted) return;

        timeRemaining -= Time.deltaTime;
        timeRemaining = Mathf.Max(timeRemaining, 0f);

        Debug.Log("Time remaining: " + timeRemaining.ToString("F1"));

        if (timeRemaining <= 0f)
        {
            gameStarted = false;
            Debug.Log("Escaped.");
        }
    }

    public void StartGame()
    {
        timeRemaining = gameDuration;
        gameStarted = true;
    }

    public float GetTimeRemaining() => timeRemaining;
    public bool IsGameActive() => gameStarted;

    public void TriggerLoss()
    {
        gameStarted = false;
        Debug.Log("Engine failure.");
    }
}
