using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public GameConfig CurrentConfig;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void StartJourney(GameConfig cfg)
    {
        CurrentConfig = cfg;
        // load your battle scene name
        SceneManager.LoadScene("BattleScene");
    }
}
