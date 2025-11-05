using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DeckRuntime
{
    public string deckId;
    public string deckName;

    public HeroRuntime hero;              // still used for combat stats (HP/mana)
    public RegionSO mainRegion;            // main region bonus
    public RegionSO secondaryRegion;       // optional secondary region
    public List<CardSO> baseCards = new();
    public List<CardRuntime> runtimeCards = new();

    public DeckRuntime(DeckData data)
    {
        deckId = data.deckId;
        deckName = data.deckName;

        // Assign regions
        mainRegion = DeckManager.Instance.GetRegionById(data.mainRegionId);
        if (!string.IsNullOrEmpty(data.secondaryRegionId))
            secondaryRegion = DeckManager.Instance.GetRegionById(data.secondaryRegionId);

        // Create hero runtime (generic stats)
        hero = new HeroRuntime(mainRegion); // now stats are universal, no HeroSO needed
        hero.mainRegion = mainRegion;

        // Load cards
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
