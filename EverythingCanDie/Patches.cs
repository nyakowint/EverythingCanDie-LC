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
                "ExplosionEffectAllMobs", // The key of the configuration option in the configuration file
                                             true, // The default value
                                             "If this is set to true then explosion effect stays and removes the body otherwise when false all explosions on death will not appear and the body will appear."); // Description
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
                if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", mobName + ".Health")))
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
                "ExplosionEffectAllMobs", // The key of the configuration option in the configuration file
                                             true, // The default value
                                             "If this is set to true then explosion effect stays and removes the body otherwise when false all explosions on death will not appear and the body will appear."); // Description
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
                if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", mobName + ".Health")))
                {
                    ConfigEntry<bool> tempEntry = Plugin.Instance.Config.Bind("Mobs", // The section under which the option is shown
                                             mobName + ".Explodeable", // The key of the configuration option in the configuration file
                                             true, // The default value
                                             "The value of whether to spawn an explosion effect(Default on)"); // Description
                }
                if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", mobName + ".Health")))
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

        public static bool DoAIIntervalPatch(ref EnemyAI __instance)
        {
            if (__instance == null)
            {
                return false;
            }
            if (__instance.GetComponent<Plugin.KilledEnemy>() != null)
            {
                return false;
            }
            return true;
        }

        public static bool OnCollideWithPlayerPatch(ref EnemyAI __instance, Collider other)
        {
            if (__instance == null)
            {
                return false;
            }
            if (__instance.GetComponent<Plugin.KilledEnemy>() != null)
            {
                return false;
            }
        }

        public static void KillEnemyPatch(ref EnemyAI __instance, bool destroy = false)
        {
            if (!(__instance == null))
            {
                if (!__instance.isEnemyDead && __instance.IsOwner)
                {
                    EnemyType type = __instance.enemyType;
                    string name = Plugin.RemoveInvalidCharacters(type.enemyName);
                    if (Plugin.CanMob("UnimmortalAllMobs", ".Unimmortal", name))
                    {
                        Plugin.Log.LogInfo($"Exploding {name}");
                        __instance.enemyType.canDie = true;
                        var enemyPos = __instance.transform.position;
                        if (Plugin.CanMob("ExplosionEffectAllMobs", ".Explodeable", name))
                        {
                            Object.Instantiate(Plugin.explosionPrefab, enemyPos, Quaternion.Euler(-90f, 0f, 0f),
                            RoundManager.Instance.mapPropsContainer.transform).SetActive(value: true);

                            HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
                        }
                        __instance.isEnemyDead = true;
                        __instance.gameObject.AddComponent<Plugin.KilledEnemy>();
                        ScanNodeProperties componentInChildren = __instance.gameObject.GetComponentInChildren<ScanNodeProperties>();
                        if (componentInChildren != null && (bool)componentInChildren.gameObject.GetComponent<Collider>())
                        {
                            componentInChildren.gameObject.GetComponent<Collider>().enabled = false;
                        }
                        if (__instance.creatureVoice != null)
                        {
                            if (__instance.dieSFX != null)
                            {
                                __instance.creatureVoice.PlayOneShot(__instance.dieSFX);
                            }
                        }
                        if (__instance is NutcrackerEnemyAI)
                        {
                            NutcrackerEnemyAI ai = (NutcrackerEnemyAI)__instance;
                            DropItem(enemyPos, ai.gunPrefab, ai.gun.scrapValue, RoundManager.Instance);
                            ai.targetTorsoDegrees = 0;
                            ai.StopInspection();
                            System.Type nut = typeof(EnemyAI);
                            MethodInfo sReload_method = nut.GetMethod("StopReloading", BindingFlags.NonPublic | BindingFlags.Instance);
                            sReload_method.Invoke(__instance, null);
                            Vector3 position = enemyPos + Vector3.up * 0.6f;
                            DropItem(position, ai.shotgunShellPrefab, 20, RoundManager.Instance);
                            DropItem(position, ai.shotgunShellPrefab, 20, RoundManager.Instance);
                            ai.creatureVoice.Stop();
                            ai.torsoTurnAudio.Stop();
                            ai.gunPrefab.SetActive(false);
                            ai.gun.gameObject.SetActive(false);
                        }
                        try
                        {
                            if (__instance.creatureAnimator != null)
                            {
                                __instance.creatureAnimator.SetBool("Stunned", value: false);
                                __instance.creatureAnimator.SetBool("stunned", value: false);
                                __instance.creatureAnimator.SetBool("stun", value: false);
                                __instance.creatureAnimator.SetTrigger("KillEnemy");
                                __instance.creatureAnimator.SetBool("Dead", value: true);
                            }
                        }
                        catch (Exception arg)
                        {
                            Debug.LogError($"enemy did not have bool in animator in KillEnemy, error returned; {arg}");
                        }
                        __instance.CancelSpecialAnimationWithPlayer();
                        System.Type typ = typeof(EnemyAI);
                        MethodInfo target_method = typ.GetMethod("SubtractFromPowerLevel", BindingFlags.NonPublic | BindingFlags.Instance);
                        target_method.Invoke(__instance, null);
                        if (__instance.agent != null)
                        {
                            __instance.agent.enabled = false;
                        }
                    }
                }
            }
        }

        public static void HitEnemyLocalPatch(ref EnemyAI __instance, int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false)
        {
            if (!__instance.isEnemyDead)
            {
                if (Plugin.CanMob("UnimmortalAllMobs", ".Unimmortal", __instance.enemyType.enemyName))
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
                        __instance.KillEnemyOnOwnerClient();
                    }
                }
            }
        }

        public static bool ReplaceShotgunCode(ref ShotgunItem __instance, Vector3 shotgunPosition, Vector3 shotgunForward)
        {
            ShootGun(__instance, shotgunPosition, shotgunForward);
            return false;
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

        public static System.Collections.IEnumerator DelayedItemDrop(float timeTilDrop, NutcrackerEnemyAI enemy)
        {
            Transform enemyTransform = enemy.transform;
            Vector3 pos = enemyTransform.position;
            if (Physics.SphereCast(pos, 3, enemyTransform.up, out RaycastHit hit))
            {

            }
            yield return new WaitForSeconds(timeTilDrop);
            enemy.DropGunServerRpc(pos);
        }

        public static System.Collections.IEnumerator DelayedEarsRinging(float effectSeverity)
        {
            yield return new WaitForSeconds(0.6f);
            SoundManager.Instance.earsRingingTimer = effectSeverity;
        }
    }
}