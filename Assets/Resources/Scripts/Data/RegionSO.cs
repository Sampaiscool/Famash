using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Famash/Region")]
public class RegionSO : ScriptableObject
{
    public string regionId;
    public string regionName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Gameplay Perks (Optional)")]
    public bool hasPassiveEffect;
    public string passiveDescription;

    [Header("Associated Cards")]
    public List<CardSO> regionCards = new();
}
