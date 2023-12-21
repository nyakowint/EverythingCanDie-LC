using GameNetcodeStuff;
using UnityEngine;
using Object = UnityEngine.Object;

// ReSharper disable InconsistentNaming

namespace EverythingCanDie
{
    public class Patches
    {
        private static readonly int Damage = Animator.StringToHash("damage");

        public static bool KillEnemyPatch(EnemyAI __instance, bool overrideDestroy = false)
        {
            if (__instance.isEnemyDead) return false;
            Plugin.Log.LogInfo($"Exploding {__instance.name}");
            __instance.enemyType.canDie = true;
            var enemyPos = __instance.transform.position;
            Object.Instantiate(StartOfRound.Instance.explosionPrefab, enemyPos, Quaternion.Euler(-90f, 0f, 0f),
                RoundManager.Instance.mapPropsContainer.transform).SetActive(value: true);
            HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
            return true;
        }

        public static void HitEnemyLocalPatch(int force, Vector3 hitDirection, PlayerControllerB playerWhoHit,
            bool playHitSFX, EnemyAI __instance)
        {
            if (__instance.isEnemyDead) return;
            Plugin.Log.LogInfo(
                $"Enemy Hit: {__instance.enemyType.enemyName}, health: {__instance.enemyHP}, canDie: {__instance.enemyType.canDie}");
            __instance.creatureAnimator.SetTrigger(Damage);
            __instance.enemyHP -= force;
            if (__instance.enemyHP > 0 || !__instance.IsOwner) return;
            Plugin.Log.LogInfo($"{__instance.name} HP is {__instance.enemyHP}, killing");
            __instance.KillEnemyOnOwnerClient(true);
        }
    }
}