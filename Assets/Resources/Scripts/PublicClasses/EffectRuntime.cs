using UnityEngine;

[System.Serializable]
public class RuntimeEffect
{
    public EffectSOBase effect;
    public EffectParams parameters;
    public CardTrigger trigger;
    public bool isOncePerTurn;
    public bool hasBeenUsedThisTurn;
}

