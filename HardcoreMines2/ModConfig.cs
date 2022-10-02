namespace HardcoreMines2
{
    internal class ModConfig
    {
        public double difficulty { get; set; } = 3;
        public double difficultyPct { get; set; } = -1; //-1 disables percentage and focuses on the 5 difficulty levels. Everything below 100% is a reduction, over 100% increases
        public bool extraXP { get; set; } = false;
        public bool skullCave { get; set; } = false;
        public bool volcanoDungeon { get; set; } = false;
    }
}
