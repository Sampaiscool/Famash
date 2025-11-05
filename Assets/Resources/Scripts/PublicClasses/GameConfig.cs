using System;

[Serializable]
public class GameConfig
{
    public string deckId;     // chosen deck
    public int seed;          // for RNG consistency

    // Cached during setup
    public string mainRegionId;
    public string secondaryRegionId;
}
