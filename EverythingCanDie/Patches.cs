using BepInEx.Configuration;
using GameNetcodeStuff;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Collections;
using Unity.Netcode;

// ReSharper disable InconsistentNaming
namespace EverythingCanDie
{
    public class Patches
    {
        public static List<string> UnkillableEnemies = new List<string>();

        public static List<string> BonkableEnemies = new List<string>();

        public static List<string> NotBonkableEnemies = new List<string>();

        public static List<string> InvalidEnemies = new List<string>();

        private static readonly int Damage = Animator.StringToHash("damage");

        private static bool CheckingIfBonkable = false;

        public static void StartOfRoundPatch()
        {
            Plugin.enemies = Resources.FindObjectsOfTypeAll(typeof(EnemyType)).Cast<EnemyType>().Where(e => e != null).ToList();
            Plugin.items = Resources.FindObjectsOfTypeAll(typeof(Item)).Cast<Item>().Where(i => i != null).ToList();
            Plugin.ENEMY_MASK = (1 << 19);
            Plugin.PLAYER_HIT_MASK = StartOfRound.Instance.collidersRoomMaskDefaultAndPlayers | (Plugin.ENEMY_MASK & 2621448); //2621448 = enemy mask

            if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", "UnimmortalAllMobs")))
            {
                ConfigEntry<bool> tempEntry = Plugin.Instance.Config.Bind("Mobs", // Section Title
                "UnimmortalAllMobs", // The key of the configuration option in the configuration file
                                             true, // The default value
                                             "Leave On To Customise Mobs Below Or Turn Off To Make All Mobs Return To Normal(Make Immortal mobs immortal again)."); // Description
            }
            if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", "ExplosionEffectAllMobs")))
            {
                ConfigEntry<bool> tempEntry = Plugin.Instance.Config.Bind("Mobs", // Section Title
                "ExplosionEffectAllMobs", // The key of the configuration option in the configuration file
                                             true, // The default value
                                             "If this is set to true then explosion effect stays and removes the body otherwise when false all explosions on death will not appear and the body will appear."); // Description
            }
            if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", "HealthAllMobs")))
            {
                ConfigEntry<bool> tempEntry = Plugin.Instance.Config.Bind("Mobs", // Section Title
                "HealthAllMobs", // The key of the configuration option in the configuration file
                                             true, // The default value
                                             "If this is set to false the enemies health will remain the same otherwise have fun configging."); // Description
            }

            foreach (EnemyType enemy in Plugin.enemies)
            {
                string mobName = Plugin.RemoveInvalidCharacters(enemy.enemyName).ToUpper();

                try
                {
                    if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", mobName + ".Unimmortal")))
                    {
                        ConfigEntry<bool> tempEntry = Plugin.Instance.Config.Bind("Mobs", // The section under which the option is shown
                                                mobName + ".Unimmortal", // The key of the configuration option in the configuration file
                                                true, // The default value
                                                "If true this mob will explode and if immortal it will also be killable."); // Description
                    }
                    if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", mobName + ".Explodeable")))
                    {
                        ConfigEntry<bool> tempEntry = Plugin.Instance.Config.Bind("Mobs", // The section under which the option is shown
                                                mobName + ".Explodeable", // The key of the configuration option in the configuration file
                                                true, // The default value
                                                "The value of whether to spawn an explosion effect(Default on)"); // Description
                    }
                    if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", mobName + ".Hittable")))
                    {
                        ConfigEntry<bool> tempEntry = Plugin.Instance.Config.Bind("Mobs", // The section under which the option is shown
                                                mobName + ".Hittable", // The key of the configuration option in the configuration file
                                                true, // The default value
                                                "The value of whether this mob is hittable with things like shovels." +
                                                "(WARNING: If it is a modded item that happens to shoot then this might effect those items too)"); // Description
                    }
                    if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", mobName + ".Shootable")))
                    {
                        ConfigEntry<bool> tempEntry = Plugin.Instance.Config.Bind("Mobs", // The section under which the option is shown
                                                mobName + ".Shootable", // The key of the configuration option in the configuration file
                                                true, // The default value
                                                "The value of whether this mob is shootable with things like shotguns.(WARNING: Only works for items that use the ShotgunItem as its parent)"); // Description
                    }
                    if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", mobName + ".Health")))
                    {
                        EnemyAI enemyAI = enemy.enemyPrefab.GetComponent<EnemyAI>();
                        ConfigEntry<int> tempEntryHP = Plugin.Instance.Config.Bind("Mobs", // The section under which the option is shown
                                                mobName + ".Health", // The key of the configuration option in the configuration file
                                                enemyAI.enemyHP, // The default value
                                                "The value of the mobs health.(Default Vanilla is 3 for most unkillable mobs)"); // Description
                        if (Plugin.CanMob("HealthAllMobs", ".Unimmortal", mobName))
                        {
                            enemyAI.enemyHP = tempEntryHP.Value;
                            Plugin.Log.LogInfo($"Set {enemy.name} HP to {enemyAI.enemyHP}");
                        }
                    }
                }
                catch (Exception)
                {
                    Plugin.Log.LogInfo($"It was not possible to generate the configs for the enemy: {enemy.enemyName}");
                    InvalidEnemies.Add(enemy.enemyName);
                }

                try
                {
                    if (Plugin.CanMob("UnimmortalAllMobs", ".Unimmortal", mobName))
                    {
                        UnkillableEnemies.Add(enemy.enemyName);
                        enemy.canDie = true;
                    }

                    Plugin.Log.LogInfo($"Vanilla canDie variable for {enemy.enemyName} = {enemy.canDie}");
                }
                catch (Exception)
                {
                    Plugin.Log.LogError($"It was not possible to determine whether the enemy {enemy.enemyName} was killable or not");
                }
            }

            if (Plugin.explosionPrefab == null && StartOfRound.Instance.explosionPrefab != null)
            {
                Plugin.explosionPrefab = Object.Instantiate(StartOfRound.Instance.explosionPrefab);
                if (Plugin.explosionPrefab.GetComponent<AudioSource>() != null)
                {
                    Plugin.explosionPrefab.GetComponent<AudioSource>().volume = 0.35f;
                }
                Plugin.explosionPrefab.SetActive(false);
                Object.DontDestroyOnLoad(Plugin.explosionPrefab);
            }
        }

