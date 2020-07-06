using Microsoft.Xna.Framework;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Monsters;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HardcoreMines2
{
    internal class ModEntry : Mod
    {
        private int mineLevel = 0;
        private Dictionary<int, List<treasure_item>> boss_treasures_inventory = new Dictionary<int, List<treasure_item>>();
        private Dictionary<int, int> boss_treasures_state = new Dictionary<int, int>();
        private Dictionary<Monster, Action> boss_hp_events = new Dictionary<Monster, Action>();
        private List<ModEntry.BossSpawn> monsters_to_spawn = new List<ModEntry.BossSpawn>();

        public override void Entry(IModHelper helper)
        {
            Helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            Helper.Events.GameLoop.Saved += OnSave;
            Helper.Events.Player.InventoryChanged += OnInventoryChange;
            Helper.Events.GameLoop.UpdateTicked += OnUpdate;
            Helper.Events.Player.Warped += OnWarp;
        }

        private void OnWarp(object sender, WarpedEventArgs e)
        {
            if (e.NewLocation is MineShaft mineShaft)
            {
                this.Monitor.Log($"{e.NewLocation.Name}, level: {mineShaft.mineLevel}", LogLevel.Debug);

                if(mineShaft.mineLevel == 10)
                {
                    CreateLevel10();
                }
            }
        }

        private void CreateLevel10()
        {
            int height = Game1.mine.Map.DisplayHeight;
            int width = Game1.mine.Map.DisplayWidth;
            bool flag = false;

            for(int tileX = 1; tileX < width; ++tileX)
            {
                for(int tileY = 1; tileY < height; ++tileY)
                {
                    if(Game1.mine.isTileClearForMineObjects(new Vector2((float) tileX, (float) tileY)))
                    {
                        BigSlime bigSlime = new BigSlime(new Vector2((float)tileX, (float)tileY), 0);
                        bigSlime.Health = 1;
                        bigSlime.speed = 6;
                        bigSlime.ExperienceGained = 40;
                        bigSlime.DamageToFarmer = 12;
                        bigSlime.isGlowing = true;
                        bigSlime.glowingTransparency = 0.0f;
                        //bigSlime.jitteriness = (NetDouble)100.0;
                        //bigSlime.c = (NetColor)new Color(0.0f, 50f, 0.0f);

                        Game1.mine.tryToAddMonster((Monster)bigSlime, tileX, tileY);
                        boss_hp_events.Add((Monster)bigSlime, new Action(BossLevel10Die));
                        flag = true;

                        monsters_to_spawn.Add(new BossSpawn((Monster)new GreenSlime(), 2, 300));
                        monsters_to_spawn.Add(new BossSpawn((Monster)new GreenSlime(), 4, 200));
                        monsters_to_spawn.Add(new BossSpawn((Monster)new GreenSlime(), 8, 100));
                        monsters_to_spawn.Add(new BossSpawn((Monster)new GreenSlime(), 16, 20));
                    }

                    if(flag)
                    {
                        break;
                    }
                }

                if(flag)
                {
                    break;
                }
            }

            var objects = Game1.currentLocation.Objects;

            if(objects.Count() > 0)
            {
                SerializableDictionary<Vector2, StardewValley.Object> keyValuePair = objects.FirstOrDefault();
                
                foreach(var i in keyValuePair)
                {
                    this.Monitor.Log($"Key: {i.Key}, Value: {i.Value}");                }
                //.mine.removeObject(keyValuePair.Values, false);
            }
        }

        private void BossLevel10Die()
        {
            if(boss_treasures_state[0] == 0)
            {
                Chest chest = new Chest(false, new Vector2(9f, 9f));

                foreach(treasure_item treasureItem in boss_treasures_inventory[0])
                {
                    if(treasureItem.id == 506)
                    {
                        chest.addItem((Item)new Boots(506));
                    } else
                    {
                        chest.addItem((Item)new StardewValley.Object(Vector2.Zero, treasureItem.id, treasureItem.count));
                    }
                }

                (Game1.mine.objects).Add(new Vector2(9f, 9f), (StardewValley.Object)chest);

                boss_treasures_state[0] = 1;
            }

            this.Monitor.Log("[Hardcore Mines] DEATH", LogLevel.Debug);
            Game1.playSound("powerup");

        }

        private void OnUpdate(object sender, UpdateTickedEventArgs e)
        {
            if (!Game1.hasLoadedGame || !Game1.inMine)
            {
                return;
            }

            List<Monster> monsterList = new List<Monster>();

            foreach (KeyValuePair<Monster, Action> bossHPEvent in boss_hp_events)
            {
                List<BossSpawn> spawnList = new List<BossSpawn>();

                foreach (BossSpawn spawn in monsters_to_spawn)
                {
                    if (bossHPEvent.Key.Health <= spawn.trigger_hp)
                    {
                        for (int index = 0; index < spawn.count; ++index)
                        {
                            bool flag = false;
                            int x = (int)bossHPEvent.Key.getTileLocation().X;
                            int y = (int)bossHPEvent.Key.getTileLocation().Y;

                            for(int tileX = x - 3; tileX < x + 3; ++tileX)
                            {
                                for (int tileY = y - 3; tileY < y + 3; ++tileY)
                                {
                                    if (Game1.mine.isTileClearForMineObjects(new Vector2((float)tileX, (float)tileY)))
                                    {
                                        this.Monitor.Log("[Hardcore Mines 2] Tile clear, trying to spawn.", LogLevel.Debug);

                                        Game1.mine.tryToAddMonster((Monster)new GreenSlime(new Vector2((float)tileX, (float)tileY), Color.Green), tileX, tileY);

                                        flag = true;

                                        break;
                                    }

                                    if(flag)
                                    {
                                        break;
                                    }
                                }
                                if(flag)
                                {
                                    break;
                                }
                            }
                        }
                    }

                    spawnList.Add(spawn);
                }

                foreach (BossSpawn spawn in spawnList)
                {
                    monsters_to_spawn.Remove(spawn);
                }

                if(bossHPEvent.Key.Health <= 0)
                {
                    bossHPEvent.Value();

                    monsterList.Add(bossHPEvent.Key);
                }
            }

            for(int index = 0; index < monsterList.Count; ++index)
            {
                boss_hp_events.Remove(monsterList[index]);
            }
        }

        private void OnInventoryChange(object sender, InventoryChangedEventArgs e)
        {
            if (!(Game1.currentLocation.Name == "UndergroundMine"))
            {
                return;
            }

            mineLevel = Game1.mine.mineLevel;

            if (Game1.mine.Objects.Count() > 0 && mineLevel == 10 && boss_treasures_state[0] == 1)
            {
                boss_treasures_inventory.Remove(0);

                List<treasure_item> treasureItemList = new List<treasure_item>();

                foreach (Item obj in Game1.mine.Objects.FirstOrDefault().Values)
                {
                    treasureItemList.Add(new treasure_item(obj.ParentSheetIndex, obj.Stack));
                }

                boss_treasures_inventory.Add(0, treasureItemList);
            }
        }

        private void OnSave(object sender, SavedEventArgs e)
        {
            string path = Constants.CurrentSavePath + "\\HardcoreMines2\\";
            Directory.CreateDirectory(path);
            StreamWriter streamWriter = new StreamWriter(path + "final");

            for (int index = 0; index < 12; ++index)
            {
                string str = index.ToString() + " " + boss_treasures_state[index].ToString();

                foreach (treasure_item treasureItem in boss_treasures_inventory[index])
                {
                    if (treasureItem.id != -1)
                    {
                        str = str + " " + (object)treasureItem.id + "," + (object)treasureItem.count;
                    }

                    streamWriter.WriteLine(str);
                }
            }

            streamWriter.Close();
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            boss_treasures_inventory.Clear();
            boss_treasures_state.Clear();

            string path = Constants.CurrentSavePath + "\\HardcoreMines2";

            Directory.CreateDirectory(path);

            if (File.Exists(path + "final"))
            {
                StreamReader streamReader = new StreamReader(path + "final");
                string str1;

                while ((str1 = streamReader.ReadLine()) != null)
                {
                    int length1 = str1.IndexOf(' ');
                    int key = int.Parse(str1.Substring(0, length1));
                    int startIndex1 = length1 + 1;
                    string str2 = str1.Substring(startIndex1, str1.Length - startIndex1);
                    int length2 = str2.IndexOf(' ');
                    int num = int.Parse(str2.Substring(0, length2));
                    int startIndex2 = length2 + 1;
                    string str3 = str2.Substring(startIndex2, str2.Length - startIndex2);
                    boss_treasures_state.Add(key, num);
                    List<string> stringList = new List<string>((IEnumerable<string>)str3.Split(' '));
                    List<treasure_item> treasureItemList = new List<treasure_item>();

                    foreach (string str4 in stringList)
                    {
                        char[] chArray = new char[1] { ',' };
                        string[] strArray = str4.Split(chArray);
                        treasureItemList.Add(new treasure_item(int.Parse(strArray[0]), int.Parse(strArray[1])));
                    }

                    boss_treasures_inventory.Add(key, treasureItemList);
                }

                streamReader.Close();
            }

            for (int index = 0; index < 12; ++index)
            {
                if (!boss_treasures_state.ContainsKey(index))
                {
                    InitialiseTreasure(index);
                }
            }
        }

        private void InitialiseTreasure(int id)
        {
            boss_treasures_state.Add(id, 0);
            List<treasure_item> treasureItemList = new List<treasure_item>();

            if (id == 0)
            {
                treasureItemList.Add(new treasure_item(334, 5));
                treasureItemList.Add(new treasure_item(459, 1));
                treasureItemList.Add(new treasure_item(506, 1));
            }
            else
            {
                treasureItemList.Add(new treasure_item(350, 3));
            }

            boss_treasures_inventory.Add(id, treasureItemList);
        }

        private struct BossSpawn
        {
            public Monster m;
            public int count;
            public int trigger_hp;

            public BossSpawn(Monster p_m, int p_count, int p_trigger_hp)
            {
                this.m = p_m;
                this.count = p_count;
                this.trigger_hp = p_trigger_hp;
            }
        }
    }
}