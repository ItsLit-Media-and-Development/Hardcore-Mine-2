﻿using Microsoft.Xna.Framework;
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
using StardewValley.Tools;

namespace HardcoreMines2
{
    internal class ModEntry : Mod
    {
        private int mineLevel = 0;
        private readonly Dictionary<int, List<treasure_item>> _bossTreasuresInventory = new Dictionary<int, List<treasure_item>>();
        private readonly Dictionary<int, int> _bossTreasuresState = new Dictionary<int, int>();
        private readonly Dictionary<Monster, Action> _bossHpEvents = new Dictionary<Monster, Action>();
        private readonly List<ModEntry.BossSpawn> _monstersToSpawn = new List<ModEntry.BossSpawn>();
        private ModConfig Config;
        private double difficulty;
        private Random rng = new Random();

        public override void Entry(IModHelper helper)
        {
            Config = helper.ReadConfig<ModConfig>();
            if (Config.difficultyPct == -1)
            {
                switch (Config.difficulty)
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
                    case 6:
                        difficulty = 10;

                        break;
                    case 7:
                        difficulty = 30;

                        break;
                    case 8:
                        difficulty = 60;

                        break;
                    case 9:
                        difficulty = 100;

                        break;
                    case 10:
                        difficulty = 500;

                        break;
                    default:
                        difficulty = 1;

                        break;
                }
            } else
            {
                difficulty = Config.difficultyPct / 100;
            }

            Helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            Helper.Events.GameLoop.Saved += OnSave;
            Helper.Events.Player.InventoryChanged += OnInventoryChange;
            Helper.Events.GameLoop.UpdateTicked += OnUpdate;
            Helper.Events.Player.Warped += OnWarp;
        }

        private void OnWarp(object sender, WarpedEventArgs e)
        {
            //this.Monitor.Log("Current Season: " + Game1.currentSeason, LogLevel.Debug);
            //this.Monitor.Log("Current Year: " + Game1.year.ToString(), LogLevel.Debug);

            if (e.NewLocation is MineShaft mineShaft)
            {
                ////this.Monitor.Log($"Mineshaft {mineShaft.name} at level {mineShaft.mineLevel}", LogLevel.Debug);

                if (!SpecialLevel(mineShaft.mineLevel))
                {
                    if ((mineShaft.mineLevel > 120 && Config.skullCave) || mineShaft.mineLevel <= 120)
                    {
                        mineLevel = mineShaft.mineLevel / 10;

                        if (isBossLevel(mineShaft.mineLevel))
                        {
                           BossLevel();
                        }
                        else
                        {
                            GeneralLevel();
                        }
                    }
                }
            }
            else
            {
                if (e.NewLocation is VolcanoDungeon volcano && Config.volcanoDungeon) 
                {
                    ////this.Monitor.Log($"Volcano Level {volcano.level}", LogLevel.Debug);
                    GeneralLevel();
                }
            }
        }

        private bool SpecialLevel(int level)
        {
            switch (level)
            {
                case 90:
                case 77377:
                case 120:
                    return true;

                default:
                    return false;
            }
        }

