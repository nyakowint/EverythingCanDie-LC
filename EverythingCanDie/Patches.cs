using BepInEx.Configuration;
using GameNetcodeStuff;
using HarmonyLib;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

// ReSharper disable InconsistentNaming

namespace EverythingCanDie
{
    public class Patches
    {
        private static readonly int Damage = Animator.StringToHash("damage");
        [HarmonyPatch(typeof(RoundManager), "Start")]
        [HarmonyPostfix]
        public static void RoundManagerPatch()
        {
            if (Plugin.explosionPrefab == null && StartOfRound.Instance.explosionPrefab != null)
            {
                Plugin.explosionPrefab = Object.Instantiate(StartOfRound.Instance.explosionPrefab);
                Plugin.explosionPrefab.GetComponent<AudioSource>().volume = 0.35f;
                Plugin.explosionPrefab.SetActive(false);
                Object.DontDestroyOnLoad(Plugin.explosionPrefab);
            }
            Plugin.enemies = Resources.FindObjectsOfTypeAll(typeof(EnemyType)).Cast<EnemyType>().Where(e => e != null).ToList();
            Plugin.items = Resources.FindObjectsOfTypeAll(typeof(Item)).Cast<Item>().Where(i => i != null).ToList();

            if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", "BoomableAllMobs")))
            {
                ConfigEntry<bool> tempEntry = Plugin.Instance.Config.Bind("Mobs", // Section Title
                "BoomableAllMobs", // The key of the configuration option in the configuration file
                                             true, // The default value
                                             "Leave On To Customise Mobs Below Or Turn Off To Make All Mobs Unable To Be Affected By This Mod."); // Description
            }
            foreach (EnemyType enemy in Plugin.enemies)
            {
                string mobName = ECDUtils.RemoveInvalidCharacters(enemy.enemyName);
                if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", mobName + ".Boomable")))
                {
                    ConfigEntry<bool> tempEntry = Plugin.Instance.Config.Bind("Mobs", // The section under which the option is shown
                                             mobName + ".Boomable", // The key of the configuration option in the configuration file
                                             true, // The default value
                                             "If true this mob will explode and if immortal it will also be killable."); // Description
                }
            }
        }

        [HarmonyPatch(typeof(StartOfRound), "Start")]
        [HarmonyPostfix]
        public static void StartOfRoundPatch()
        {
            if (Plugin.explosionPrefab == null && StartOfRound.Instance.explosionPrefab != null)
            {
                Plugin.explosionPrefab = Object.Instantiate(StartOfRound.Instance.explosionPrefab);
                Plugin.explosionPrefab.GetComponent<AudioSource>().volume = 0.35f;
                Plugin.explosionPrefab.SetActive(false);
                Object.DontDestroyOnLoad(Plugin.explosionPrefab);
            }

            Plugin.enemies = Resources.FindObjectsOfTypeAll(typeof(EnemyType)).Cast<EnemyType>().Where(e => e != null).ToList();
            Plugin.items = Resources.FindObjectsOfTypeAll(typeof(Item)).Cast<Item>().Where(i => i != null).ToList();

            if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", "BoomableAllMobs")))
            {
                ConfigEntry<bool> tempEntry = Plugin.Instance.Config.Bind("Mobs", // Section Title
                "BoomableAllMobs", // The key of the configuration option in the configuration file
                                             true, // The default value
                                             "Leave On To Customise Mobs Below Or Turn Off To Make All Mobs Unable To Be Affected By This Mod."); // Description
            }
            foreach (EnemyType enemy in Plugin.enemies)
            {
                string mobName = ECDUtils.RemoveInvalidCharacters(enemy.enemyName);
                if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", mobName + ".Boomable")))
                {
                    ConfigEntry<bool> tempEntry = Plugin.Instance.Config.Bind("Mobs", // The section under which the option is shown
                                             mobName + ".Boomable", // The key of the configuration option in the configuration file
                                             true, // The default value
                                             "If true this mob will explode and if immortal it will also be killable."); // Description
                }
            }
        }

        public static bool KillEnemyPatch(EnemyAI __instance, bool overrideDestroy = false)
        {

            if (__instance.isEnemyDead) return true;
            if (ECDUtils.CanMob("BoomableAllMobs", ".Boomable", __instance.enemyType.enemyName))
            {
                Plugin.Log.LogInfo($"Exploding {__instance.name}");
                __instance.enemyType.canDie = true;
                var enemyPos = __instance.transform.position;
                Object.Instantiate(Plugin.explosionPrefab, enemyPos, Quaternion.Euler(-90f, 0f, 0f),
                RoundManager.Instance.mapPropsContainer.transform).SetActive(value: true);
                HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
            }
            return true;
        }

        public static void HitEnemyLocalPatch(int force, Vector3 hitDirection, PlayerControllerB playerWhoHit,
            bool playHitSFX, EnemyAI __instance)
        {
            if (!__instance.isEnemyDead) 
            {
                if (ECDUtils.CanMob("BoomableAllMobs", ".Boomable", __instance.enemyType.enemyName))
                {
                    Plugin.Log.LogInfo(
                    $"Enemy Hit: {__instance.enemyType.enemyName}, health: {__instance.enemyHP}, canDie: {__instance.enemyType.canDie}");
                    if (__instance.creatureAnimator != null)
                    {
                        __instance.creatureAnimator.SetTrigger(Damage);
                    }
                    __instance.enemyHP -= force;
                    if (!(__instance.enemyHP > 0) || __instance.IsOwner)
                    {
                        Plugin.Log.LogInfo($"{__instance.name} HP is {__instance.enemyHP}, killing");
                        __instance.KillEnemyOnOwnerClient(true);
                    }
                }
            }
        }

        public static bool ReplaceShotgunCode(ShotgunItem __instance, Vector3 shotgunPosition, Vector3 shotgunForward)
        {
            ECDUtils.ShootGun(__instance, shotgunPosition, shotgunForward);
            return false;
        }
    }
}