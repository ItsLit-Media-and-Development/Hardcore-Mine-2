using StardewValley.Monsters;

namespace HardcoreMines2
{
    internal class hardcore_monster
    {
        public bool attributes_initialized;
        public Monster _monster;

        public hardcore_monster(Monster monster)
        {
            this._monster = monster;
            this.attributes_initialized = false;
        }
    }
}
