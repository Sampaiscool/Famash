using UnityEngine;

[CreateAssetMenu(menuName = "Famash/Effects/DealDamage/ToTarget")]
public class DealDamageToTargetEffect : EffectSOBase
{
    public override void Apply(CardRuntime source, EffectParams parameters)
    {
        // Ensure a target was selected
        if (parameters.target == null)
        {
            Debug.LogWarning("DealDamageToTargetEffect: No target selected.");
            return;
        }

        // Apply damage
        Debug.Log($"Dealing {parameters.amount1} damage to {parameters.target.cardData.cardName}");

        parameters.target.TakeDamage(parameters.amount1);
    }
}
