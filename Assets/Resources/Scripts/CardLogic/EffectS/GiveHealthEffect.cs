using UnityEngine;

[CreateAssetMenu(menuName = "Famash/Effects/GiveHealth")]
public class GiveHealthEffect : EffectSOBase
{
    public override void Apply(CardRuntime card, EffectParams parameters)
    {
        card.ModifyStats(0, parameters.amount1, true);
    }
}
