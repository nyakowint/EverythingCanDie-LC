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
            Plugin.ENEMY_HIT_MASK = StartOfRound.Instance.collidersRoomMaskDefaultAndPlayers;

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
                try
                {
                    string mobName = Plugin.RemoveInvalidCharacters(enemy.enemyName).ToUpper();
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
                    if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", mobName + ".Rocketable")))
                    {
                        ConfigEntry<bool> tempEntry = Plugin.Instance.Config.Bind("Mobs", // The section under which the option is shown
                                                mobName + ".Rocketable", // The key of the configuration option in the configuration file
                                                true, // The default value
                                                "The value of whether this mob is able to be shot with rockets from LethalThings rocket launcher(optional if LethalThings is installed)."); // Description
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
                            Plugin.Log.LogInfo("Set " + enemy.name + " HP to " + enemyAI.enemyHP);
                        }
                    }
                }
                catch (Exception)
                {
                    Plugin.Log.LogError("It was not possible to generate the configs for the enemy: " + enemy.enemyName);
                    InvalidEnemies.Add(enemy.enemyName);
                }
            }

            foreach (EnemyType enemy in Plugin.enemies)
            {
                try
                {
                    string name = Plugin.RemoveInvalidCharacters(enemy.enemyName).ToUpper();
                    if (enemy.canDie)
                    {
                        Plugin.Log.LogInfo("Vanilla canDie variable for " + enemy.enemyName + " = true");
                    }
                    else if (Plugin.CanMob("UnimmortalAllMobs", ".Unimmortal", name))
                    {
                        Plugin.Log.LogInfo("Vanilla canDie variable for " + enemy.enemyName + " = false");
                        UnkillableEnemies.Add(enemy.enemyName);
                        enemy.canDie = true;
                    }
                }
                catch (Exception)
                {
                    Plugin.Log.LogError("It was not possible to determine whether the enemy (" + enemy.enemyName + ") was killable or not");
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
            if (__instance != null && !CheckingIfBonkable && !InvalidEnemies.Contains(__instance.enemyType.enemyName))
            {
                EnemyType type = __instance.enemyType;
                string name = Plugin.RemoveInvalidCharacters(type.enemyName).ToUpper();
                bool canDamage = true;
                if (Plugin.CanMob("UnimmortalAllMobs", ".Unimmortal", name))
                {
                    if (playerWhoHit != null)
                    {
                        GrabbableObject held = playerWhoHit.ItemSlots[playerWhoHit.currentItemSlot];
                        if (held.itemProperties.isDefensiveWeapon && !Plugin.Can(name + ".Hittable"))
                        {
                            if (!(held is ShotgunItem))
                            {
                                if (Plugin.hasEvaisaRPG)
                                {
                                    if (!held.GetType().IsEquivalentTo(Plugin.FindType("LethalThings.RocketLauncher")))
                                    {
                                        canDamage = false;
                                        Plugin.Log.LogInfo($"Hit Disabled for {name}!");
                                    }
                                }
                                else
                                {
                                    canDamage = false;
                                    Plugin.Log.LogInfo($"Hit Disabled for {name}!");
                                }
                            }
                        }
                        else if ((held is ShotgunItem) && !Plugin.Can(name + ".Shootable"))
                        {
                            canDamage = false;
                            Plugin.Log.LogInfo($"Shoot Disabled for {name}!");
                        }
                        else if (Plugin.hasEvaisaRPG && !Plugin.Can(name + ".Rocketable"))
                        {
                            if (held.GetType().IsEquivalentTo(Plugin.FindType("LethalThings.RocketLauncher")))
                            {
                                canDamage = false;
                                Plugin.Log.LogInfo($"Rockets Disabled for {name}!");
                            }
                        }
                    }
                    if (canDamage)
                    {
                        if (BonkableEnemies.Contains(__instance.enemyType.enemyName))
                        {
                            if (__instance.enemyHP > 1)
                            {
                                __instance.enemyHP += force;
                            }
                            Plugin.Log.LogInfo(__instance.enemyType.enemyName + " is in the Bonkable list");
                        }
                        else
                        {
                            if (!NotBonkableEnemies.Contains(__instance.enemyType.enemyName))
                            {
                                Plugin.Log.LogInfo(__instance.enemyType.enemyName + " is not in the Bonkable or in the NotBonkable list");
                                CanEnemyGetBonked(__instance);
                            }
                            else
                            {
                                Plugin.Log.LogInfo(__instance.enemyType.enemyName + " is in the NotBonkable list");
                            }
                        }
                        if (__instance.creatureAnimator != null)
                        {
                            __instance.creatureAnimator.SetTrigger(Damage);
                        }
                        if (__instance.enemyHP - force > 0)
                        {
                            __instance.enemyHP -= force;
                            Plugin.Log.LogInfo($"Enemy Hit: {name}, health: {__instance.enemyHP}, canDie: {type.canDie}");
                        }
                        else
                        {
                            __instance.enemyHP = 0;
                            Plugin.Log.LogInfo($"Enemy Hit: {name}, health: {__instance.enemyHP}, canDie: {type.canDie}");
                        }
                        if (__instance.enemyHP <= 0)
                        {
                            __instance.KillEnemyOnOwnerClient(false);
                        }
                    }
                }
            }
        }

        public static void CanEnemyGetBonked(EnemyAI __instance)
        {
            CheckingIfBonkable = true;
            int beforeHitHP = __instance.enemyHP;
            Plugin.Log.LogInfo("Enemy HP before bonk test: " + beforeHitHP);

            __instance.HitEnemy();

            int afterHitHP = __instance.enemyHP;
            Plugin.Log.LogInfo("Enemy HP after bonk test: " + afterHitHP);

            if (beforeHitHP != afterHitHP && !BonkableEnemies.Contains(__instance.enemyType.enemyName))
            {
                BonkableEnemies.Add(__instance.enemyType.enemyName);
                __instance.enemyHP = beforeHitHP;
                __instance.enemyHP++;
                Plugin.Log.LogInfo(__instance.enemyType.enemyName + " is been added to the Bonkable list, HP = " + __instance.enemyHP);
            }
            else
            {
                NotBonkableEnemies.Add(__instance.enemyType.enemyName);
                Plugin.Log.LogInfo(__instance.enemyType.enemyName + " is been added to the NotBonkable list, HP =" + __instance.enemyHP);
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

                            if (!__instance.isEnemyDead)
                            {
                                __instance.KillEnemyOnOwnerClient(false);
                            }
                        }
                    }
                    else
                    {
                        HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
                        if (__instance.IsServer)
                        {
                            Object.Instantiate(Plugin.explosionPrefab, __instance.transform.position, Quaternion.Euler(-90f, 0f, 0f),
                            RoundManager.Instance.mapPropsContainer.transform).SetActive(value: true);
                            if (!__instance.isEnemyDead)
                            {
                                __instance.KillEnemyOnOwnerClient(false);
                            }
                        }
                        __instance.StartCoroutine(MoveBody(__instance, 0.1f));
                    }
                }
                else
                {
                    if (UnkillableEnemies.Contains(type.enemyName))
                    {
                        if (!__instance.isEnemyDead && __instance.IsServer)
                        {
                            __instance.KillEnemyOnOwnerClient(false);
                        }
                        __instance.StartCoroutine(MoveBody(__instance, 4));
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

        public static bool ReplaceShotgunCode(ref ShotgunItem __instance, Vector3 shotgunPosition, Vector3 shotgunForward)
        {
            ShootGun(__instance, shotgunPosition, shotgunForward);
            return false;
        }

        public static void ShootGun(ShotgunItem gun, Vector3 shotgunPosition, Vector3 shotgunForward)
        {
            PlayerControllerB holder = gun.playerHeldBy;
            bool playerFired = gun.isHeld && gun.playerHeldBy != null;
            if (playerFired)
            {
                // correct offset to something more reasonable when a player fires
                //shotgunPosition += GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.up * 0.25f; // vanilla code is -0.45
                shotgunPosition = gun.shotgunRayPoint.position;
            }
            bool thisPlayerFired = playerFired && gun.playerHeldBy == GameNetworkManager.Instance.localPlayerController;
            if (thisPlayerFired) gun.playerHeldBy.playerBodyAnimator.SetTrigger("ShootShotgun");

            // fire and reduce shell count - copied from vanilla

            RoundManager.PlayRandomClip(gun.gunShootAudio, gun.gunShootSFX, randomize: true, 1f, 1840);
            WalkieTalkie.TransmitOneShotAudio(gun.gunShootAudio, gun.gunShootSFX[0]);
            gun.gunShootParticle.Play(withChildren: true);

            gun.isReloading = false;
            gun.shellsLoaded = Mathf.Clamp(gun.shellsLoaded - 1, 0, 2);

            PlayerControllerB localPlayerController = GameNetworkManager.Instance.localPlayerController;
            if (localPlayerController == null) return;

            // generic firing stuff - replaced with pellets

            // generate pellet vectors (done separately to minimise time random state is modified)
            var vectorList = new Vector3[Plugin.numTightPellets + Plugin.numLoosePellets];
            var oldRandomState = UnityEngine.Random.state;
            UnityEngine.Random.InitState(UnityEngine.Random.Range(0, int.MaxValue));
            for (int i = 0; i < Plugin.numTightPellets + Plugin.numLoosePellets; i++)
            {
                float variance = (i < Plugin.numTightPellets) ? Plugin.tightPelletAngle : Plugin.loosePelletAngle;
                var circlePoint = UnityEngine.Random.onUnitSphere; // pick a random point on a sphere
                var angle = variance * Mathf.Sqrt(UnityEngine.Random.value); // pick a random angle to spread by
                if (Vector3.Angle(shotgunForward, circlePoint) < angle) circlePoint *= -1; // make sure the spread will be by the specified angle amount
                var vect = Vector3.RotateTowards(shotgunForward, circlePoint, angle * Mathf.PI / 180f, 0f); // rotate towards that random point, capped by chosen angle
                vectorList[i] = vect;
            }
            UnityEngine.Random.state = oldRandomState;

            // calculate ear ring and shake based on distance to gun
            float distance = Vector3.Distance(localPlayerController.transform.position, gun.shotgunRayPoint.transform.position);
            float earRingSeverity = 0f;
            if (distance < 5f)
            {
                earRingSeverity = 0.8f;
                HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
            }
            else if (distance < 15f)
            {
                earRingSeverity = 0.5f;
                HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
            }
            else if (distance < 23f) HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);

            if (earRingSeverity > 0f && SoundManager.Instance.timeSinceEarsStartedRinging > 16f && !playerFired)
                gun.StartCoroutine(DelayedEarsRinging(earRingSeverity));

            List<GameObject> targets = new List<GameObject>();
            // raycast those vectors to find hits
            Ray ray;
            // TODO: modify count tracker to handle distance pellets travel? sqrt(1-dist/range) seems reasonable for damage worth
            for (int i = 0; i < vectorList.Length; i++)
            {
                Vector3 vect = vectorList[i];
                ray = new Ray(shotgunPosition, vect);
                RaycastHit[] hits = Physics.RaycastAll(ray, Plugin.range, playerFired ? Plugin.PLAYER_HIT_MASK : Plugin.ENEMY_HIT_MASK, QueryTriggerInteraction.Collide);
                Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));
                Vector3 end = shotgunPosition + vect * Plugin.range;
                for (int j = 0; j < hits.Length; j++)
                {
                    GameObject obj = hits[j].transform.gameObject;
                    if (obj.TryGetComponent(out IHittable hittable))
                    {
                        if (ReferenceEquals(hittable, gun.playerHeldBy))
                            continue; // self hit
                        EnemyAI ai = null;
                        if (hittable is EnemyAICollisionDetect detect)
                            ai = detect.mainScript;
                        if (ai != null)
                        {
                            if (!playerFired)
                                continue; // enemy hit enemy
                            if (ai.isEnemyDead || ai.enemyHP <= 0 || !ai.enemyType.canDie)
                                continue; // skip dead things
                        }
                        if (hittable is PlayerControllerB)
                            targets.Add(obj);
                        else if (ai != null)
                            targets.Add(obj);
                        else if (playerFired)
                            targets.Add(obj);
                        else continue; // enemy hit something else (webs?)
                        end = hits[j].point;
                        break;
                    }
                    else
                    {
                        // precaution: hit enemy without hitting hittable (immune to shovels?)
                        if (hits[j].collider.TryGetComponent(out EnemyAI ai))
                        {
                            if (playerFired && !ai.isEnemyDead && ai.enemyHP > 0)
                            {
                                targets.Add(ai.gameObject);
                                end = hits[j].point;
                                break;
                            }
                            else continue;
                        }
                        end = hits[j].point;
                        break; // wall or other obstruction
                    }
                }
                Plugin.VisualiseShot(shotgunPosition, end);
            }
            // deal damage all at once - prevents piercing alive and reduces damage calls
            targets.RemoveAll(t => t == null);
            if (targets.Any())
            {
                targets.ForEach(t =>
                {
                    if (t == null)
                    {
                        Plugin.Log.LogInfo("NO!");
                    }
                    else
                    {
                        if (t.GetComponent<EnemyAI>() != null)
                        {
                            EnemyAI enemy = t.GetComponent<EnemyAI>();
                            int damage = 1;
                            enemy.HitEnemyOnLocalClient(damage, default, holder);
                        }
                        else if (t.GetComponent<IHittable>() != null)
                        {
                            IHittable hit = t.GetComponent<IHittable>();
                            if (hit is EnemyAICollisionDetect)
                            {
                                EnemyAICollisionDetect enemy = (EnemyAICollisionDetect)hit;
                                int damage = 1;
                                enemy.mainScript.HitEnemyOnLocalClient(damage, default, holder);
                            }
                            else if (hit is PlayerControllerB)
                            {
                                PlayerControllerB player = (PlayerControllerB)hit;
                                // grouping player damage also ensures strong hits (3+ pellets) ignore critical damage - 5 is always lethal rather than being critical
                                int damage = 18;
                                player.DamagePlayer(damage, true, true, CauseOfDeath.Gunshots, 0, false, shotgunForward);
                            }
                            else
                            {
                                hit.Hit(1, shotgunForward, gun.playerHeldBy, true);
                            }
                        }
                    }
                });
            }

            ray = new Ray(shotgunPosition, shotgunForward);
            if (Physics.Raycast(ray, out RaycastHit hitInfo, Plugin.range, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
            {
                gun.gunBulletsRicochetAudio.transform.position = ray.GetPoint(hitInfo.distance - 0.5f);
                gun.gunBulletsRicochetAudio.Play();
            }
        }

        public static IEnumerator DelayedEarsRinging(float effectSeverity)
        {
            yield return new WaitForSeconds(0.6f);
            SoundManager.Instance.earsRingingTimer = effectSeverity;
        }
    }
}
