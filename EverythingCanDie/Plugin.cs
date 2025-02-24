using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace EverythingCanDie
{
    [BepInPlugin(Guid, Name, Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "nwnt.EverythingCanDie";
        public const string Name = "EverythingCanDie";
        public const string Version = "1.3.1";

        public static Plugin Instance;
        public static Harmony Harmony;
        public static ManualLogSource Log;
        public static GameObject explosionPrefab;
        public static List<EnemyType> enemies;
        public static List<Item> items;

        public const float range = 30f;
        public static int ENEMY_MASK = (1 << 19);
        public static int PLAYER_HIT_MASK; //2621448 = enemy mask

        public static int numTightPellets = 2;
        public static float tightPelletAngle = 2.5f;
        public static int numLoosePellets = 3;
        public static float loosePelletAngle = 10f;

        private void Awake()
        {

            Harmony = new Harmony(Guid);
            if (Instance == null)
            {
                Instance = this;
            }
            Harmony.PatchAll(typeof(Plugin));
            Log = Logger;
            CreateHarmonyPatch(Harmony, typeof(StartOfRound), "Start", null, typeof(Patches), nameof(Patches.StartOfRoundPatch), false);
            CreateHarmonyPatch(Harmony, typeof(EnemyAI), nameof(EnemyAI.HitEnemy), new[] { typeof(int), typeof(PlayerControllerB), typeof(bool), typeof(int) }, typeof(Patches), nameof(Patches.HitEnemyPatch), false);
            CreateHarmonyPatch(Harmony, typeof(EnemyAI), nameof(EnemyAI.KillEnemy), new[] { typeof(bool) }, typeof(Patches), nameof(Patches.KillEnemyPatch), false);
            Logger.LogInfo("Patching should be complete now :]");
        }

        public static Type FindType(string fullName)
        {
            try
            {
                if (AppDomain.CurrentDomain.GetAssemblies()
                        .Where(a => !a.IsDynamic)
                        .SelectMany(a => a.GetTypes())
                        .FirstOrDefault(t => t.FullName.Equals(fullName)) != null)
                {
                    return AppDomain.CurrentDomain.GetAssemblies()
                        .Where(a => !a.IsDynamic)
                        .SelectMany(a => a.GetTypes())
                        .FirstOrDefault(t => t.FullName.Equals(fullName));
                }
            }
            catch
            {
                return null;
            }
            return null;
        }

        public static void CreateHarmonyPatch(Harmony harmony, Type typeToPatch, string methodToPatch, Type[] parameters, Type patchType, string patchMethod, bool isPrefix)
        {
            if (typeToPatch == null || patchType == null)
            {
                Log.LogInfo("Type is either incorrect or does not exist!");
                return;
            }
            MethodInfo Method = AccessTools.Method(typeToPatch, methodToPatch, parameters, null);
            MethodInfo Patch_Method = AccessTools.Method(patchType, patchMethod, null, null);

            if (isPrefix)
            {
                harmony.Patch(Method, new HarmonyMethod(Patch_Method), null, null, null, null);
                Log.LogInfo("Prefix " + Method.Name + " Patched!");
            }
            else
            {
                harmony.Patch(Method, null, new HarmonyMethod(Patch_Method), null, null, null);
                Log.LogInfo("Postfix " + Method.Name + " Patched!");
            }
        }

        public static string RemoveInvalidCharacters(string source)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in source)
            {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
                {
                    sb.Append(c);
                }
            }
            return string.Join("", sb.ToString().Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
        }

        public static bool Can(string identifier)
        {
            if (Instance.Config[new ConfigDefinition("Mobs", identifier)].BoxedValue.ToString().ToUpper().Equals("TRUE"))
            {
                return true;
            }
            return false;
        }

        public static bool CanMob(string parentIdentifier, string identifier, string mobName)
        {
            string mob = RemoveInvalidCharacters(mobName).ToUpper();
            if (Instance.Config[new ConfigDefinition("Mobs", parentIdentifier)].BoxedValue.ToString().ToUpper().Equals("TRUE"))
            {
                foreach (ConfigDefinition entry in Instance.Config.Keys)
                {
                    if (RemoveInvalidCharacters(entry.Key.ToUpper()).Equals(RemoveInvalidCharacters(mob + identifier.ToUpper())))
                    {
                        return Instance.Config[entry].BoxedValue.ToString().ToUpper().Equals("TRUE");
                    }
                }
                Log.LogInfo(identifier + ": No mob found!");
                return false;
            }
            else
            {
                Log.LogInfo(parentIdentifier + ": All mobs disabled!");

            }
            return false;
        }
    }
}
