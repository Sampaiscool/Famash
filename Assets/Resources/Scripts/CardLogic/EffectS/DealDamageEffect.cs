using UnityEngine;

[CreateAssetMenu(menuName = "Famash/Effects/DealDamage")]
public class DealDamageEffect : EffectSOBase
{
    public override void Apply(CardRuntime card, EffectParams parameters)
    {
        Debug.Log($"Dealing {parameters.amount1} damage to hero of {card.owner.controllerName}");
        card.owner.hero.TakeDamage(parameters.amount1);
    }
}

