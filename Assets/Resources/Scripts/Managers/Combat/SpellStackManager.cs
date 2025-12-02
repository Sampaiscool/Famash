using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpellStackManager : MonoBehaviour
{
    public static SpellStackManager Instance;

    private void Awake() => Instance = this;

    private Stack<StackEntry> stack = new();
    public bool HasPendingStack => stack.Count > 0;
    public bool stackIsResolving = false;

    public void AddToStack(CardRuntime source, RuntimeEffect effect)
    {
        stack.Push(new StackEntry(source, effect));
        BattleUIManager.Instance.UpdateStackUI(stack);

        // If instant, resolve immediately
        if (effect.spellSpeed == SpellSpeed.Instant)
        {
            ResolveStack();
            return;
        }

        // Grant priority to the opposing player to respond
        BaseController opponent = (source.owner == BattleManager.Instance.turnPlayer)
            ? BattleManager.Instance.otherPlayer
            : BattleManager.Instance.turnPlayer;

        BattleManager.Instance.GivePriorityTo(opponent);
    }

    public void ResolveStack()
    {
        if (stackIsResolving || stack.Count == 0) return;

        stackIsResolving = true;
        StartCoroutine(ResolveRoutine());
    }

    private IEnumerator ResolveRoutine()
    {
        while (stack.Count > 0)
        {
            var entry = stack.Pop();
            BattleUIManager.Instance.UpdateStackUI(stack);

            entry.Resolve();
            yield return new WaitForSeconds(0.2f);
        }

        stackIsResolving = false;
        BattleUIManager.Instance.HideStackUI();

        // Return priority to turn player if stack is empty
        BattleManager.Instance.GivePriorityTo(BattleManager.Instance.turnPlayer);
    }
}

public class StackEntry
{
    public CardRuntime source;
    public RuntimeEffect effect;

    public StackEntry(CardRuntime s, RuntimeEffect e)
    {
        source = s;
        effect = e;
    }

    public void Resolve()
    {
        effect.effect.Apply(source, effect.parameters);

        if (effect.isOncePerTurn)
            effect.hasBeenUsedThisTurn = true;
    }
}