        public static void HitEnemyPatch(ref EnemyAI __instance, int force = 1, PlayerControllerB playerWhoHit = null)
        {
            if (__instance != null && !CheckingIfBonkable)
            {
                if (!InvalidEnemies.Contains(__instance.enemyType.enemyName) || !__instance.isEnemyDead) 
                {
                    EnemyType type = __instance.enemyType;
                    string name = Plugin.RemoveInvalidCharacters(type.enemyName).ToUpper();
                    bool canDamage = true;
                    if (Plugin.CanMob("UnimmortalAllMobs", ".Unimmortal", name))
                    {
                        if (playerWhoHit == null && __instance.enemyType.enemyName == "RadMech")
                        {
                            return;
                        }

                        if (playerWhoHit != null)
                        {
                            if (playerWhoHit.ItemSlots[playerWhoHit.currentItemSlot] != null)
                            {
                                GrabbableObject held = playerWhoHit.ItemSlots[playerWhoHit.currentItemSlot];
                                if (held.itemProperties.isDefensiveWeapon && !Plugin.Can(name + ".Hittable"))
                                {
                                    canDamage = false;
                                    Plugin.Log.LogInfo($"Hit Disabled for {__instance.enemyType.enemyName}!");
                                }
                                else if ((held is ShotgunItem) && !Plugin.Can(name + ".Shootable"))
                                {
                                    canDamage = false;
                                    Plugin.Log.LogInfo($"Shoot Disabled for {__instance.enemyType.enemyName}!");
                                }
                            }
                        }

                        if (canDamage)
                        {
                            if (!BonkableEnemies.Contains(__instance.enemyType.enemyName) && !NotBonkableEnemies.Contains(__instance.enemyType.enemyName))
                            {
                                Plugin.Log.LogInfo($"{__instance.enemyType.enemyName} is not in the Bonkable or in the NotBonkable list");
                                CanEnemyGetBonked(__instance);
                            }

                            if (BonkableEnemies.Contains(__instance.enemyType.enemyName))
                            {
                                Plugin.Log.LogInfo($"{__instance.enemyType.enemyName} is in the Bonkable list");
                            }
                            else if (NotBonkableEnemies.Contains(__instance.enemyType.enemyName))
                            {
                                Plugin.Log.LogInfo($"{__instance.enemyType.enemyName} is in the NotBonkable list");

                                if (__instance.creatureAnimator != null)
                                {
                                    __instance.creatureAnimator.SetTrigger(Damage);
                                }
                                if (__instance.enemyHP - force > 0)
                                {
                                    __instance.enemyHP -= force;
                                }
                                else
                                {
                                    __instance.enemyHP = 0;
                                }
                                if (__instance.enemyHP <= 0)
                                {
                                    __instance.KillEnemyOnOwnerClient(false);
                                }
                            }
                        }
                        else
                        {
                            return;
                        }
                    }
                }
            }
        }

