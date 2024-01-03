using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using GameNetcodeStuff;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;


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
            Harmony = new Harmony(PluginInfo.Guid);
            if (Instance == null)
            {
                Instance = this;
            }
            Log = BepInEx.Logging.Logger.CreateLogSource(PluginInfo.Guid);
            Harmony.PatchAll(typeof(Plugin));
            Harmony.PatchAll(typeof(Patches));
            MethodInfo AI_KillEnemy_Method = AccessTools.Method(typeof(EnemyAI), nameof(EnemyAI.KillEnemyOnOwnerClient), null, null);
            MethodInfo KillEnemy_Patch_Method = AccessTools.Method(typeof(Patches), nameof(Patches.KillEnemyPatch), null, null);
            Harmony.Patch(AI_KillEnemy_Method, new HarmonyMethod(KillEnemy_Patch_Method), null, null, null, null);
            MethodInfo AI_HitEnemy_Method = AccessTools.Method(typeof(EnemyAI), nameof(EnemyAI.HitEnemyOnLocalClient), null, null);
            MethodInfo HitEnemy_Patch_Method = AccessTools.Method(typeof(Patches), nameof(Patches.HitEnemyLocalPatch), null, null);
            Harmony.Patch(AI_HitEnemy_Method, new HarmonyMethod(HitEnemy_Patch_Method), null, null, null, null);
            MethodInfo Shotgun_Method = AccessTools.Method(typeof(ShotgunItem), nameof(ShotgunItem.ShootGun), null, null);
            MethodInfo Shotgun_Patch_Method = AccessTools.Method(typeof(Patches), nameof(Patches.ReplaceShotgunCode), null, null);
            Harmony.Patch(Shotgun_Method, new HarmonyMethod(Shotgun_Patch_Method), null, null, null, null);
            Logger.LogInfo(":]");
        }
    }
}