namespace HardcoreMines2
{
    internal struct treasure_item
    {
        public int id;
        public int count;
        public string type;

        public treasure_item(int id, int count, string type = "")
        {
            this.id = id;
            this.count = count;
            this.type = type;
        }
    }
}
