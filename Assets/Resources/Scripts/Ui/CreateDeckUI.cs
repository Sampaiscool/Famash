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
    public TMP_Dropdown mainRegionDropdown;
    public TMP_Dropdown secondaryRegionDropdown;
    public RectTransform cardsContent;
    public GameObject cardEntryPrefab;
    public TMP_Text deckCountText;
    public Button saveButton;
    public Button cancelButton;
    public Button deleteButton;

    private DeckData editingDeck;
    private Dictionary<string, int> deckCounts = new();
    private List<CardSelectEntry> entries = new();
    private MenuSceneUiManager menuManager;

    void Awake() => Instance = this;

    void Start()
    {
        saveButton.onClick.AddListener(OnSaveClicked);
        cancelButton.onClick.AddListener(Close);

        PopulateRegions();

        mainRegionDropdown.onValueChanged.AddListener(_ => PopulateCards());
        secondaryRegionDropdown?.onValueChanged.AddListener(_ => PopulateCards());
    }

    private void PopulateRegions()
    {
        mainRegionDropdown.ClearOptions();
        secondaryRegionDropdown.ClearOptions();

        var options = new List<string>();
        foreach (var r in DeckManager.Instance.allRegions)
            options.Add(r.regionName);

        mainRegionDropdown.AddOptions(options);
        secondaryRegionDropdown.AddOptions(options);
    }

    public void OpenNew()
    {
        editingDeck = null;
        deckNameInput.text = "";
        mainRegionDropdown.value = 0;
        secondaryRegionDropdown.value = 0;
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
        deleteButton.gameObject.SetActive(true);

        deleteButton.onClick.RemoveAllListeners();
        deleteButton.onClick.AddListener(() =>
        {
            DeckManager.Instance.RemoveDeck(deck.deckId);
            Close();
            FindFirstObjectByType<DecksScrollUI>()?.Refresh();
        });

        // Match dropdowns with saved regions
        if (!string.IsNullOrEmpty(deck.mainRegionId))
        {
            int idx1 = DeckManager.Instance.allRegions.FindIndex(r => r.regionId == deck.mainRegionId);
            mainRegionDropdown.value = Mathf.Max(0, idx1);
        }

        if (!string.IsNullOrEmpty(deck.secondaryRegionId))
        {
            int idx2 = DeckManager.Instance.allRegions.FindIndex(r => r.regionId == deck.secondaryRegionId);
            secondaryRegionDropdown.value = Mathf.Max(0, idx2);
        }

        // Build deck counts
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

    private (string main, string secondary) GetSelectedRegionIds()
    {
        var regions = DeckManager.Instance.allRegions;
        string mainId = regions[Mathf.Clamp(mainRegionDropdown.value, 0, regions.Count - 1)].regionId;
        string secondaryId = regions[Mathf.Clamp(secondaryRegionDropdown.value, 0, regions.Count - 1)].regionId;
        if (mainId == secondaryId) secondaryId = null;
        return (mainId, secondaryId);
    }

    void PopulateCards()
    {
        foreach (Transform t in cardsContent) Destroy(t.gameObject);
        entries.Clear();

        var (mainId, secondaryId) = GetSelectedRegionIds();
        var validCards = new List<CardSO>();

        foreach (var c in DeckManager.Instance.allCards)
        {
            if (c.region == null) continue;
            if (c.region.regionId == mainId || c.region.regionId == secondaryId)
                validCards.Add(c);
        }

        validCards.Sort((a, b) => string.Compare(a.cardName, b.cardName));
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
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogWarning("Deck needs a name");
            return;
        }

        var (mainId, secondaryId) = GetSelectedRegionIds();

        List<string> selectedCardIds = new();
        foreach (var kv in deckCounts)
            for (int i = 0; i < kv.Value; i++)
                selectedCardIds.Add(kv.Key);

        if (selectedCardIds.Count < 40 || selectedCardIds.Count > 60)
            Debug.LogWarning("Deck must be 40–60 cards.");

        // save
        var deck = editingDeck ?? new DeckData();
        deck.deckName = name;
        deck.mainRegionId = mainId;
        deck.secondaryRegionId = secondaryId;
        deck.cardIds = selectedCardIds;

        if (editingDeck == null)
            DeckManager.Instance.AddDeck(deck);
        else
            DeckManager.Instance.SaveDecks();

        FindFirstObjectByType<DecksScrollUI>()?.Refresh();
        Close();
    }

    public void Close()
    {
        menuManager = FindFirstObjectByType<MenuSceneUiManager>();
        menuManager?.ShowPage("Decks");
    }
}
