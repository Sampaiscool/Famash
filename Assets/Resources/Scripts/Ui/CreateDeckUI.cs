using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CreateDeckUI : MonoBehaviour
{
    public static CreateDeckUI Instance { get; private set; }

    [Header("Form Fields")]
    public TMP_InputField deckNameInput;
    public TMP_Dropdown hero1Dropdown;
    public TMP_Dropdown hero2Dropdown; // optional second hero
    public RectTransform cardsContent;
    public GameObject cardEntryPrefab; // CardSelectEntry prefab
    public TMP_Text deckCountText;
    public Button saveButton;
    public Button cancelButton;
    public Button deleteButton;

    private DeckData editingDeck;
    private Dictionary<string, int> deckCounts = new(); // cardId -> count
    private List<CardSelectEntry> entries = new();

    private MenuSceneUiManager menuManager;

    void Awake() => Instance = this;

    void Start()
    {
        saveButton.onClick.AddListener(OnSaveClicked);
        cancelButton.onClick.AddListener(Close);

        PopulateHeroes();

        // Optional: refresh cards whenever hero dropdown changes
        hero1Dropdown.onValueChanged.AddListener(_ => PopulateCards());
        hero2Dropdown?.onValueChanged.AddListener(_ => PopulateCards());
    }

    public void OpenNew()
    {
        editingDeck = null;
        deckNameInput.text = "";
        hero1Dropdown.value = 0;
        hero2Dropdown.value = 0;
        deckCounts.Clear();
        PopulateCards();
        UpdateDeckCount();

        deleteButton.gameObject.SetActive(false);

        gameObject.SetActive(true);
    }

    public void OpenWithDeck(DeckData deck)
    {
        editingDeck = deck;
        deckNameInput.text = deck.deckName;

        // Show and wire delete button
        deleteButton.gameObject.SetActive(true);
        deleteButton.onClick.RemoveAllListeners(); // clear old listeners
        deleteButton.onClick.AddListener(() =>
        {
            DeckManager.Instance.RemoveDeck(deck.deckId);
            Close();

            // Refresh the scroll list
            var scrollUI = FindFirstObjectByType<DecksScrollUI>();
            scrollUI?.Refresh();
        });

        // select heroes
        if (deck.heroIds.Count > 0)
        {
            int idx1 = DeckManager.Instance.allHeroes.FindIndex(h => h.heroId == deck.heroIds[0]);
            hero1Dropdown.value = Mathf.Max(0, idx1);
        }
        if (deck.heroIds.Count > 1 && hero2Dropdown != null)
        {
            int idx2 = DeckManager.Instance.allHeroes.FindIndex(h => h.heroId == deck.heroIds[1]);
            hero2Dropdown.value = Mathf.Max(0, idx2);
        }

        // build deck counts
        deckCounts.Clear();
        foreach (var cid in deck.cardIds)
        {
            if (!deckCounts.ContainsKey(cid)) deckCounts[cid] = 0;
            deckCounts[cid]++;
        }

        PopulateCards();
        UpdateDeckCount();

        menuManager = FindFirstObjectByType<MenuSceneUiManager>();
        menuManager?.ShowPage("CreateDeck");
    }

    private void PopulateHeroes()
    {
        hero1Dropdown.ClearOptions();
        hero2Dropdown?.ClearOptions();
        var options = new List<string>();
        foreach (var h in DeckManager.Instance.allHeroes) options.Add(h.heroName);
        hero1Dropdown.AddOptions(options);
        hero2Dropdown?.AddOptions(options);
    }

    private List<string> GetSelectedHeroIds()
    {
        List<string> ids = new List<string>();
        if (DeckManager.Instance.allHeroes.Count == 0) return ids;

        // primary hero
        int idx1 = Mathf.Clamp(hero1Dropdown.value, 0, DeckManager.Instance.allHeroes.Count - 1);
        ids.Add(DeckManager.Instance.allHeroes[idx1].heroId);

        // optional second hero
        if (hero2Dropdown != null)
        {
            int idx2 = Mathf.Clamp(hero2Dropdown.value, 0, DeckManager.Instance.allHeroes.Count - 1);
            string id2 = DeckManager.Instance.allHeroes[idx2].heroId;
            if (!ids.Contains(id2)) ids.Add(id2);
        }

        return ids;
    }

    void PopulateCards()
    {
        // clear old entries
        foreach (Transform t in cardsContent) Destroy(t.gameObject);
        entries.Clear();

        var selectedHeroIds = GetSelectedHeroIds();

        // gather all allowed cards
        List<CardSO> validCards = new List<CardSO>();
        foreach (var c in DeckManager.Instance.allCards)
        {
            // show general cards or those belonging to selected heroes
            if (c.hero == null || selectedHeroIds.Contains(c.hero.heroId))
                validCards.Add(c);
        }

        // sort alphabetically by name
        validCards.Sort((a, b) => string.Compare(a.cardName, b.cardName));

        // spawn entries
        foreach (var card in validCards)
        {
            var go = Instantiate(cardEntryPrefab, cardsContent);
            var entry = go.GetComponent<CardSelectEntry>();
            entry.Bind(card);
            entry.SetCount(deckCounts.TryGetValue(card.cardId, out int count) ? count : 0);
            entry.onCountChanged += OnCardCountChanged;
            entries.Add(entry);
        }
    }

    private void OnCardCountChanged(CardSO card, int newCount)
    {
        deckCounts[card.cardId] = newCount;
        UpdateDeckCount();
    }

    private void UpdateDeckCount()
    {
        int total = 0;
        foreach (var kv in deckCounts) total += kv.Value;
        deckCountText.text = $"Cards: {total}/60";
    }

    private void OnSaveClicked()
    {
        var name = deckNameInput.text.Trim();
        if (string.IsNullOrEmpty(name)) { Debug.LogWarning("Deck needs a name"); return; }

        var selectedHeroIds = GetSelectedHeroIds();
        if (selectedHeroIds.Count > 2) { Debug.LogWarning("At most 2 heroes per deck."); return; }

        // build selected cardIds
        List<string> selectedCardIds = new List<string>();
        foreach (var kv in deckCounts)
            for (int i = 0; i < kv.Value; i++)
                selectedCardIds.Add(kv.Key);

        // validation
        if (selectedCardIds.Count < 40 || selectedCardIds.Count > 60)
        {
            Debug.LogWarning("Deck must be 40–60 cards.");
            //return;
        }

        var counts = new Dictionary<string, int>();
        foreach (var cid in selectedCardIds)
        {
            if (!counts.ContainsKey(cid)) counts[cid] = 0;
            counts[cid]++;
            if (counts[cid] > 3)
            {
                var card = DeckManager.Instance.GetCardById(cid);
                Debug.LogWarning($"Card {card?.cardName ?? cid} exceeds 3 copies.");
                return;
            }

            var cardData = DeckManager.Instance.GetCardById(cid);
            if (cardData.hero != null && !selectedHeroIds.Contains(cardData.hero.heroId))
            {
                Debug.LogWarning($"Card {cardData.cardName} belongs to hero {cardData.hero.heroName}, not in selected heroes.");
                return;
            }
        }

        // hero card enforcement: exactly 3 copies
        foreach (var heroId in selectedHeroIds)
        {
            var hero = DeckManager.Instance.GetHeroById(heroId);
            if (hero != null && hero.heroCard != null)
            {
                counts.TryGetValue(hero.heroCard.cardId, out int present);
                if (present != 3)
                {
                    Debug.LogWarning($"Hero {hero.heroName} requires exactly 3 copies of {hero.heroCard.cardName}. Found {present}");
                    return;
                }
            }
        }

        // save deck
        var deck = editingDeck ?? new DeckData();
        deck.deckName = name;
        deck.heroIds = selectedHeroIds;
        deck.cardIds = selectedCardIds;

        if (editingDeck == null)
            DeckManager.Instance.AddDeck(deck);
        else
            DeckManager.Instance.SaveDecks();

        var scrollUI = FindFirstObjectByType<DecksScrollUI>();
        scrollUI?.Refresh();

        Close();
    }

    public void Close()
    {
        menuManager = FindFirstObjectByType<MenuSceneUiManager>();
        menuManager?.ShowPage("Decks");
    }
}
