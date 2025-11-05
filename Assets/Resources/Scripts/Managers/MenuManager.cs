using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance { get; private set; }

    public DeckData currentDeck;

    private MenuSceneUiManager menuUiManager;
    private DefaultUi defaultUi;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

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

    public void OnClickJourney()
    {
        if (currentDeck == null)
        {
            Debug.LogWarning("No deck selected");
            return;
        }

        var cfg = new GameConfig()
        {
            deckId = currentDeck.deckId,
            mainRegionId = currentDeck.mainRegionId,
            secondaryRegionId = currentDeck.secondaryRegionId,
            seed = Random.Range(0, int.MaxValue)
        };

        GameManager.Instance.StartJourney(cfg);
    }

    public Canvas FindUiCanvas() => FindFirstObjectByType<Canvas>();
}
