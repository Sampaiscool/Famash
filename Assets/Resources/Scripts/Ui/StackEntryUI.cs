using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StackEntryUI : MonoBehaviour
{
    public Image icon;

    private StackEntry boundEntry;

    public void Bind(StackEntry entry)
    {
        boundEntry = entry;

        if (icon != null)
            icon.sprite = entry.source.cardData.artwork;
    }
}
