using BepInEx.Configuration;
using GameNetcodeStuff;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;
using Unity.Netcode;

// ReSharper disable InconsistentNaming
namespace EverythingCanDie
{
    public class Patches
    {
        private static readonly int Damage = Animator.StringToHash("damage");

        public static void RoundManagerPatch()
        {
            Plugin.enemies = Resources.FindObjectsOfTypeAll(typeof(EnemyType)).Cast<EnemyType>().Where(e => e != null).ToList();
            Plugin.items = Resources.FindObjectsOfTypeAll(typeof(Item)).Cast<Item>().Where(i => i != null).ToList();

            if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", "BoomableAllMobs")))
            {
                ConfigEntry<bool> tempEntry = Plugin.Instance.Config.Bind("Mobs", // Section Title
                "BoomableAllMobs", // The key of the configuration option in the configuration file
                                             true, // The default value
                                             "Leave On To Customise Mobs Below Or Turn Off To Make All Mobs Unable To Be Affected By This Mod."); // Description
            }
            if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", "DefaultHealthAllMobs")))
            {
                ConfigEntry<bool> tempEntry = Plugin.Instance.Config.Bind("Mobs", // Section Title
                "DefaultHealthAllMobs", // The key of the configuration option in the configuration file
                                             false, // The default value
                                             "If this is set to true then none of the health values will matter and will be default vanilla"); // Description
            }
            if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", "ExplosionEffectAllMobs")))
            {
                ConfigEntry<bool> tempEntry = Plugin.Instance.Config.Bind("Mobs", // Section Title
                "ExplosionEffectAllMobs", // The key of the configuration option in the configuration file
                                             false, // The default value
                                             "If this is set to true then explosion effect stays otherwise when false all explosions on death will not appear."); // Description
            }
            if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", "HealthAllMobs")))
            {
                ConfigEntry<bool> tempEntry = Plugin.Instance.Config.Bind("Mobs", // Section Title
                "HealthAllMobs", // The key of the configuration option in the configuration file
                                             true, // The default value
                                             "If this is set to false it will be set to health values I set otherwise have fun configging"); // Description
            }
            foreach (EnemyType enemy in Plugin.enemies)
            {
                string mobName = Plugin.RemoveInvalidCharacters(enemy.enemyName).ToUpper();
                if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", mobName + ".Boomable")))
                {
                    ConfigEntry<bool> tempEntry = Plugin.Instance.Config.Bind("Mobs", // The section under which the option is shown
                                             mobName + ".Boomable", // The key of the configuration option in the configuration file
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
                if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", mobName + ".Health")))
                {
                    ConfigEntry<int> tempEntry = Plugin.Instance.Config.Bind("Mobs", // The section under which the option is shown
                                             mobName + ".Health", // The key of the configuration option in the configuration file
                                             3, // The default value
                                             "The value of the mobs health.(Default Vanilla is 3 for most mobs)"); // Description
                }
                else if (Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", mobName + ".Health")))
                {
                    foreach (ConfigDefinition def in Plugin.Instance.Config.Keys)
                    {
                        CreateHealthConfigEntry(mobName, def);
                    }
                }
            }

            if (StartOfRound.Instance != null)
            {
                Plugin.ENEMY_MASK = (1 << 19);
                Plugin.PLAYER_HIT_MASK = StartOfRound.Instance.collidersRoomMaskDefaultAndPlayers | Plugin.ENEMY_MASK | 2621448; //2621448 = enemy mask
                Plugin.ENEMY_HIT_MASK = StartOfRound.Instance.collidersRoomMaskDefaultAndPlayers;
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
        }

        public static void StartOfRoundPatch()
        {
            Plugin.enemies = Resources.FindObjectsOfTypeAll(typeof(EnemyType)).Cast<EnemyType>().Where(e => e != null).ToList();
            Plugin.items = Resources.FindObjectsOfTypeAll(typeof(Item)).Cast<Item>().Where(i => i != null).ToList();
            Plugin.ENEMY_MASK = (1 << 19);
            Plugin.PLAYER_HIT_MASK = StartOfRound.Instance.collidersRoomMaskDefaultAndPlayers | Plugin.ENEMY_MASK | 2621448; //2621448 = enemy mask
            Plugin.ENEMY_HIT_MASK = StartOfRound.Instance.collidersRoomMaskDefaultAndPlayers;

            if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", "BoomableAllMobs")))
            {
                ConfigEntry<bool> tempEntry = Plugin.Instance.Config.Bind("Mobs", // Section Title
                "BoomableAllMobs", // The key of the configuration option in the configuration file
                                             true, // The default value
                                             "Leave On To Customise Mobs Below Or Turn Off To Make All Mobs Unable To Be Affected By This Mod."); // Description
            }
            if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", "DefaultHealthAllMobs")))
            {
                ConfigEntry<bool> tempEntry = Plugin.Instance.Config.Bind("Mobs", // Section Title
                "DefaultHealthAllMobs", // The key of the configuration option in the configuration file
                                             false, // The default value
                                             "If this is set to true then none of the health values will matter and will be default vanilla"); // Description
            }
            if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", "ExplosionEffectAllMobs")))
            {
                ConfigEntry<bool> tempEntry = Plugin.Instance.Config.Bind("Mobs", // Section Title
                "ExplosionEffectAllMobs", // The key of the configuration option in the configuration file
                                             false, // The default value
                                             "If this is set to true then explosion effect stays otherwise when false all explosions on death will not appear."); // Description
            }
            if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", "HealthAllMobs")))
            {
                ConfigEntry<bool> tempEntry = Plugin.Instance.Config.Bind("Mobs", // Section Title
                "HealthAllMobs", // The key of the configuration option in the configuration file
                                             true, // The default value
                                             "If this is set to false it will be set to health values I set otherwise have fun configging"); // Description
            }
            foreach (EnemyType enemy in Plugin.enemies)
            {
                string mobName = Plugin.RemoveInvalidCharacters(enemy.enemyName).ToUpper();
                if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", mobName + ".Boomable")))
                {
                    ConfigEntry<bool> tempEntry = Plugin.Instance.Config.Bind("Mobs", // The section under which the option is shown
                                             mobName + ".Boomable", // The key of the configuration option in the configuration file
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
                if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", mobName + ".Health")))
                {
                    ConfigEntry<int> tempEntry = Plugin.Instance.Config.Bind("Mobs", // The section under which the option is shown
                                             mobName + ".Health", // The key of the configuration option in the configuration file
                                             3, // The default value
                                             "The value of the mobs health.(Default Vanilla is 3 for most mobs)"); // Description
                }
                else if (Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", mobName + ".Health")))
                {
                    foreach (ConfigDefinition def in Plugin.Instance.Config.Keys)
                    {
                        CreateHealthConfigEntry(mobName, def);
                    }
                }
            }

            if (StartOfRound.Instance != null)
            {
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
        }

        public static void PatchSpawnEnemyGameObject(ref NetworkObjectReference __result, Vector3 spawnPosition, float yRot, int enemyNumber, EnemyType enemyType = null)
        {
            if (!Plugin.Can("DefaultHealthAllMobs"))
            {
                if (__result.TryGet(out NetworkObject enemy))
                {
                    EnemyAI e = enemy.GetComponent<EnemyAI>();
                    if (e == null)
                    {
                        e = enemy.GetComponent<EnemyAICollisionDetect>().mainScript;
                    }
                    if (e != null && e.IsOwner)
                    {
                        if (Plugin.CanMob("BoomableAllMobs", ".Boomable", e.enemyType.enemyName))
                        {
                            e.enemyHP = Plugin.GetInt(".Health", e.enemyType.enemyName);
                        }
                        else
                        {
                            if (e.enemyType.canDie)
                            {
                                e.enemyHP = Plugin.GetInt(".Health", e.enemyType.enemyName);
                            }
                        }
                    }
                }
            }
        }

        public static void KillEnemyPatch(ref EnemyAI __instance, bool overrideDestroy = false)
        {
            if (!__instance.isEnemyDead && __instance.IsOwner) 
            {
                if (Plugin.CanMob("BoomableAllMobs", ".Boomable", __instance.enemyType.enemyName))
                {
                    if (__instance is NutcrackerEnemyAI)
                    {
                        NutcrackerEnemyAI ai = (NutcrackerEnemyAI) __instance;
                        DropItem(ai.transform.position, ai.gunPrefab, ai.gun.scrapValue, RoundManager.Instance);
                        ai.gun = null;
                    }
                    Plugin.Log.LogInfo($"Exploding {__instance.name}");
                    __instance.enemyType.canDie = true;
                    var enemyPos = __instance.transform.position;
                    if (Plugin.CanMob("ExplosionEffectAllMobs", ".Explodeable", __instance.enemyType.enemyName))
                    {
                        Object.Instantiate(Plugin.explosionPrefab, enemyPos, Quaternion.Euler(-90f, 0f, 0f),
                        RoundManager.Instance.mapPropsContainer.transform).SetActive(value: true);

                        HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
                    }
                }
            }
        }

        public static void HitEnemyLocalPatch(int force, Vector3 hitDirection, PlayerControllerB playerWhoHit,
            bool playHitSFX, EnemyAI __instance)
        {
            if (!__instance.isEnemyDead && __instance.IsOwner)
            {
                if (Plugin.CanMob("BoomableAllMobs", ".Boomable", __instance.enemyType.enemyName))
                {
                    Plugin.Log.LogInfo(
                    $"Enemy Hit: {__instance.enemyType.enemyName}, health: {__instance.enemyHP - force}, canDie: {__instance.enemyType.canDie}");
                    if (__instance.creatureAnimator != null)
                    {
                        __instance.creatureAnimator.SetTrigger(Damage);
                    }
                    __instance.enemyHP -= force;
                    if (__instance.enemyHP <= 0)
                    {
                        Plugin.Log.LogInfo($"{__instance.name} HP is {__instance.enemyHP}, killing");
                        __instance.KillEnemyOnOwnerClient(true);
                    }
                }
            }
        }

        public static bool ReplaceShotgunCode(ref ShotgunItem __instance, Vector3 shotgunPosition, Vector3 shotgunForward)
        {
            ShootGun(__instance, shotgunPosition, shotgunForward);
            return false;
        }

        static void DropItem(Vector3 position, GameObject itemPrefab, int scrapValue, RoundManager instance)
        {
            Transform scrapContainer = instance.spawnedScrapContainer;
            position += new Vector3(UnityEngine.Random.Range(-0.8f, 0.8f), 0f, UnityEngine.Random.Range(-0.8f, 0.8f));
            GameObject obj = UnityEngine.Object.Instantiate(itemPrefab, position, Quaternion.identity, scrapContainer);
            GrabbableObject component = obj.GetComponent<GrabbableObject>();
            component.transform.rotation = Quaternion.Euler(component.itemProperties.restingRotation);
            component.fallTime = 0f;
            int valueOfScrap = scrapValue;
            if (instance.scrapValueMultiplier > 1)
            {
                valueOfScrap = (int)(valueOfScrap * instance.scrapValueMultiplier);
            }
            component.itemProperties.isScrap = true;
            component.SetScrapValue(valueOfScrap);
            NetworkObject net = obj.GetComponent<NetworkObject>();
            net.Spawn();
            instance.SyncScrapValuesClientRpc(new List<NetworkObjectReference>() { net }.ToArray(), new List<int>() { valueOfScrap }.ToArray());
        }

        public static void CreateHealthConfigEntry(string mobName, ConfigDefinition originalDefinition = null)
        {
            if (!Plugin.Can("DefaultHealthAllMobs"))
            {
                if (!Plugin.Can("HealthAllMobs"))
                {
                    if (originalDefinition == null)
                    {
                        ConfigEntry<int> tempEntry;
                        if (mobName.Equals("BLOB"))
                        {
                            tempEntry = Plugin.Instance.Config.Bind("Mobs", // The section under which the option is shown
                                                 mobName + ".Health", // The key of the configuration option in the configuration file
                                                 5, // The default value
                                                 "The value of the mobs health.(Default Vanilla is 3 for most mobs)"); // Description
                        }
                        else if (mobName.Equals("CRAWLER"))
                        {
                            tempEntry = Plugin.Instance.Config.Bind("Mobs", // The section under which the option is shown
                                                 mobName + ".Health", // The key of the configuration option in the configuration file
                                                 5, // The default value
                                                 "The value of the mobs health.(Default Vanilla is 3 for most mobs)"); // Description
                        }
                        else if (mobName.Equals("FLOWERMAN"))
                        {
                            tempEntry = Plugin.Instance.Config.Bind("Mobs", // The section under which the option is shown
                                                 mobName + ".Health", // The key of the configuration option in the configuration file
                                                 7, // The default value
                                                 "The value of the mobs health.(Default Vanilla is 3 for most mobs)"); // Description
                        }
                        else if (mobName.Equals("FORESTGIANT"))
                        {
                            tempEntry = Plugin.Instance.Config.Bind("Mobs", // The section under which the option is shown
                                                 mobName + ".Health", // The key of the configuration option in the configuration file
                                                 11, // The default value
                                                 "The value of the mobs health.(Default Vanilla is 3 for most mobs)"); // Description
                        }
                        else if (mobName.Equals("JESTER"))
                        {
                            tempEntry = Plugin.Instance.Config.Bind("Mobs", // The section under which the option is shown
                                                 mobName + ".Health", // The key of the configuration option in the configuration file
                                                 9, // The default value
                                                 "The value of the mobs health.(Default Vanilla is 3 for most mobs)"); // Description
                        }
                        else if (mobName.Equals("MASKED"))
                        {
                            tempEntry = Plugin.Instance.Config.Bind("Mobs", // The section under which the option is shown
                                                 mobName + ".Health", // The key of the configuration option in the configuration file
                                                 5, // The default value
                                                 "The value of the mobs health.(Default Vanilla is 3 for most mobs)"); // Description
                        }
                        else if (mobName.Equals("MOUTHDOG"))
                        {
                            tempEntry = Plugin.Instance.Config.Bind("Mobs", // The section under which the option is shown
                                                 mobName + ".Health", // The key of the configuration option in the configuration file
                                                 11, // The default value
                                                 "The value of the mobs health.(Default Vanilla is 3 for most mobs)"); // Description
                        }
                        else if (mobName.Equals("PUFFER"))
                        {
                            tempEntry = Plugin.Instance.Config.Bind("Mobs", // The section under which the option is shown
                                                 mobName + ".Health", // The key of the configuration option in the configuration file
                                                 5, // The default value
                                                 "The value of the mobs health.(Default Vanilla is 3 for most mobs)"); // Description
                        }
                        else if (mobName.Equals("BUNKERSPIDER"))
                        {
                            tempEntry = Plugin.Instance.Config.Bind("Mobs", // The section under which the option is shown
                                                 mobName + ".Health", // The key of the configuration option in the configuration file
                                                 5, // The default value
                                                 "The value of the mobs health.(Default Vanilla is 3 for most mobs)"); // Description
                        }
                        else if (mobName.Equals("EARTHLEVIATHAN"))
                        {
                            tempEntry = Plugin.Instance.Config.Bind("Mobs", // The section under which the option is shown
                                                 mobName + ".Health", // The key of the configuration option in the configuration file
                                                 15, // The default value
                                                 "The value of the mobs health.(Default Vanilla is 3 for most mobs)"); // Description
                        }
                        else if (mobName.Equals("SPRING"))
                        {
                            tempEntry = Plugin.Instance.Config.Bind("Mobs", // The section under which the option is shown
                                                 mobName + ".Health", // The key of the configuration option in the configuration file
                                                 10, // The default value
                                                 "The value of the mobs health.(Default Vanilla is 3 for most mobs)"); // Description
                        }
                        else
                        {
                            tempEntry = Plugin.Instance.Config.Bind("Mobs", // The section under which the option is shown
                                                 mobName + ".Health", // The key of the configuration option in the configuration file
                                                 3, // The default value
                                                 "The value of the mobs health.(Default Vanilla is 3 for most mobs)"); // Description
                        }
                    }
                    else
                    {
                        if (mobName.Equals("BLOB"))
                        {
                            Plugin.Instance.Config[originalDefinition].SetSerializedValue("5");
                        }
                        else if (mobName.Equals("CRAWLER"))
                        {
                            Plugin.Instance.Config[originalDefinition].SetSerializedValue("5");
                        }
                        else if (mobName.Equals("FLOWERMAN"))
                        {
                            Plugin.Instance.Config[originalDefinition].SetSerializedValue("7");
                        }
                        else if (mobName.Equals("FORESTGIANT"))
                        {
                            Plugin.Instance.Config[originalDefinition].SetSerializedValue("11");
                        }
                        else if (mobName.Equals("JESTER"))
                        {
                            Plugin.Instance.Config[originalDefinition].SetSerializedValue("9");
                        }
                        else if (mobName.Equals("MASKED"))
                        {
                            Plugin.Instance.Config[originalDefinition].SetSerializedValue("5");
                        }
                        else if (mobName.Equals("MOUTHDOG"))
                        {
                            Plugin.Instance.Config[originalDefinition].SetSerializedValue("11");
                        }
                        else if (mobName.Equals("PUFFER"))
                        {
                            Plugin.Instance.Config[originalDefinition].SetSerializedValue("5");
                        }
                        else if (mobName.Equals("BUNKERSPIDER"))
                        {
                            Plugin.Instance.Config[originalDefinition].SetSerializedValue("5");
                        }
                        else if (mobName.Equals("EARTHLEVIATHAN"))
                        {
                            Plugin.Instance.Config[originalDefinition].SetSerializedValue("15");
                        }
                        else if (mobName.Equals("SPRING"))
                        {
                            Plugin.Instance.Config[originalDefinition].SetSerializedValue("10");
                        }
                        else
                        {
                            Plugin.Instance.Config[originalDefinition].SetSerializedValue("3");
                        }
                    }
                }
            }
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
            UnityEngine.Random.InitState(Plugin.ShotgunRandom.Next());
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
                        if (ReferenceEquals(hittable, gun.playerHeldBy)) continue; // self hit
                        EnemyAI ai = null;
                        if (hittable is EnemyAICollisionDetect detect) ai = detect.mainScript;
                        if (ai != null)
                        {
                            if (!playerFired) continue; // enemy hit enemy
                            if (ai.isEnemyDead || ai.enemyHP <= 0 || !ai.enemyType.canDie) continue; // skip dead things
                        }
                        if (hittable is PlayerControllerB) targets.Add(obj);
                        else if (ai != null) targets.Add(obj);
                        else if (playerFired) targets.Add(obj);
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
                //VisualiseShot(shotgunPosition, end);
            }

            // deal damage all at once - prevents piercing alive and reduces damage calls
            if (targets.Any())
            {
                targets.ForEach(t =>
                {
                    if (t != null)
                    {
                        if (t.GetComponent<PlayerControllerB>() != null)
                        {
                            PlayerControllerB player = t.GetComponent<PlayerControllerB>();
                            // grouping player damage also ensures strong hits (3+ pellets) ignore critical damage - 5 is always lethal rather than being critical
                            int damage = 20;
                            player.DamagePlayer(damage, true, true, CauseOfDeath.Gunshots, 0, false, shotgunForward);
                        }
                        else if (t.GetComponent<EnemyAICollisionDetect>() != null)
                        {
                            EnemyAICollisionDetect enemy = t.GetComponent<EnemyAICollisionDetect>();
                            int damage = 1;
                            if (!enemy.mainScript.isEnemyDead)
                            {
                                if (Plugin.CanMob("BoomableAllMobs", ".Boomable", enemy.mainScript.enemyType.enemyName))
                                {
                                    if (enemy.mainScript.creatureAnimator != null)
                                    {
                                        enemy.mainScript.creatureAnimator.SetTrigger(Animator.StringToHash("damage"));
                                    }
                                    Plugin.Log.LogInfo(
                                    $"Enemy Hit: {enemy.mainScript.enemyType.enemyName}, health: {enemy.mainScript.enemyHP - damage}, canDie: {enemy.mainScript.enemyType.canDie}");
                                    enemy.mainScript.enemyHP -= damage;
                                    if (enemy.mainScript.enemyHP <= 0 && enemy.mainScript.IsOwner)
                                    {
                                        enemy.mainScript.KillEnemyOnOwnerClient(true);
                                    }
                                }
                                else
                                {
                                    enemy.mainScript.HitEnemyOnLocalClient(damage);
                                }
                            }
                        }
                        else if (t.GetComponent<EnemyAI>() != null)
                        {
                            EnemyAI enemy = t.GetComponent<EnemyAI>();
                            int damage = 1;
                            if (Plugin.CanMob("BoomableAllMobs", ".Boomable", enemy.enemyType.enemyName))
                            {
                                if (enemy.creatureAnimator != null)
                                {
                                    enemy.creatureAnimator.SetTrigger(Animator.StringToHash("damage"));
                                }
                                enemy.enemyHP -= damage;
                                Plugin.Log.LogInfo(
                                $"Enemy Hit: {enemy.enemyType.enemyName}, health: {enemy.enemyHP - damage}, canDie: {enemy.enemyType.canDie}");
                                if (enemy.enemyHP <= 0 && enemy.IsOwner)
                                {
                                    enemy.KillEnemyOnOwnerClient(true);
                                }
                            }
                            else
                            {
                                enemy.HitEnemyOnLocalClient(damage);
                            }
                        }
                        else if (t.GetComponent<IHittable>() != null)
                        {
                            IHittable hit = t.GetComponent<IHittable>();
                            if (hit is EnemyAICollisionDetect)
                            {
                                EnemyAICollisionDetect enemy = (EnemyAICollisionDetect)hit;
                                int damage = 1;
                                if (Plugin.CanMob("BoomableAllMobs", ".Boomable", enemy.mainScript.enemyType.enemyName))
                                {
                                    if (enemy.mainScript.creatureAnimator != null)
                                    {
                                        enemy.mainScript.creatureAnimator.SetTrigger(Animator.StringToHash("damage"));
                                    }
                                    Plugin.Log.LogInfo(
                                    $"Enemy Hit: {enemy.mainScript.enemyType.enemyName}, health: {enemy.mainScript.enemyHP - damage}, canDie: {enemy.mainScript.enemyType.canDie}");
                                    enemy.mainScript.enemyHP -= damage;
                                    if (enemy.mainScript.enemyHP <= 0 && enemy.mainScript.IsOwner)
                                    {
                                        enemy.mainScript.KillEnemyOnOwnerClient(true);
                                    }
                                }
                                else
                                {
                                    enemy.mainScript.HitEnemyOnLocalClient(damage);
                                }
                            }
                            else if (hit is PlayerControllerB)
                            {
                                PlayerControllerB player = (PlayerControllerB)hit;
                                // grouping player damage also ensures strong hits (3+ pellets) ignore critical damage - 5 is always lethal rather than being critical
                                int damage = 33;
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

        public static System.Collections.IEnumerator DelayedEarsRinging(float effectSeverity)
        {
            yield return new WaitForSeconds(0.6f);
            SoundManager.Instance.earsRingingTimer = effectSeverity;
        }
    }
}