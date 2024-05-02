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
using Unity.Netcode;
using UnityEngine;

namespace EverythingCanDie
{
    internal static class PluginInfo
    {
        public const string Guid = "nwnt.EverythingCanDie";
        public const string Name = "EverythingCanDie";
        public const string Version = "1.2.17";
    }

    [BepInPlugin(PluginInfo.Guid, PluginInfo.Name, PluginInfo.Version)]
    [BepInDependency("Evaisa-LethalThings-0.10.3", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance;
        public static Harmony Harmony;
        public static ManualLogSource Log;
        public static GameObject explosionPrefab;
        public static List<EnemyType> enemies;
        public static List<Item> items;

        public const float range = 30f;
        public static int ENEMY_MASK = (1 << 19);
        public static int PLAYER_HIT_MASK; //2621448 = enemy mask
        public static int ENEMY_HIT_MASK;

        public static int numTightPellets = 2;
        public static float tightPelletAngle = 2.5f;
        public static int numLoosePellets = 3;
        public static float loosePelletAngle = 10f;

        public static bool hasEvaisaRPG = false;
        public static bool hasEvaisaHammer = false;

        private void Awake()
        {
            Harmony = new Harmony(PluginInfo.Guid);
            if (Instance == null)
            {
                Instance = this;
            }
            Harmony.PatchAll(typeof(Plugin));
            Log = Logger;
            CreateHarmonyPatch(Harmony, typeof(RoundManager), "Start", null, typeof(Patches), nameof(Patches.RoundManagerPatch), false);
            CreateHarmonyPatch(Harmony, typeof(StartOfRound), "Start", null, typeof(Patches), nameof(Patches.StartOfRoundPatch), false);
            //CreateHarmonyPatch(Harmony, typeof(RoundManager), nameof(RoundManager.SpawnEnemyGameObject), new[] { typeof(Vector3), typeof(float), typeof(int), typeof(EnemyType) }, typeof(Patches), nameof(Patches.PatchSpawnEnemyGameObject), false);
            CreateHarmonyPatch(Harmony, typeof(EnemyAI), nameof(EnemyAI.DoAIInterval), null, typeof(Patches), nameof(Patches.DoAIIntervalPatch), true);
            CreateHarmonyPatch(Harmony, typeof(EnemyAI), nameof(EnemyAI.OnCollideWithPlayer), new[] { typeof(Collider) }, typeof(Patches), nameof(Patches.OnCollideWithPlayerPatch), true);
            //CreateHarmonyPatch(Harmony, typeof(EnemyAI), nameof(EnemyAI.OnCollideWithEnemy), new[] { typeof(Collider) }, typeof(Patches), nameof(Patches.OnCollideWithEnemyPatch), true);
            CreateHarmonyPatch(Harmony, typeof(EnemyAI), nameof(EnemyAI.HitEnemyClientRpc), new[] { typeof(int), typeof(int), typeof(bool), typeof(int) }, typeof(Patches), nameof(Patches.HitEnemyClientPatch), false);
            //CreateHarmonyPatch(Harmony, typeof(RoundManager), nameof(RoundManager.SpawnEnemyGameObject), new[] { typeof(Vector3), typeof(float), typeof(int), typeof(EnemyType) }, typeof(Patches), nameof(Patches.PatchSpawnEnemyGameObject), false);
            CreateHarmonyPatch(Harmony, typeof(ShotgunItem), nameof(ShotgunItem.ShootGun), new[] { typeof(Vector3), typeof(Vector3) }, typeof(Patches), nameof(Patches.ReplaceShotgunCode), true);
            if (FindType("LethalThings.RocketLauncher") != null)
            {
                hasEvaisaRPG = true;
            }
            if (FindType("LethalThings.ToyHammer") != null)
            {
                hasEvaisaHammer = true;
            }
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
                Plugin.Log.LogInfo("Type is either incorrect or does not exist!");
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
            if (Plugin.Instance.Config[new ConfigDefinition("Mobs", identifier)].BoxedValue.ToString().ToUpper().Equals("TRUE"))
            {
                return true;
            }
            return false;
        }

        public static bool CanMob(string parentIdentifier, string identifier, string mobName)
        {
            string mob = RemoveInvalidCharacters(mobName).ToUpper();
            if (Plugin.Instance.Config[new ConfigDefinition("Mobs", parentIdentifier)].BoxedValue.ToString().ToUpper().Equals("TRUE"))
            {
                foreach (ConfigDefinition entry in Plugin.Instance.Config.Keys)
                {
                    if (RemoveInvalidCharacters(entry.Key.ToUpper()).Equals(RemoveInvalidCharacters(mob + identifier.ToUpper())))
                    {
                        return Plugin.Instance.Config[entry].BoxedValue.ToString().ToUpper().Equals("TRUE");
                    }
                }
                Plugin.Log.LogInfo(identifier + ": No mob found!");
                return false;
            }
            else
            {
                Plugin.Log.LogInfo(parentIdentifier + ": All mobs disabled!");

            }
            return false;
        }

        public static void VisualiseShot(Vector3 start, Vector3 end)
        {
            GameObject trail = new GameObject("Trail Visual");
            FadeOutLine line = trail.AddComponent<FadeOutLine>();
            line.start = start;
            line.end = end;
            line.Prep();
        }

        public class KilledEnemy : NetworkBehaviour
        {
            void Awake()
            {
                Plugin.Log.LogInfo("Killed!");
            }
        }

        public class FadeOutLine : MonoBehaviour
        {
            private const float lifetime = 0.4f;
            private const float width = 0.02f;
            private static readonly Color col = new Color(1f, 0f, 0f);

            private float alive = 0f;
            private LineRenderer line;
            public Vector3 start, end;
            private static readonly Material mat = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));
            public void Prep()
            {
                var len = Vector3.Distance(start, end);
                var lenFrac = (range - len) / range;
                line = gameObject.AddComponent<LineRenderer>();
                line.startColor = col;
                line.endColor = col * lenFrac + Color.black * (1f - lenFrac);
                line.startWidth = width;
                line.endWidth = lenFrac * width;
                line.SetPositions(new Vector3[] { start, end });
                line.material = mat;
            }
            void Update()
            {
                alive += Time.deltaTime;
                if (alive >= lifetime) Destroy(gameObject);
                else
                {
                    line.startColor = new Color(col.r, col.g, col.b, (lifetime - alive) / lifetime);
                    line.endColor = new Color(line.endColor.r, line.endColor.g, line.endColor.b, (lifetime - alive) / lifetime);
                }
            }
        }
    }
}
