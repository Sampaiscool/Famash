[System.Serializable]
public class EffectParams
{
    public int amount1;
    public int amount2;
    public int amount3;
    public int cost;

    // This becomes assigned during targeting
    [System.NonSerialized]
    public CardRuntime target;
}