        public static void CanEnemyGetBonked(EnemyAI __instance)
        {
            if(__instance.GetComponent<NutcrackerEnemyAI>() != null)
            {
                BonkableEnemies.Add(__instance.enemyType.enemyName);
                Plugin.Log.LogInfo($"{__instance.enemyType.enemyName} is been added to the Bonkable list");
                return;
            }
            
            CheckingIfBonkable = true;
            int beforeHitHP = __instance.enemyHP;
            Plugin.Log.LogInfo($"Enemy HP before bonk test: {beforeHitHP}");

            __instance.HitEnemy(1,default,default,-100);

            int afterHitHP = __instance.enemyHP;
            Plugin.Log.LogInfo($"Enemy HP after bonk test: {afterHitHP}");

            if (beforeHitHP != afterHitHP || __instance.isEnemyDead)
            {
                BonkableEnemies.Add(__instance.enemyType.enemyName);
                __instance.enemyHP = beforeHitHP;
                Plugin.Log.LogInfo($"{__instance.enemyType.enemyName} is been added to the Bonkable list, HP = {__instance.enemyHP}");
            }
            else
            {
                NotBonkableEnemies.Add(__instance.enemyType.enemyName);
                Plugin.Log.LogInfo($"{__instance.enemyType.enemyName} is been added to the NotBonkable list, HP = {__instance.enemyHP}");
            }
            CheckingIfBonkable = false;
        }

        public static void KillEnemyPatch(ref EnemyAI __instance)
        {
            if (__instance != null && !InvalidEnemies.Contains(__instance.enemyType.enemyName))
            {
                EnemyType type = __instance.enemyType;
                string name = Plugin.RemoveInvalidCharacters(type.enemyName).ToUpper();
                Plugin.Log.LogInfo($"{__instance.name} HP is {__instance.enemyHP}, killing");
                if (Plugin.CanMob("ExplosionEffectAllMobs", ".Explodeable", name))
                {
                    if (__instance.enemyType.enemyName == "Nutcracker" || __instance.enemyType.enemyName == "Butler")
                    {
                        HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
                        if (__instance.IsServer)
                        {
                            Object.Instantiate(Plugin.explosionPrefab, __instance.transform.position, Quaternion.Euler(-90f, 0f, 0f),
                            RoundManager.Instance.mapPropsContainer.transform).SetActive(value: true);
                        }
                    }
                    else if (__instance.GetComponentInChildren<PlayerControllerB>())
                    {
                        HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
                        if (__instance.IsServer)
                        {
                            Object.Instantiate(Plugin.explosionPrefab, __instance.transform.position, Quaternion.Euler(-90f, 0f, 0f),
                            RoundManager.Instance.mapPropsContainer.transform).SetActive(value: true);
                        }
                    }
                    else
                    {
                        HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
                        if (__instance.IsServer)
                        {
                            Object.Instantiate(Plugin.explosionPrefab, __instance.transform.position, Quaternion.Euler(-90f, 0f, 0f),
                            RoundManager.Instance.mapPropsContainer.transform).SetActive(value: true);
                        }
                        __instance.StartCoroutine(MoveBody(__instance, 0.1f));
                    }
                }
            }
        }

        static IEnumerator MoveBody(EnemyAI __instance, float time)
        {
            yield return new WaitForSeconds(time);

            Vector3 OriginalBodyPos = new Vector3(-10000, -10000, -10000);
            __instance.transform.position = OriginalBodyPos;
            __instance.SyncPositionToClients();
            if (__instance.enemyType.enemyName == "Blob" && __instance.IsServer)
            {
                __instance.GetComponent<NetworkObject>().Despawn();
            }
        }
    }
}
