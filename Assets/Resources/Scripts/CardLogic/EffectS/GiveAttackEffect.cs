using UnityEngine;

[CreateAssetMenu(menuName = "Famash/Effects/GiveAttack")]
public class GiveAttackEffect : EffectSOBase
{
    public override void Apply(CardRuntime card, EffectParams parameters)
    {
        card.ModifyStats(parameters.amount1, 0);
        Debug.Log($"{card.cardData.cardName} gains {parameters.amount1} attack!");
    }
}

