using UnityEngine;

public class BattleLoader : MonoBehaviour
{
    public static BattleLoader Instance { get; private set; }

    public DeckData playerDeckData;
    public DeckData enemyDeckData;

    public DeckRuntime playerDeckRuntime;
    public DeckRuntime enemyDeckRuntime;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void PreparePlayerDeck(DeckData deck)
    {
        playerDeckData = deck;
        playerDeckRuntime = new DeckRuntime(deck);
    }

    public void PrepareEnemyDeck(DeckData deck)
    {
        enemyDeckData = deck;
        enemyDeckRuntime = new DeckRuntime(deck);
    }
}
