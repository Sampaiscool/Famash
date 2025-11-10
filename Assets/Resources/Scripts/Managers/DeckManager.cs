using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    public static DeckManager Instance { get; private set; }

    [Header("Data Sources")]
    public List<RegionSO> allRegions;
    public List<CardSO> allCards;

    public List<DeckData> decks = new();

    private string savePath;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        string folder = Application.persistentDataPath;
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        savePath = Path.Combine(folder, "decks.json");

        LoadAllCards();
        LoadAllRegions();
        LoadDecks();
    }

    private void LoadAllCards()
    {
        allCards = new List<CardSO>(Resources.LoadAll<CardSO>("Cards"));
    }

    private void LoadAllRegions()
    {
        allRegions = new List<RegionSO>(Resources.LoadAll<RegionSO>("Regions"));
    }

    #region Persistence
    [Serializable]
    private class DeckCollection { public List<DeckData> decks; }

    public void SaveDecks()
    {
        try
        {
            var json = JsonUtility.ToJson(new DeckCollection { decks = decks }, true);
            File.WriteAllText(savePath, json);
            Debug.Log($"Saved {decks.Count} decks to {savePath}");
        }
        catch (Exception e)
        {
            Debug.LogError("SaveDecks error: " + e);
        }
    }
    public void AddDeck(DeckData newDeck)
    {
        // Assign unique ID if missing
        if (string.IsNullOrEmpty(newDeck.deckId))
            newDeck.deckId = Guid.NewGuid().ToString();

        // Add and save
        decks.Add(newDeck);
        SaveDecks();
        Debug.Log($"Added new deck: {newDeck.deckName}");
    }

    public void RemoveDeck(string deckId)
    {
        int before = decks.Count;
        decks.RemoveAll(d => d.deckId == deckId);
        if (decks.Count < before)
        {
            SaveDecks();
            Debug.Log($"Removed deck {deckId}");
        }
        else
        {
            Debug.LogWarning($"Tried to remove deck {deckId}, but it wasn't found.");
        }
    }


    public void LoadDecks()
    {
        decks.Clear();
        if (!File.Exists(savePath)) return;

        try
        {
            var json = File.ReadAllText(savePath);
            var container = JsonUtility.FromJson<DeckCollection>(json);
            decks = container?.decks ?? new List<DeckData>();
            Debug.Log($"Loaded {decks.Count} decks from {savePath}");
        }
        catch (Exception e)
        {
            Debug.LogError("LoadDecks error: " + e);
        }
    }
    #endregion

    #region Helpers
    public RegionSO GetRegionById(string id) =>
        allRegions.Find(r => r != null && r.regionId == id);

    public CardSO GetCardById(string id) =>
        allCards.Find(c => c != null && c.cardId == id);
    public DeckData GetDeck(string deckId)
    {
        return decks.Find(d => d != null && d.deckId == deckId);
    }
    #endregion
}
