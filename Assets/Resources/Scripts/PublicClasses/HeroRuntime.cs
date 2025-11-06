using UnityEngine;

[System.Serializable]
public class HeroRuntime
{
    public RegionSO mainRegion;
    public int currentHealth;
    public int currentMana;
    public int maxMana;

    private const int BASE_HEALTH = 20;
    private const int START_MANA = 1;
    private const int MAX_MANA_CAP = 10;

    public HeroRuntime(RegionSO region)
    {
        mainRegion = region;
        currentHealth = BASE_HEALTH;
        currentMana = START_MANA;
        maxMana = START_MANA;
    }

    public void StartTurn()
    {
        // Just refill — no bonus mana here
        currentMana = maxMana;
    }
    public void TakeDamage(int amount)
    {
        currentHealth = Mathf.Max(0, currentHealth - amount);
        BattleUIManager.Instance.UpdateHeroUI();
    }

    public void EndTurnGainMana()
    {
        // Like Runeterra — after each player’s turn, both heroes grow
        maxMana = Mathf.Min(MAX_MANA_CAP, maxMana + 1);
        currentMana = maxMana;
    }

    public void GainMana(int amount)
    {
        currentMana = Mathf.Min(maxMana, currentMana + amount);
    }

    public void SpendMana(int amount)
    {
        currentMana = Mathf.Max(0, currentMana - amount);
    }

    public bool CanAfford(CardSO card)
    {
        return currentMana >= card.cost;
    }
}
