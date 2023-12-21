using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;


namespace EverythingCanDie
{
    internal static class PluginInfo
    {
        public const string Guid = "nwnt.EverythingCanDie";
        public const string Name = "Everything Can Die";
        public const string Version = "1.1.0";
    }

    [BepInPlugin(PluginInfo.Guid, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance;
        public static Harmony Harmony;
        public static ManualLogSource Log;

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

            Logger.LogInfo(":]");

            var killMethod = AccessTools.Method(typeof(EnemyAI), nameof(EnemyAI.KillEnemyOnOwnerClient),
                new[] { typeof(bool) });
            var killPatch = new HarmonyMethod(typeof(Patches).GetMethod(nameof(Patches.KillEnemyPatch)));
            Harmony.Patch(killMethod, killPatch);
            
            var hitMethod = AccessTools.Method(typeof(EnemyAI), nameof(EnemyAI.HitEnemyOnLocalClient), new []{ typeof(int), typeof(Vector3), typeof(PlayerControllerB), typeof(bool) });
            var hitPatch = new HarmonyMethod(typeof(Patches).GetMethod(nameof(Patches.HitEnemyLocalPatch)));
            Harmony.Patch(hitMethod, postfix: hitPatch);
            
        }
    }
}