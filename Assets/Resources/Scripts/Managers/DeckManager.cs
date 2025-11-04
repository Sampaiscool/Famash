using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    public static DeckManager Instance { get; private set; }

    // Data sources you assign in inspector
    public List<HeroSO> allHeroes;
    public List<CardSO> allCards;

    // runtime
    public List<DeckData> decks = new List<DeckData>();

    private string savePath;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Make sure the folder exists (optional, but safe)
        string folder = Application.persistentDataPath;
        if (!System.IO.Directory.Exists(folder))
            System.IO.Directory.CreateDirectory(folder);

        savePath = System.IO.Path.Combine(folder, "decks.json");

        LoadAllCards();
        LoadAllHeroes();
        LoadDecks();
    }

    private void LoadAllCards()
    {
        allCards = new List<CardSO>(Resources.LoadAll<CardSO>("Cards"));
        Debug.Log($"Loaded {allCards.Count} cards from Resources/Cards");
    }
    private void LoadAllHeroes()
    {
        allHeroes = new List<HeroSO>(Resources.LoadAll<HeroSO>("Heroes"));
        Debug.Log($"Loaded {allHeroes.Count} heroes from Resources/Heroes");
    }

    #region Persistence
    [Serializable]
    private class DeckCollection { public List<DeckData> decks; }

    public void SaveDecks()
    {
        try
        {
            var container = new DeckCollection { decks = decks };
            var json = JsonUtility.ToJson(container, true);
            System.IO.File.WriteAllText(savePath, json);
            Debug.Log($"Saved {decks.Count} decks to {savePath}");
        }
        catch (Exception e)
        {
            Debug.LogError("SaveDecks error: " + e);
        }
    }


    public void LoadDecks()
    {
        decks.Clear();
        if (!System.IO.File.Exists(savePath)) return;

        try
        {
            string json = System.IO.File.ReadAllText(savePath);
            var container = JsonUtility.FromJson<DeckCollection>(json);
            if (container != null && container.decks != null)
                decks = container.decks;

            Debug.Log($"Loaded {decks.Count} decks from {savePath}");
        }
        catch (Exception e)
        {
            Debug.LogError("LoadDecks error: " + e);
        }
    }

    #endregion

    #region CRUD
    public void AddDeck(DeckData d)
    {
        if (string.IsNullOrEmpty(d.deckId)) d.deckId = Guid.NewGuid().ToString();
        decks.Add(d);
        SaveDecks();
    }

    public void RemoveDeck(string deckId)
    {
        decks.RemoveAll(x => x.deckId == deckId);
        SaveDecks();
    }

    public DeckData GetDeck(string deckId)
    {
        return decks.Find(d => d.deckId == deckId);
    }
    #endregion

    #region Helpers
    public HeroSO GetHeroById(string id) => allHeroes.Find(h => h != null && h.heroId == id);
    public CardSO GetCardById(string id) => allCards.Find(c => c != null && c.cardId == id);
    #endregion
}
