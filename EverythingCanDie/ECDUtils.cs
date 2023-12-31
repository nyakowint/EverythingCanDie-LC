using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace EverythingCanDie
{
    internal class ECDUtils
    {
        public const float range = 30f;

        static readonly int PLAYER_HIT_MASK = StartOfRound.Instance.collidersRoomMaskDefaultAndPlayers | 2621448; //2621448 = enemy mask
        static readonly int ENEMY_HIT_MASK = StartOfRound.Instance.collidersRoomMaskDefaultAndPlayers;

        public static System.Random ShotgunRandom = new System.Random(0);
        public static int numTightPellets = 3;
        public static float tightPelletAngle = 2.5f;
        public static int numLoosePellets = 7;
        public static float loosePelletAngle = 10f;

        private static System.Collections.IEnumerator DelayedEarsRinging(float effectSeverity)
        {
            yield return new WaitForSeconds(0.6f);
            SoundManager.Instance.earsRingingTimer = effectSeverity;
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
            var vectorList = new Vector3[numTightPellets + numLoosePellets];
            var oldRandomState = UnityEngine.Random.state;
            UnityEngine.Random.InitState(ShotgunRandom.Next());
            for (int i = 0; i < numTightPellets + numLoosePellets; i++)
            {
                float variance = (i < numTightPellets) ? tightPelletAngle : loosePelletAngle;
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
                RaycastHit[] hits = Physics.RaycastAll(ray, range, playerFired ? PLAYER_HIT_MASK : ENEMY_HIT_MASK, QueryTriggerInteraction.Collide);
                Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));
                Vector3 end = shotgunPosition + vect * range;
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
                            if (playerFired && !ai.isEnemyDead && ai.enemyHP > 0 && ai.enemyType.canDie)
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
            }

            // deal damage all at once - prevents piercing alive and reduces damage calls
            targets.ForEach(t => {
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
                            if (enemy.mainScript.creatureAnimator != null)
                            {
                                enemy.mainScript.creatureAnimator.SetTrigger(Animator.StringToHash("damage"));
                            }
                            enemy.mainScript.enemyHP -= damage;
                            if (!(enemy.mainScript.enemyHP > 0) || enemy.mainScript.IsOwner)
                            {
                                enemy.mainScript.KillEnemyOnOwnerClient(true);
                            }
                        }
                    }
                    else if (t.GetComponent<EnemyAI>() != null)
                    {
                        EnemyAI enemy = t.GetComponent<EnemyAI>();
                        int damage = 1;
                        if (!enemy.isEnemyDead)
                        {
                            if (enemy.creatureAnimator != null)
                            {
                                enemy.creatureAnimator.SetTrigger(Animator.StringToHash("damage"));
                            }
                            enemy.enemyHP -= damage;
                            if (!(enemy.enemyHP > 0) || enemy.IsOwner)
                            {
                                enemy.KillEnemyOnOwnerClient(true);
                            }
                        }
                    }
                    else if (t.GetComponent<IHittable>() != null)
                    {
                        IHittable hit = t.GetComponent<IHittable>();
                        if (hit is EnemyAICollisionDetect)
                        {
                            EnemyAICollisionDetect enemy = (EnemyAICollisionDetect)hit;
                            int damage = 1;
                            if (!enemy.mainScript.isEnemyDead)
                            {
                                if (enemy.mainScript.creatureAnimator != null)
                                {
                                    enemy.mainScript.creatureAnimator.SetTrigger(Animator.StringToHash("damage"));
                                }
                                enemy.mainScript.enemyHP -= damage;
                                if (!(enemy.mainScript.enemyHP > 0) || enemy.mainScript.IsOwner)
                                {
                                    enemy.mainScript.KillEnemyOnOwnerClient(true);
                                }
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

            ray = new Ray(shotgunPosition, shotgunForward);
            if (Physics.Raycast(ray, out RaycastHit hitInfo, range, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
            {
                gun.gunBulletsRicochetAudio.transform.position = ray.GetPoint(hitInfo.distance - 0.5f);
                gun.gunBulletsRicochetAudio.Play();
            }
        }
    }
}