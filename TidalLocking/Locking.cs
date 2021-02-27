using System;
using System.IO;
using System.Threading;
using BepInEx;
using UnityEngine;

namespace TidalLocking
{
    [BepInPlugin("CuSO4.DSPMOD.TidalLocking", "TidalLocking", "1.0")]
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

        private static PlanetConfig config;

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
            while (count>0)
            {
                count--;
                Thread.Sleep(1000);
            }
            Console.WriteLine("saveConfig");
            using (var bw = new BinaryWriter(new FileInfo(conifgPath).Create()))
            {
                bw.Write('b');
                bw.Write(config.id);
                bw.Write(config.seed);
                bw.Write(config.orbitPhase);
            }
        });

        private Thread applyConfig = new Thread(() =>
        {
            Console.WriteLine("applyConfig");
            while (true)
            {
                if (!locked && playerOn != null)
                {
                    Console.WriteLine("if in");
                    var con = new PlanetConfig
                        {id = playerOn.id, seed = playerOn.seed, orbitPhase = playerOn.orbitPhase};

                    if (con.Equals(config))
                    {
                        Console.WriteLine("apply");
                        playerOn.rotationPeriod = playerOn.orbitalPeriod;
                        playerOn.orbitPhase = config.orbitPhase;
                    }
                }
                Thread.Sleep(2000);
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
                if (GameMain.galaxy==null)
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
                    bw.ReadChar();
                    config = new PlanetConfig {id = bw.ReadInt32(), seed = bw.ReadInt32(), orbitPhase = bw.ReadSingle()};
                }
            }

            applyConfig.Start();
            Console.WriteLine("**********done**********");
        }

        private void FixedUpdate()
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
                            config = new PlanetConfig(playerOn.id, playerOn.seed, playerOn.orbitPhase);
                            count = 5;
                            try { saveConfig.Start(); }
                            catch (ThreadStateException) { }
                        }
                    }
                    else
                    {
                        UIRealtimeTip.Popup("当前的星球为潮汐锁定");
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.KeypadPlus) && locked)
            {
                UIRealtimeTip.Popup("将当前星球的公转相位+10");
                playerOn.orbitPhase += 10.0f;
                config.orbitPhase = playerOn.orbitPhase;
                count = 5;
                try { saveConfig.Start(); }
                catch (ThreadStateException) { }
            }

            if (Input.GetKeyDown(KeyCode.KeypadMinus) && locked)
            {
                UIRealtimeTip.Popup("将当前星球的公转相位-10");
                playerOn.orbitPhase -= 10.0f;
                config.orbitPhase = playerOn.orbitPhase;
                count = 5;
                try { saveConfig.Start(); }
                catch (ThreadStateException) { }
            }
        }
    }
}