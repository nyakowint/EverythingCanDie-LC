using BepInEx;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;


namespace EverythingCanDie
{
    internal static class PluginInfo
    {
        public const string Guid = "nwnt.EverythingCanDie";
        public const string Name = "Everything Can Die";
        public const string Version = "1.2.0";
    }

    [BepInPlugin(PluginInfo.Guid, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance;
        public static Harmony Harmony;
        public static ManualLogSource Log;
        public static GameObject explosionPrefab;
        public static List<EnemyType> enemies;
        public static List<Item> items;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            Log = Logger;
            Harmony = new Harmony(PluginInfo.Guid);
            Harmony.PatchAll(typeof(Plugin));
            Logger.LogInfo(":]");
            var managerStartMethod = AccessTools.Method(typeof(RoundManager), "Start", new System.Type[] { });
            var managerStartPatch = new HarmonyMethod(typeof(Patches).GetMethod(nameof(Patches.RoundManagerPatch)));
            Harmony.Patch(managerStartMethod, postfix: managerStartPatch);

            var startMethod = AccessTools.Method(typeof(StartOfRound), "Start", new System.Type[] { });
            var startPatch = new HarmonyMethod(typeof(Patches).GetMethod(nameof(Patches.StartOfRoundPatch)));
            Harmony.Patch(startMethod, postfix: startPatch);

            var hitMethod = AccessTools.Method(typeof(EnemyAI), nameof(EnemyAI.HitEnemyOnLocalClient), new[] { typeof(int), typeof(Vector3), typeof(PlayerControllerB), typeof(bool) });
            var hitPatch = new HarmonyMethod(typeof(Patches).GetMethod(nameof(Patches.HitEnemyLocalPatch)));
            Harmony.Patch(hitMethod, postfix: hitPatch);

            var killMethod = AccessTools.Method(typeof(EnemyAI), nameof(EnemyAI.KillEnemyOnOwnerClient),
                new[] { typeof(bool) });
            var killPatch = new HarmonyMethod(typeof(Patches).GetMethod(nameof(Patches.KillEnemyPatch)));
            Harmony.Patch(killMethod, killPatch);

            var shootMethod = AccessTools.Method(typeof(ShotgunItem), nameof(ShotgunItem.ShootGun), new[] { typeof(Vector3), typeof(Vector3) });
            var shootPatch = new HarmonyMethod(typeof(Patches).GetMethod(nameof(Patches.ReplaceShotgunCode)));
            Harmony.Patch(shootMethod, prefix: shootPatch);
        }
    }
}