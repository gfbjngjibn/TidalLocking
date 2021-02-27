using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using BepInEx;
using UnityEngine;

namespace TidalLocking
{
    [BepInPlugin("CuSO4.DSPMOD.TidalLocking", "TidalLocking", "1.1")]
    public class Locking : BaseUnityPlugin
    {
        /// <summary>
        /// 公转周期
        /// </summary>
        private double orbitalPeriod;

        /// <summary>
        /// 自传周期
        /// </summary>
        private double rotationPeriod = 0.0;

        /// <summary>
        /// 公转相位
        /// </summary>
        private float orbitPhase = 0.0f;

        /// <summary>
        /// 玩家所在的星球
        /// </summary>
        private static PlanetData playerOn = null;

        private static int playerOnIndex = 0;

        private static bool unsave = false;

        private static List<PlanetData> planets = new List<PlanetData>();

        private static List<PlanetConfig> configs = new List<PlanetConfig>();

        private static int seed = 0;

        private Thread th = new Thread(() =>
        {
            int old = times;
            while (true)
            {
                if (times == old)
                {
                    times = 0;
                    old = 0;
                }

                Thread.Sleep(3000);
            }
        });

        private Thread saveConfig = new Thread(() =>
        {
            while (true)
            {
                Thread.Sleep(3000);
                if (!unsave) continue;
                while (count > 0)
                {
                    count--;
                    Thread.Sleep(1000);
                }
                
                Console.WriteLine("saveConfig");
                var p = new PlanetConfig(playerOn.id, playerOn.seed, playerOn.orbitPhase);
                if (configs.Count > 0 && configs[playerOnIndex].Equals(p))
                {
                    Console.WriteLine("cmp");
                    var t = configs[playerOnIndex];
                    t.orbitPhase = playerOn.orbitPhase;
                    configs[playerOnIndex] = t;
                }
                else
                {
                    Console.WriteLine("else");
                    var e = false;
                    for (int i = 0; i < configs.Count; i++)
                    {
                        if (configs[i].Equals(p))
                        {
                            playerOnIndex = i;
                            var t = configs[playerOnIndex];
                            t.orbitPhase = playerOn.orbitPhase;
                            configs[playerOnIndex] = t;
                            e = true;
                            break;
                        }
                    }

                    if (!e)
                    {
                        Console.WriteLine("not e");
                        configs.Add(p);
                    }
                }

                Console.WriteLine("write");
                using (var bw = new BinaryWriter(new FileInfo(conifgPath).Create()))
                {
                    bw.Write('c');
                    bw.Write(GameMain.galaxy.seed);
                    bw.Write(configs.Count);
                    foreach (var config in configs)
                    {
                        bw.Write(config.id);
                        bw.Write(config.seed);
                        bw.Write(config.orbitPhase);
                    }
                }

                unsave = false;
                Console.WriteLine("save done");
            }
        });

        private Thread applyConfig = new Thread(() =>
        {
            
            Console.WriteLine("applyConfig");
            while (true)
            {
                Thread.Sleep(3000);
                if (GameMain.galaxy == null || GameMain.galaxy.stars == null)
                {
                    continue;
                }

                if (seed != GameMain.galaxy.seed)
                {
                    Console.WriteLine("seed与配置不一致");
                    continue;
                }

                for (int i = 0; i < configs.Count; i++)
                {
                    var p = GameMain.galaxy.PlanetById(configs[i].id);
                    if (p == null)
                    {
                        Console.WriteLine(i);
                        break;
                    }

                    planets.Add(p);
                    p.rotationPeriod = p.orbitalPeriod;
                    p.orbitPhase = configs[i].orbitPhase;
                }

                Console.WriteLine("apply");
                break;
            }
        });

        private static readonly string conifgPath = Environment.CurrentDirectory + "\\PlanetConfig.dat";
        private const int OPEN = 10;
        private static int times = 0;
        private static int count = 0;

        private static bool locked
        {
            get
            {
                if (GameMain.galaxy == null)
                {
                    return false;
                }

                playerOn = GameMain.galaxy.PlanetById(GameMain.mainPlayer.planetId);
                //Console.WriteLine("PlanetById");

                return playerOn != null && ((int) playerOn.orbitalPeriod) == ((int) playerOn.rotationPeriod);
            }
        }

        private void Awake()
        {
            th.Start();
            Console.WriteLine("**********load config**********");
            var _fileinfo = new FileInfo(conifgPath);
            if (_fileinfo.Exists)
            {
                Console.WriteLine("Exists");
                using (var bw = new BinaryReader(_fileinfo.OpenRead()))
                {
                    if (bw.ReadChar() != 'c')
                    {
                        Console.WriteLine("跳过配置文件加载");
                        return;
                    }

                    seed = bw.ReadInt32();
                    var temp = bw.ReadInt32();
                    for (var i = 0; i < temp; i++)
                    {
                        configs.Add(new PlanetConfig
                            {id = bw.ReadInt32(), seed = bw.ReadInt32(), orbitPhase = bw.ReadSingle()});
                    }
                }
            }

            applyConfig.Start();
            saveConfig.Start();
            Console.WriteLine("**********done**********");
        }

        private void FixedUpdate()
        {
            LockPlanet();

            OrbitPhasePlus();

            OrbitPhaseMinus();
        }

        private static void OrbitPhaseMinus()
        {
            if (Input.GetKeyDown(KeyCode.KeypadMinus) && locked)
            {
                UIRealtimeTip.Popup("将当前星球的公转相位-10");
                playerOn.orbitPhase -= 10.0f;
                count = 5;
                unsave = true;
            }
        }

        private static void OrbitPhasePlus()
        {
            if (Input.GetKeyDown(KeyCode.KeypadPlus) && locked)
            {
                UIRealtimeTip.Popup("将当前星球的公转相位+10");
                playerOn.orbitPhase += 10.0f;
                count = 5;
                unsave = true;
            }
        }

        private void LockPlanet()
        {
            if (Input.GetKeyDown(KeyCode.O))
            {
                times++;
                if (times > 4 && times < OPEN)
                {
                    UIRealtimeTip.Popup($"再按{OPEN - times}次将当前行星改为潮汐锁定");
                }

                if (times == OPEN)
                {
                    times = 0;
                    if (!locked)
                    {
                        if (GameMain.mainPlayer.planetId != 0)
                        {
                            UIRealtimeTip.Popup("将当前的星球改为潮汐锁定");
                            playerOn = GameMain.galaxy.PlanetById(GameMain.mainPlayer.planetId);
                            orbitalPeriod = playerOn.orbitalPeriod;
                            orbitPhase = playerOn.orbitPhase;
                            rotationPeriod = playerOn.rotationPeriod;
                            playerOn.rotationPeriod = playerOn.orbitalPeriod;
                            count = 5;
                            unsave = true;
                        }
                    }
                    else
                    {
                        UIRealtimeTip.Popup("当前的星球为潮汐锁定");
                    }
                }
            }
        }
    }
}