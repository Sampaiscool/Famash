using UnityEngine;

public abstract class EffectSOBase : ScriptableObject
{
    public string effectName;
    [TextArea] public string description;

    // Called when the card is played (you’ll expand later)
    public abstract void OnPlay(CardRuntime card);
}
