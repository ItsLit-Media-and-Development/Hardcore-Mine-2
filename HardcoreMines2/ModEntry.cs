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
        private ModConfig Config;
        private double difficulty;
        private Random rng = new Random();

        public override void Entry(IModHelper helper)
        {
            Config = helper.ReadConfig<ModConfig>();

            switch(Config.difficulty)
            {
                case 1:
                    difficulty = 0.5;

                    break;
                case 2:
                    difficulty = 0.7;

                    break;
                case 3:
                    difficulty = 1;

                    break;
                case 4:
                    difficulty = 1.5;

                    break;
                case 5:
                    difficulty = 2;

                    break;
                default:
                    difficulty = 1;
                    
                    break;
            }

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
                mineLevel = mineShaft.mineLevel / 10;

                if(isBossLevel(mineShaft.mineLevel))
                {
                    BossLevel();
                } else
                {
                    GeneralLevel();
                }
            }
        }

        private void BossLevel()
        {
            boss_treasures_state[0] = 0;
            int height = Game1.mine.Map.DisplayHeight;
            int width = Game1.mine.Map.DisplayWidth;
            bool flag = false;

            for (int tileX = 1; tileX < width; ++tileX)
            {
                for (int tileY = 1; tileY < height; ++tileY)
                {
                    if (Game1.mine.isTileClearForMineObjects(new Vector2((float)tileX, (float)tileY)))
                    {
                        flag = true;

                        //lets get the monster
                        switch (rng.Next(1, 8))
                        {
                            case 1:
                                BatBoss(tileX, tileY);
                                
                                break;
                            case 2:
                                SlimeBoss(tileX, tileY);

                                break;
                            case 3:
                                BugBoss(tileX, tileY);

                                break;
                            case 4:
                                DinoBoss(tileX, tileY);

                                break;
                            case 5:
                                DuggyBoss(tileX, tileY);

                                break;
                            case 6:
                                DustBoss(tileX, tileY);

                                break;
                            case 7:
                                GhostBoss(tileX, tileY);

                                break;
                            case 8:
                                MetalBoss(tileX, tileY);

                                break;
                            default:
                                break;
                        }
                    }

                    if (flag)
                    {
                        break;
                    }
                }

                if (flag)
                {
                    break;
                }
            }

            foreach (var pair in Game1.currentLocation.Objects.Pairs)
            {
                Vector2 tile = pair.Key;
                StardewValley.Object obj = pair.Value;

                this.Monitor.Log($"{obj.Name} at {tile}", LogLevel.Debug);

                if (obj.name == "Chest")
                {
                    Game1.currentLocation.removeObject(tile, false);
                    break;
                }
            }
        }

        private void MetalBoss(int x, int y)
        {
            MetalHead metal = new MetalHead(new Vector2(x, y), 0);
            metal.Health = (mineLevel == 1) ? 400 : ((mineLevel / 2) * 400);
            metal.speed = 6;
            metal.ExperienceGained = 40;
            metal.DamageToFarmer = (mineLevel == 1) ? 12 : ((mineLevel / 2) * 12);
            metal.isGlowing = true;
            metal.glowingTransparency = 0.0f;
            metal.jitteriness.Value = (Double)100.0;
            metal.c.Value = new NetColor(new Color(rng.Next(255), rng.Next(255), rng.Next(255)));

            Game1.mine.tryToAddMonster((Monster)metal, x, y);
            boss_hp_events.Add((Monster)metal, new Action(BossLevel10Die));
        }

        private void GhostBoss(int x, int y)
        {
            Ghost ghost = new Ghost(new Vector2(x, y));
            ghost.Health = (mineLevel == 1) ? 400 : ((mineLevel / 2) * 400);
            ghost.speed = 6;
            ghost.ExperienceGained = 40;
            ghost.DamageToFarmer = (mineLevel == 1) ? 12 : ((mineLevel / 2) * 12);
            ghost.isGlowing = true;
            ghost.glowingTransparency = 0.0f;
            ghost.jitteriness.Value = (Double)100.0;

            Game1.mine.tryToAddMonster((Monster)ghost, x, y);
            boss_hp_events.Add((Monster)ghost, new Action(BossLevel10Die));
        }

        private void DustBoss(int x, int y)
        {
            DustSpirit dust = new DustSpirit(new Vector2(x, y), true);
            dust.Health = (mineLevel == 1) ? 400 : ((mineLevel / 2) * 400);
            dust.speed = 6;
            dust.ExperienceGained = 40;
            dust.DamageToFarmer = (mineLevel == 1) ? 12 : ((mineLevel / 2) * 12);
            dust.isGlowing = true;
            dust.glowingTransparency = 0.0f;
            dust.jitteriness.Value = (Double)100.0;

            Game1.mine.tryToAddMonster((Monster)dust, x, y);
            boss_hp_events.Add((Monster)dust, new Action(BossLevel10Die));
        }

        private void DuggyBoss(int x, int y)
        {
            Duggy duggy = new Duggy(new Vector2(x, y));
            duggy.Health = (mineLevel == 1) ? 400 : ((mineLevel / 2) * 400);
            duggy.speed = 6;
            duggy.ExperienceGained = 40;
            duggy.DamageToFarmer = (mineLevel == 1) ? 12 : ((mineLevel / 2) * 12);
            duggy.isGlowing = true;
            duggy.glowingTransparency = 0.0f;
            duggy.jitteriness.Value = (Double)100.0;

            Game1.mine.tryToAddMonster((Monster)duggy, x, y);
            boss_hp_events.Add((Monster)duggy, new Action(BossLevel10Die));
        }

        private void DinoBoss(int x, int y)
        {
            DinoMonster dino = new DinoMonster(new Vector2(x, y));
            dino.Health = (mineLevel == 1) ? 400 : ((mineLevel / 2) * 400);
            dino.speed = 6;
            dino.ExperienceGained = 40;
            dino.DamageToFarmer = (mineLevel == 1) ? 12 : ((mineLevel / 2) * 12);
            dino.isGlowing = true;
            dino.glowingTransparency = 0.0f;
            dino.jitteriness.Value = (Double)100.0;

            Game1.mine.tryToAddMonster((Monster)dino, x, y);
            boss_hp_events.Add((Monster)dino, new Action(BossLevel10Die));
        }

        private void BugBoss(int x, int y)
        {
            Bug bug = new Bug(new Vector2(x, y), 0);
            bug.Health = (mineLevel == 1) ? 400 : ((mineLevel / 2) * 400);
            bug.speed = 6;
            bug.ExperienceGained = 40;
            bug.DamageToFarmer = (mineLevel == 1) ? 12 : ((mineLevel / 2) * 12);
            bug.isGlowing = true;
            bug.glowingTransparency = 0.0f;
            bug.jitteriness.Value = (Double)100.0;

            Game1.mine.tryToAddMonster((Monster)bug, x, y);
            boss_hp_events.Add((Monster)bug, new Action(BossLevel10Die));
        }

        private void BatBoss(int x, int y)
        {
            Bat bat = new Bat(new Vector2(x, y), 0);
            bat.Health = (mineLevel == 1) ? 400 : ((mineLevel / 2) * 400);
            bat.speed = 6;
            bat.ExperienceGained = 40;
            bat.DamageToFarmer = (mineLevel == 1) ? 12 : ((mineLevel / 2) * 12);
            bat.isGlowing = true;
            bat.glowingTransparency = 0.0f;
            bat.jitteriness.Value = (Double)100.0;

            Game1.mine.tryToAddMonster((Monster)bat, x, y);
            boss_hp_events.Add((Monster)bat, new Action(BossLevel10Die));
        }

        private void SlimeBoss(int x, int y)
        {
            BigSlime bigSlime = new BigSlime(new Vector2(x, y), 0);
            bigSlime.Health = (mineLevel == 1) ? 400 : ((mineLevel / 2) * 400);
            bigSlime.speed = 6;
            bigSlime.ExperienceGained = 40;
            bigSlime.DamageToFarmer = (mineLevel == 1) ? 12 : ((mineLevel / 2) * 12);
            bigSlime.isGlowing = true;
            bigSlime.glowingTransparency = 0.0f;
            bigSlime.jitteriness.Value = (Double)100.0;
            bigSlime.c.Value = new NetColor(new Color(rng.Next(255), rng.Next(255), rng.Next(255)));

            Game1.mine.tryToAddMonster((Monster)bigSlime, x, y);
            boss_hp_events.Add((Monster)bigSlime, new Action(BossLevel10Die));

            if (rng.Next(1, 10) > 5)
            {
                monsters_to_spawn.Add(new BossSpawn((Monster)new GreenSlime(), 2, 300));
                monsters_to_spawn.Add(new BossSpawn((Monster)new GreenSlime(), 4, 200));
                monsters_to_spawn.Add(new BossSpawn((Monster)new GreenSlime(), 8, 100));
                monsters_to_spawn.Add(new BossSpawn((Monster)new GreenSlime(), 16, 20));
            }
        }

        private void GeneralLevel()
        {

            var characters = Game1.currentLocation.getCharacters().OfType<Monster>().ToArray<Monster>();

            foreach(Monster mon in characters)
            {
                mon.MaxHealth = (int)Math.Max(1.0, mon.maxHealth * difficulty);
                mon.Health = mon.MaxHealth;
                
                if(mon.damageToFarmer > 0)
                {
                    mon.DamageToFarmer = (int)Math.Max(1.0, mon.damageToFarmer * difficulty);
                }

                if(Config.extraXP)
                {
                    mon.ExperienceGained = (int)Math.Max(1.0, mon.experienceGained * difficulty);
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
                        bigSlime.Health = 400;
                        bigSlime.speed = 6;
                        bigSlime.ExperienceGained = 40;
                        bigSlime.DamageToFarmer = 12;
                        bigSlime.isGlowing = true;
                        bigSlime.glowingTransparency = 0.0f;
                        bigSlime.jitteriness.Value = (Double)100.0;
                        bigSlime.c.Value = new NetColor(new Color(100,0,0));

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

            foreach (var pair in Game1.currentLocation.Objects.Pairs)
            {
                Vector2 tile = pair.Key;
                StardewValley.Object obj = pair.Value;

                //this.Monitor.Log($"{obj.Name} at {tile}", LogLevel.Debug);

                if(obj.name == "Chest")
                {
                    Game1.currentLocation.removeObject(tile, false);
                    break;
                }
            }
        }

        private void BossLevel10Die()
        {
            if(boss_treasures_state[0] == 0)
            {
                Chest chest = new Chest(false, new Vector2(9f, 9f));

                //this.Monitor.Log($"count: {boss_treasures_inventory.Count}",LogLevel.Debug);
                foreach(treasure_item treasureItem in boss_treasures_inventory[rng.Next(0, 10)])
                {
                    //this.Monitor.Log($"{treasureItem.id}", LogLevel.Debug);

                    if(treasureItem.id == 506)
                    {
                        chest.addItem((Item)new Boots(506));
                    } else
                    {
                        //chest.addItem((Item)new StardewValley.Object(Vector2.Zero, items[rng.Next(0, 10)], treasureItem.count));
                        chest.addItem((Item)new StardewValley.Object(Vector2.Zero, treasureItem.id, treasureItem.count));
                    }

                    break;
                }

                (Game1.mine.objects).Add(new Vector2(9f, 9f), chest);

                boss_treasures_state[0] = 1;
            }/* else
            {
                this.Monitor.Log($"{boss_treasures_state[0]}", LogLevel.Debug);
            }*/

            //this.Monitor.Log("[Hardcore Mines] DEATH", LogLevel.Debug);
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
                                        //this.Monitor.Log("[Hardcore Mines 2] Tile clear, trying to spawn.", LogLevel.Debug);

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
                treasureItemList.Add(new treasure_item(96, 1));
                treasureItemList.Add(new treasure_item(60, 2));
                treasureItemList.Add(new treasure_item(86, 1));
                treasureItemList.Add(new treasure_item(287, 5));
                treasureItemList.Add(new treasure_item(335, 5));
                treasureItemList.Add(new treasure_item(336, 5));
                treasureItemList.Add(new treasure_item(517, 1));
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

        public bool isBossLevel(int lvl)
        {
            return (lvl % 10) == 0;
        }
    }
}