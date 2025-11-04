using UnityEngine;

[System.Serializable]
public class CardRuntime
{
    public string instanceId;
    public CardSO cardData;

    public int currentHealth;
    public int currentAttack;
    public bool isExhausted;
    public bool isInPlay;
    public bool isDead;

    [System.NonSerialized]
    public GameObject cardUI;  // reference to the spawned UI prefab

    public CardRuntime(CardSO source)
    {
        instanceId = System.Guid.NewGuid().ToString();
        cardData = source;
        currentHealth = source.health;
        currentAttack = source.attack;
        isExhausted = false;
        isInPlay = false;
        isDead = false;
        cardUI = null;
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            isDead = true;
        }
    }

    public void Play()
    {
        Debug.Log($"Playing {cardData.cardName}");
        //GameManager.Instance.ResolveCardTrigger(cardData, CardTrigger.OnPlay);
    }
}
