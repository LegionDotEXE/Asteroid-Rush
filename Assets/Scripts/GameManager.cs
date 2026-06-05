using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
 
    [Header("Run")]
    public float gameDuration = 30f;
 
    [Header("Refs")]
    public ShipHealth shipHealth;   // assign the ship; we listen for its death
 
    public enum State { Menu, Playing, Won, Lost }
    public State CurrentState { get; private set; } = State.Menu;
 
    // Subscribe to these from HUD / feedback / spawners.
    public event Action OnGameStarted;
    public event Action OnGameWon;
    public event Action OnGameLost;
 
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
    void OnEnable()  
    {
        if (shipHealth != null) shipHealth.OnDied += HandleShipDied; 
    }
    void OnDisable() 
    { 
        if (shipHealth != null) shipHealth.OnDied -= HandleShipDied; 
    }

    void Update()
    {
        if (CurrentState != State.Playing) return;
 
        timeRemaining = Mathf.Max(0f, timeRemaining - Time.deltaTime);
        if (timeRemaining <= 0f) Win();
    }

    public void StartGame()
    {
        timeRemaining = gameDuration;
        CurrentState = State.Playing;
        if (shipHealth != null) shipHealth.ResetHull();
        OnGameStarted?.Invoke();   // spawners clear, ship recenters, HUD resets
    }

    void Win()
    {
        if (CurrentState != State.Playing) return;
        CurrentState = State.Won;          // "engine sputters out as you clear the belt"
        OnGameWon?.Invoke();
    }
 
    public void Lose()
    {
        if (CurrentState != State.Playing) return;
        CurrentState = State.Lost;
        OnGameLost?.Invoke();
    }

    void HandleShipDied() => Lose();

    public float GetTimeRemaining() => timeRemaining;
    public float GetElapsed()       => gameDuration - timeRemaining;
    public bool IsGameActive()      => CurrentState == State.Playing;
}
