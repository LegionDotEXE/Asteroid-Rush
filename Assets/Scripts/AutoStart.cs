using UnityEngine;

public class AutoStart : MonoBehaviour
{
    void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.StartGame();
    }
}