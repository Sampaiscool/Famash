using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance { get; private set; }

    public DeckData currentDeck;

    private MenuSceneUiManager menuUiManager;
    private DefaultUi defaultUi;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void SetCurrentDeck(DeckData deck)
    {
        currentDeck = deck;

        PlayerPrefs.SetString("SelectedDeckId", deck.deckId);
        PlayerPrefs.Save();

        Debug.Log($"Selected deck: {deck.deckName}");

        defaultUi = FindFirstObjectByType<DefaultUi>();
        menuUiManager = FindFirstObjectByType<MenuSceneUiManager>();
        if (menuUiManager != null)
        {
            defaultUi.LoadSelectedDeck();
            menuUiManager.ShowPage("Default");
        }
    }

    public void OnClickHome()
    {
        // show home panel; you can load scenes or toggle panels
        Debug.Log("Home clicked");
    }

    public void OnClickJourney()
    {
        if (currentDeck == null) { Debug.LogWarning("No deck selected"); return; }

        // Prepare GameConfig, pass to GameManager and load battle scene
        var cfg = new GameConfig()
        {
            deckId = currentDeck.deckId,
            heroIds = currentDeck.heroIds,  // Use heroIds, which is a list now
            seed = UnityEngine.Random.Range(0, int.MaxValue)
        };

        GameManager.Instance.StartJourney(cfg);
    }


    public Canvas FindUiCanvas()
    {
        return FindFirstObjectByType<Canvas>();
    }
}
