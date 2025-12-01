using UnityEngine;

[CreateAssetMenu(menuName = "Famash/Effects/HealUnit")]
public class HealUnitEffect : EffectSOBase
{
    public override void Apply(CardRuntime card, EffectParams parameters)
    {
        card.ModifyStats(0, parameters.amount1, false);
    }
}
