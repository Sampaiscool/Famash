using System.Collections.Generic;

[System.Serializable]
public class DeckData
{
    public string deckId;
    public string deckName;
    public string mainRegionId;
    public string secondaryRegionId;
    public List<string> cardIds = new();
}
