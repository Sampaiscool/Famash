using UnityEngine;

[CreateAssetMenu(menuName = "Famash/Effects/DealDamage")]
public class DealDamageEffect : EffectSOBase
{
    public override void Apply(CardRuntime card, EffectParams parameters)
    {
        card.owner.hero.TakeDamage(parameters.amount1);
    }
}

