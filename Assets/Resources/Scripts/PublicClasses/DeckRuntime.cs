using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DeckRuntime
{
    public string deckId;
    public string deckName;

    public HeroRuntime hero;              // active hero in this battle
    public List<CardSO> baseCards = new(); // static references for info
    public List<CardRuntime> runtimeCards = new(); // mutable state

    public DeckRuntime(DeckData data)
    {
        deckId = data.deckId;
        deckName = data.deckName;

        // Grab hero (for now assume only first heroId is used)
        if (data.heroIds.Count > 0)
        {
            var heroSo = DeckManager.Instance.GetHeroById(data.heroIds[0]);
            if (heroSo != null)
                hero = new HeroRuntime(heroSo);
            else
                Debug.LogWarning($"Hero with id {data.heroIds[0]} not found for deck {data.deckName}");
        }

        // Load cards by ID
        foreach (var id in data.cardIds)
        {
            var card = DeckManager.Instance.GetCardById(id);
            if (card != null)
            {
                baseCards.Add(card);
                runtimeCards.Add(new CardRuntime(card));
            }
            else
                Debug.LogWarning($"Card with id {id} not found for deck {data.deckName}");
        }

        Shuffle();
    }
    public void Shuffle()
    {
        for (int i = runtimeCards.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var temp = runtimeCards[i];
            runtimeCards[i] = runtimeCards[j];
            runtimeCards[j] = temp;
        }
    }

    public CardRuntime DrawCard()
    {
        if (runtimeCards.Count == 0)
        {
            Debug.Log($"{deckName} is out of cards!");
            return null;
        }

        var card = runtimeCards[0];
        runtimeCards.RemoveAt(0);
        return card;
    }
}
