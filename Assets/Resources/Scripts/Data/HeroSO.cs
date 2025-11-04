using UnityEngine;

[CreateAssetMenu(menuName = "Famash/Hero")]
public class HeroSO : ScriptableObject
{
    public string heroId;
    public string heroName;
    [TextArea] public string description;

    [Header("Visuals")]
    public Sprite portrait;
    public Color themeColor;

    [Header("Gameplay")]
    public int baseHealth = 20;
    public int resourceStart = 1;
    public int resourceMax = 10;

    [Header("Hero Deck")]
    //public CardSO[] defaultCards;      // starting deck
    public CardSO heroCard;            // optional signature card

    [Header("General Cards")]
    public CardSO[] generalCards;      // (optional helper list; not strictly required)
}
