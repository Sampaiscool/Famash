using UnityEngine;

[System.Serializable]
public class CardRuntime
{
    public CardSO cardData;
    public int currentHealth;
    public int currentAttack;
    public bool isOnField;

    public CardRuntime(CardSO data)
    {
        cardData = data;
        currentHealth = data.health;
        currentAttack = data.attack;
    }

    public void Play()
    {
        Debug.Log($"Playing {cardData.cardName}");
        //GameManager.Instance.ResolveCardTrigger(cardData, CardTrigger.OnPlay);
    }
}
