using System;
using System.Collections.Generic;

[Serializable]
public class DeckData
{
    public string deckId;
    public string deckName;
    public List<string> heroIds;  // Now stores multiple heroes
    public List<string> cardIds;

    public DeckData()
    {
        heroIds = new List<string>();
        cardIds = new List<string>();
    }
}