        private void BossLevel()
        {
            _bossTreasuresState[0] = 0;
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
                                BatBoss(tileX+4, tileY);
                                
                                break;
                            case 2:
                                SlimeBoss(tileX, tileY);

                                break;
                            case 3:
                                BugBoss(tileX+4, tileY);

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

                //////this.Monitor.Log($"{obj.Name} at {tile}", LogLevel.Debug);

                if (obj.name == "Chest")
                {
                    Game1.currentLocation.removeObject(tile, false);
                    break;
                }
            }
        }

        private void MetalBoss(int x, int y)
        {
            MetalHead metal = new MetalHead(new Vector2(x, y), 0)
            {
                Health = (mineLevel == 1) ? 400 : ((mineLevel / 2) * 400),
                speed = 6,
                ExperienceGained = 40,
                DamageToFarmer = (Config.difficulty > 5) ? (int)(mineLevel / 2 * difficulty) : ((mineLevel == 1) ? 12 : ((mineLevel / 2) * 12)),
                isGlowing = true,
                glowingTransparency = 0.0f
            };
            metal.jitteriness.Value = (Double)100.0;
            metal.c.Value = new NetColor(new Color(rng.Next(255), rng.Next(255), rng.Next(255)));

            Game1.mine.tryToAddMonster((Monster)metal, x, y);
            _bossHpEvents.Add((Monster)metal, new Action(BossLevel10Die));
        }


        private void GhostBoss(int x, int y)
        {
            Ghost ghost = new Ghost(new Vector2(x, y))
            {
                Health = (mineLevel == 1) ? 400 : ((mineLevel / 2) * 400),
                speed = 6,
                ExperienceGained = 40,
                DamageToFarmer = (Config.difficulty > 5) ? (int)(mineLevel / 2 * difficulty) : ((mineLevel == 1) ? 12 : ((mineLevel / 2) * 12)),
                isGlowing = true,
                glowingTransparency = 0.0f
            };
            ghost.jitteriness.Value = (Double)100.0;

            Game1.mine.tryToAddMonster((Monster)ghost, x, y);
            _bossHpEvents.Add((Monster)ghost, new Action(BossLevel10Die));
        }

        private void DustBoss(int x, int y)
        {
            DustSpirit dust = new DustSpirit(new Vector2(x, y), true)
            {
                Health = (mineLevel == 1) ? 400 : ((mineLevel / 2) * 400),
                speed = 6,
                ExperienceGained = 40,
                DamageToFarmer = (Config.difficulty > 5) ? (int)(mineLevel / 2 * difficulty) : ((mineLevel == 1) ? 12 : ((mineLevel / 2) * 12)),
                isGlowing = true,
                glowingTransparency = 0.0f
            };
            dust.jitteriness.Value = (Double)100.0;

            Game1.mine.tryToAddMonster((Monster)dust, x, y);
            _bossHpEvents.Add((Monster)dust, new Action(BossLevel10Die));
        }

        private void DuggyBoss(int x, int y)
        {
            Duggy duggy = new Duggy(new Vector2(x, y))
            {
                Health = (mineLevel == 1) ? 400 : ((mineLevel / 2) * 400),
                speed = 6,
                ExperienceGained = 40,
                DamageToFarmer = (Config.difficulty > 5) ? (int)(mineLevel / 2 * difficulty) : ((mineLevel == 1) ? 12 : ((mineLevel / 2) * 12)),
                isGlowing = true,
                glowingTransparency = 0.0f
            };
            duggy.jitteriness.Value = (Double)100.0;

            Game1.mine.tryToAddMonster((Monster)duggy, x, y);
            _bossHpEvents.Add((Monster)duggy, new Action(BossLevel10Die));
        }

        private void DinoBoss(int x, int y)
        {
            DinoMonster dino = new DinoMonster(new Vector2(x, y))
            {
                Health = (mineLevel == 1) ? 400 : ((mineLevel / 2) * 400),
                speed = 6,
                ExperienceGained = 40,
                DamageToFarmer = (Config.difficulty > 5) ? (int)(mineLevel / 2 * difficulty) : ((mineLevel == 1) ? 12 : ((mineLevel / 2) * 12)),
                isGlowing = true,
                glowingTransparency = 0.0f
            };
            dino.jitteriness.Value = (Double)100.0;

            Game1.mine.tryToAddMonster((Monster)dino, x, y);
            _bossHpEvents.Add((Monster)dino, new Action(BossLevel10Die));
        }

        private void BugBoss(int x, int y)
        {
            Bug bug = new Bug(new Vector2(x, y), 0)
            {
                Health = (mineLevel == 1) ? 400 : ((mineLevel / 2) * 400),
                speed = 6,
                ExperienceGained = 40,
                DamageToFarmer = (Config.difficulty > 5) ? (int)(mineLevel / 2 * difficulty) : ((mineLevel == 1) ? 12 : ((mineLevel / 2) * 12)),
                isGlowing = true,
                glowingTransparency = 0.0f
            };
            bug.jitteriness.Value = (Double)100.0;

            Game1.mine.tryToAddMonster((Monster)bug, x, y);
            _bossHpEvents.Add((Monster)bug, new Action(BossLevel10Die));
        }

        private void BatBoss(int x, int y)
        {
            Bat bat = new Bat(new Vector2(x, y), 0)
            {
                Health = (mineLevel == 1) ? 400 : ((mineLevel / 2) * 400),
                speed = 6,
                ExperienceGained = 40,
                DamageToFarmer = (Config.difficulty > 5) ? (int)(mineLevel / 2 * difficulty) : ((mineLevel == 1) ? 12 : ((mineLevel / 2) * 12)),
                isGlowing = true,
                glowingTransparency = 0.0f
            };
            bat.jitteriness.Value = (Double)100.0;

            Game1.mine.tryToAddMonster((Monster)bat, x, y);
            _bossHpEvents.Add((Monster)bat, new Action(BossLevel10Die));
        }

        private void SlimeBoss(int x, int y)
        {
            BigSlime bigSlime = new BigSlime(new Vector2(x, y), 0)
            {
                Health = (mineLevel == 1) ? 400 : ((mineLevel / 2) * 400),
                speed = 6,
                ExperienceGained = 40,
                DamageToFarmer = (Config.difficulty > 5) ? (int)(mineLevel / 2 * difficulty) : ((mineLevel == 1) ? 12 : ((mineLevel / 2) * 12)),
                isGlowing = true,
                glowingTransparency = 0.0f
            };
            bigSlime.jitteriness.Value = (Double)100.0;
            bigSlime.c.Value = new NetColor(new Color(rng.Next(255), rng.Next(255), rng.Next(255)));

            Game1.mine.tryToAddMonster((Monster)bigSlime, x, y);
            _bossHpEvents.Add((Monster)bigSlime, new Action(BossLevel10Die));

            if (rng.Next(1, 10) > 5)
            {
                _monstersToSpawn.Add(new BossSpawn((Monster)new GreenSlime(), 2, 300));
                _monstersToSpawn.Add(new BossSpawn((Monster)new GreenSlime(), 4, 200));
                _monstersToSpawn.Add(new BossSpawn((Monster)new GreenSlime(), 8, 100));
                _monstersToSpawn.Add(new BossSpawn((Monster)new GreenSlime(), 16, 20));
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
                        _bossHpEvents.Add((Monster)bigSlime, new Action(BossLevel10Die));
                        flag = true;

                        _monstersToSpawn.Add(new BossSpawn((Monster)new GreenSlime(), 2, 300));
                        _monstersToSpawn.Add(new BossSpawn((Monster)new GreenSlime(), 4, 200));
                        _monstersToSpawn.Add(new BossSpawn((Monster)new GreenSlime(), 8, 100));
                        _monstersToSpawn.Add(new BossSpawn((Monster)new GreenSlime(), 16, 20));
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

                ////////this.Monitor.Log($"{obj.Name} at {tile}", LogLevel.Debug);

                if(obj.name == "Chest")
                {
                    Game1.currentLocation.removeObject(tile, false);
                    break;
                }
            }
        }

        private void BossLevel10Die()
        {
            List<Item> tmp = new List<Item>();

            if(_bossTreasuresState[0] == 0)
            {
                if (mineLevel != 10)
                {
                    if (mineLevel != 12)
                    {
                        //this.Monitor.Log($"_bossTreasuresInventory count: {_bossTreasuresInventory.Count}",LogLevel.Debug);
                        for (var c = 0; c < Config.difficulty - 2; c++)
                        {
                            foreach (treasure_item treasureItem in _bossTreasuresInventory[rng.Next(0, _bossTreasuresInventory.Count)])
                            {
                                if(treasureItem.type == "MeleeWeapon")
                                {
                                    tmp.Add((Item)new MeleeWeapon(treasureItem.id));
                                }
                                if(treasureItem.type == "Boots")
                                {
                                    tmp.Add((Item)new Boots(treasureItem.id));
                                }
                                if (treasureItem.id == 506)
                                {
                                    tmp.Add((Item)new Boots(506));
                                }
                                else if (treasureItem.id == 517)
                                {
                                    tmp.Add((Item)new Ring(517));
                                }
                                else if (treasureItem.id == 61)
                                {
                                    tmp.Add((Item)new Hat(61));
                                }
                                else if (treasureItem.id == 508)
                                {
                                    tmp.Add((Item)new Boots(508));
                                }
                                else if (treasureItem.id == 1138)
                                {
                                    tmp.Add((Item)new Clothing(1138));
                                }
                                else
                                {
                                    //chest.addItem((Item)new StardewValley.Object(Vector2.Zero, items[rng.Next(0, 10)], treasureItem.count));
                                    tmp.Add((Item)new StardewValley.Object(Vector2.Zero, treasureItem.id, treasureItem.count));
                                }

                                break;
                            }
                        }
                    }
                } 
                else
                {
                    //Stardrop for level 100
                    tmp.Add((Item)new StardewValley.Object(434, 1));
                }

                Chest chest = MakeChest(tmp, new Vector2(9f, 9f));

                (Game1.mine.objects).Add(new Vector2(9f, 9f), chest);

                _bossTreasuresState[0] = 1;
            } else
            {
                //this.Monitor.Log($"boss treasure state {_bossTreasuresState[0]}", LogLevel.Debug);
            }

            ////////this.Monitor.Log("[Hardcore Mines] DEATH", LogLevel.Debug);
            Game1.playSound("powerup");

        }

        private void OnUpdate(object sender, UpdateTickedEventArgs e)
        {
            if (!Game1.hasLoadedGame || !Game1.inMine)
            {
                return;
            }

            List<Monster> monsterList = new List<Monster>();

            foreach (KeyValuePair<Monster, Action> bossHPEvent in _bossHpEvents)
            {
                List<BossSpawn> spawnList = new List<BossSpawn>();

                foreach (BossSpawn spawn in _monstersToSpawn)
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
                                        ////////this.Monitor.Log("[Hardcore Mines 2] Tile clear, trying to spawn.", LogLevel.Debug);

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
                    _monstersToSpawn.Remove(spawn);
                }

                if(bossHPEvent.Key.Health <= 0)
                {
                    bossHPEvent.Value();

                    monsterList.Add(bossHPEvent.Key);
                }
            }

            for(int index = 0; index < monsterList.Count; ++index)
            {
                _bossHpEvents.Remove(monsterList[index]);
            }

            
        }

        private void OnInventoryChange(object sender, InventoryChangedEventArgs e)
        {
            if (Game1.currentLocation.Name != "UndergroundMine")
            {
                return;
            }

            mineLevel = Game1.mine.mineLevel;

            if (Game1.mine.Objects.Any() && mineLevel == 10 && _bossTreasuresState[0] == 1)
            {
                _bossTreasuresInventory.Remove(0);

                List<treasure_item> treasureItemList = new List<treasure_item>();

                foreach (Item obj in Game1.mine.Objects.FirstOrDefault().Values)
                {
                    treasureItemList.Add(new treasure_item(obj.ParentSheetIndex, obj.Stack));
                }

                _bossTreasuresInventory.Add(0, treasureItemList);
            }
        }

        private void OnSave(object sender, SavedEventArgs e)
        {
            string path = Constants.CurrentSavePath + "\\HardcoreMines2\\";
            Directory.CreateDirectory(path);
            StreamWriter streamWriter = new StreamWriter(path + "final");

            for (int index = 0; index < 12; ++index)
            {
                string str = index.ToString() + " " + _bossTreasuresState[index].ToString();

                foreach (treasure_item treasureItem in _bossTreasuresInventory[index])
                {
                    if (treasureItem.id != -1)
                    {
                        str = str + " " + (object)treasureItem.id + "," + (object)treasureItem.count;
                    }
                    //this.Monitor.Log(str, LogLevel.Debug);
                    streamWriter.WriteLine(str);
                }
            }

            streamWriter.Close();
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            _bossTreasuresInventory.Clear();
            _bossTreasuresState.Clear();

            string path = Constants.CurrentSavePath + "\\HardcoreMines2";
            //this.Monitor.Log(path, LogLevel.Debug);
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
                    _bossTreasuresState.Add(key, num);
                    List<string> stringList = new List<string>((IEnumerable<string>)str3.Split(' '));
                    List<treasure_item> treasureItemList = new List<treasure_item>();

                    foreach (string str4 in stringList)
                    {
                        char[] chArray = new char[1] { ',' };
                        string[] strArray = str4.Split(chArray);
                        treasureItemList.Add(new treasure_item(int.Parse(strArray[0]), int.Parse(strArray[1])));
                    }

                    _bossTreasuresInventory.Add(key, treasureItemList);
                }

                streamReader.Close();
            }

            for (int index = 0; index < 12; ++index)
            {
                if (!_bossTreasuresState.ContainsKey(index))
                {
                    InitialiseTreasure(index, Config.difficulty - 2);
                }
            }
        }

        private void InitialiseTreasure(int id, double amount)
        {
            _bossTreasuresState.Add(id, 0);
            List<treasure_item> treasureItemList = new List<treasure_item>();

            int[] basic = new int[] { 16, 18, 20, 22, 24, 30, 78, 88, 90, 92, 166, 174, 176, 178, 180, 182, 184, 186, 188, 190, 192, 245, 246, 247, 248, 250, 251, 252, 254, 256, 257, 258, 259, 260, 262, 264, 266, 268, 270, 271, 272, 274, 275, 276, 277, 278, 280, 281, 282, 283, 284, 296, 300, 303, 304, 305, 306, 307, 308, 330, 334, 335, 336, 337, 338, 340, 341, 342, 344, 346, 348, 350, 368, 369, 370, 371, 372, 373, 376, 378, 380, 382, 384, 386, 388, 392, 393, 394, 396, 397, 398, 399, 400, 402, 404, 406, 408, 410, 412, 413, 414, 416, 417, 418, 419, 420, 421, 422, 423, 424, 426, 428, 430, 432, 436, 437, 438, 439, 440, 441, 442, 444, 445, 446, 447, 454, 458, 459, 460, 465, 466, 535, 536, 537, 591, 593, 595, 597, 613, 614, 628, 629, 630, 631, 632, 633, 634, 635, 636, 637, 638, 680, 684, 685, 686, 687, 691, 692, 693, 694, 695, 703, 709, 724, 725, 726, 747, 748, 749, 766, 767, 768, 769, 771, 774, 787, 797, 803, 805, 807, 808, 809, 812, 814, 815 };
            //this.Monitor.Log(basic.Length.ToString(), LogLevel.Debug);
            for(var b = 0; b < Config.difficulty - 1; b++)
            {
                treasureItemList.Add(new treasure_item(basic[rng.Next(0, basic.Length)], rng.Next(0, 4), "Object"));
            }

            foreach(KeyValuePair<int, string> kvp in Game1.content.Load<Dictionary<int, string>>("Data\\weapons"))
            {
                if(kvp.Key == 32 || kvp.Key == 34)
                {
                    continue;
                }

                treasureItemList.Add(new treasure_item(kvp.Key, 1, "MeleeWeapon"));
            }

            foreach (KeyValuePair<int, string> kvp in Game1.content.Load<Dictionary<int, string>>("Data\\Boots"))
            {
                treasureItemList.Add(new treasure_item(kvp.Key, 1, "Boots"));
            }
            //switch (rng.Next(0, 19))
            //{
            //    case 0:
            //        treasureItemList.Add(new treasure_item(334, 5));

            //        break;
            //    case 1:
            //        treasureItemList.Add(new treasure_item(459, 1));

            //        break;
            //    case 2:
            //        treasureItemList.Add(new treasure_item(506, 1));

            //        break;
            //    case 3:
            //        treasureItemList.Add(new treasure_item(96, 1));

            //        break;
            //    case 4:
            //        treasureItemList.Add(new treasure_item(60, 2));

            //        break;
            //    case 5:
            //        treasureItemList.Add(new treasure_item(86, 1));

            //        break;
            //    case 6:
            //        treasureItemList.Add(new treasure_item(287, 5));

            //        break;
            //    case 7:
            //        treasureItemList.Add(new treasure_item(335, 5));

            //        break;
            //    case 8:
            //        treasureItemList.Add(new treasure_item(336, 5));

            //        break;
            //    case 9:
            //        treasureItemList.Add(new treasure_item(517, 1));

            //        break;
            //    case 10:
            //        treasureItemList.Add(new treasure_item(350, 3));

            //        break;
            //    case 11:
            //        treasureItemList.Add(new treasure_item(436, 2));

            //        break;
            //    case 12:
            //        treasureItemList.Add(new treasure_item(16, 1));

            //        break;
            //    case 13:
            //        treasureItemList.Add(new treasure_item(348, 1));

            //        break;
            //    case 14:
            //        treasureItemList.Add(new treasure_item(745, 3));

            //        break;
            //    case 15:
            //        treasureItemList.Add(new treasure_item(130, 2));

            //        break;
            //    case 16:
            //        treasureItemList.Add(new treasure_item(130, 2));

            //        break;
            //    case 17:
            //        treasureItemList.Add(new treasure_item(61, 1));

            //        break;
            //    case 18:
            //        treasureItemList.Add(new treasure_item(508, 1));

            //        break;
            //    case 19:
            //        treasureItemList.Add(new treasure_item(1138, 1));
            //        treasureItemList.Add(new treasure_item(489, 1));

            //        break;
            //}
            //this.Monitor.Log("Number of items: " + treasureItemList.Count.ToString(), LogLevel.Debug);
            _bossTreasuresInventory.Add(id, treasureItemList);
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

        public static Chest MakeChest(List<Item> chestItems, Vector2 chestSpot)
        {
            Chest chest = new Chest(true);
            
            chest.items.Clear();
            chest.items.AddRange(chestItems);
            chest.tileLocation.Value = chestSpot;

            return chest;
        }
    }
}