using UnityEngine;

[System.Serializable]
public class HeroRuntime
{
    public HeroSO heroData;
    public int currentHealth;
    public int currentMana;
    public int maxMana;

    public HeroRuntime(HeroSO data)
    {
        heroData = data;
        currentHealth = data.baseHealth;
        currentMana = data.resourceStart;
        maxMana = data.resourceStart;
    }

    // Method to increase current mana
    public void GainMana(int amount)
    {
        currentMana = Mathf.Min(maxMana, currentMana + amount);
    }

    // Method to reduce current mana when a card is played
    public void SpendMana(int amount)
    {
        currentMana = Mathf.Max(0, currentMana - amount);
    }

    // Check if the hero can afford the card's cost
    public bool CanAfford(CardSO card)
    {
        return currentMana >= card.cost;
    }

    // At the start of each turn, refresh the mana and increase max mana
    public void StartTurn()
    {
        currentMana = maxMana;

        maxMana = Mathf.Min(heroData.resourceMax, maxMana + 1);
    }
}
