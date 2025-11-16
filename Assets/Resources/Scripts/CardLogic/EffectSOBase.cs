using UnityEngine;
public abstract class EffectSOBase : ScriptableObject
{
    public string effectName;
    [TextArea] public string description;

    // The actual effect logic, using the parameters passed from the card
    public abstract void Apply(CardRuntime card, EffectParams parameters);
}

