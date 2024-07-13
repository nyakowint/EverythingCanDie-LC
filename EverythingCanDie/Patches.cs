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
            CheckingIfBonkable = true;
            int beforeHitHP = __instance.enemyHP;
            Plugin.Log.LogInfo($"Enemy HP before bonk test: {beforeHitHP}");

            __instance.HitEnemy(1,default,default,-3);

            int afterHitHP = __instance.enemyHP;
            Plugin.Log.LogInfo($"Enemy HP after bonk test: {afterHitHP}");

            if (beforeHitHP != afterHitHP)
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
                shotgunPosition += GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.up * 0.25f; // vanilla code is -0.45
            }
            bool thisPlayerFired = playerFired && gun.playerHeldBy == GameNetworkManager.Instance.localPlayerController;
            if (thisPlayerFired) gun.playerHeldBy.playerBodyAnimator.SetTrigger("ShootShotgun");

            // fire and reduce shell count - copied from vanilla

            RoundManager.PlayRandomClip(gun.GetComponent<ShotgunItem>().gunShootAudio, gun.GetComponent<ShotgunItem>().gunShootSFX, randomize: true, 1f, 1840);
            WalkieTalkie.TransmitOneShotAudio(gun.GetComponent<ShotgunItem>().gunShootAudio, gun.GetComponent<ShotgunItem>().gunShootSFX[0]);
            gun.gunShootParticle.Play(withChildren: true);

            gun.isReloading = false;
            gun.shellsLoaded = Mathf.Clamp(gun.shellsLoaded - 1, 0, 2);

            PlayerControllerB localPlayerController = GameNetworkManager.Instance.localPlayerController;
            if (localPlayerController == null) return;

            // generic firing stuff - replaced with pellets

            // generate pellet vectors (done separately to minimise time random state is modified)
            var vectorList = new Vector3[Plugin.numTightPellets + Plugin.numLoosePellets];
            var oldRandomState = UnityEngine.Random.state;
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

            // raycast those vectors to find hits
            Ray ray;
            var counts = new Plugin.CountHandler();
            // TODO: modify count tracker to handle distance pellets travel? sqrt(1-dist/range) seems reasonable for damage worth
            for (int i = 0; i < vectorList.Length; i++)
            {
                Vector3 vect = vectorList[i];
                ray = new Ray(shotgunPosition, vect);
                RaycastHit[] hits = Physics.RaycastAll(ray, Plugin.range, Plugin.PLAYER_HIT_MASK, QueryTriggerInteraction.Collide);
                Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));
                Vector3 end = shotgunPosition + vect * Plugin.range;
                Debug.Log("SHOTGUN: RaycastAll hit " + hits.Length + " things (" + playerFired + "," + thisPlayerFired + ")");
                for (int j = 0; j < hits.Length; j++)
                {
                    GameObject obj = hits[j].transform.gameObject;

                    Debug.Log("obj = " + obj);

                    if (obj.TryGetComponent(out IHittable hittable))
                    {
                        if (ReferenceEquals(hittable, gun.playerHeldBy)) continue; // self hit
                        EnemyAI ai = null;
                        if (hittable is EnemyAICollisionDetect detect) ai = detect.mainScript;

                        Debug.Log("ai = " + ai);

                        if (ai != null)
                        {
                            if (ai.isEnemyDead || ai.enemyHP <= 0 || !ai.enemyType.canDie) continue; // skip dead things
                        }
                        if (hittable is PlayerControllerB) counts.AddPlayerToCount(hittable as PlayerControllerB);
                        else if (ai != null) counts.AddEnemyToCount(ai);
                        else if (playerFired) counts.AddOtherToCount(hittable);
                        else continue; // enemy hit something else (webs?)
                        end = hits[j].point;
                        Debug.Log("SHOTGUN: Hit [" + hittable + "] (" + (j + 1) + "@" + Vector3.Distance(shotgunPosition, end) + ")");
                        break;
                    }
                    else
                    {
                        // precaution: hit enemy without hitting hittable (immune to shovels?)
                        if (hits[j].collider.TryGetComponent(out EnemyAI ai))
                        {
                            if (playerFired && !ai.isEnemyDead && ai.enemyHP > 0 && ai.enemyType.canDie)
                            {
                                counts.AddEnemyToCount(ai);
                                end = hits[j].point;
                                Debug.Log("SHOTGUN: Backup hit [" + ai + "] (" + (j + 1) + "@" + Vector3.Distance(shotgunPosition, end) + ")");
                                break;
                            }
                            else continue;
                        }
                        end = hits[j].point;
                        Debug.Log("SHOTGUN: Wall [" + obj + "] (" + (j + 1) + "@" + Vector3.Distance(shotgunPosition, end) + ")");
                        break; // wall or other obstruction
                    }
                }
                Plugin.VisualiseShot(shotgunPosition, end);
            }

            // deal damage all at once - prevents piercing alive and reduces damage calls
            counts.player.ForEach(p => {
                // grouping player damage also ensures strong hits (3+ pellets) ignore critical damage - 5 is always lethal rather than being critical
                int damage = p.count * 20;
                Debug.Log("SHOTGUN: Hit " + p.item + " with " + p.count + " pellets for " + damage + " damage");
                p.item.DamagePlayer(damage, true, true, CauseOfDeath.Gunshots, 0, false, shotgunForward);
            });
            counts.enemy.ForEach(e => {
                // doing 1:1 damage is too strong, but one pellet should always do damage
                int damage = e.count / 2 + 1; // half rounded down plus one (1,2,2,3,3,4,4,5,5,6)
                Debug.Log("SHOTGUN: Hit " + e.item + " with " + e.count + " pellets for " + damage + " damage");
                e.item.HitEnemyOnLocalClient(damage, shotgunForward, gun.playerHeldBy, true);
            });
            counts.other.ForEach(o => {
                int damage = o.count / 2 + 1;
                Debug.Log("SHOTGUN: Hit " + o.item + " with " + o.count + " pellets for " + damage + " damage");
                o.item.Hit(damage, shotgunForward, gun.playerHeldBy, true);
            });

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