using HarmonyLib;
using Unity.Netcode;
using System;
using System.Linq;

namespace ComboSell
{
    internal class DebugPatch
    {
        public static Random ScrapRandom = new Random();

        [HarmonyPatch(typeof(RoundManager), "InitializeRandomNumberGenerators")]
        [HarmonyPrefix]
        public static void InitializeRandomNumberGeneratorsPrefix(RoundManager __instance)
        {
            ScrapRandom = new Random(__instance.playersManager.randomMapSeed + 69);
        }

        [HarmonyPatch(typeof(QuickMenuManager), "Debug_SpawnItem")]
        [HarmonyPostfix]
        public static void Debug_SpawnItemPostfix(QuickMenuManager __instance)
        {
            if (!UnityEngine.Application.isEditor || !NetworkManager.Singleton.IsConnectedClient || !NetworkManager.Singleton.IsServer)
                return;
            Plugin.Debug(string.Join(", ", StartOfRound.Instance.allItemsList.itemsList.ToList().Select(item => item.name)));
            UnityEngine.GameObject gameObject = UnityEngine.Object.Instantiate<UnityEngine.GameObject>(StartOfRound.Instance.allItemsList.itemsList[__instance.itemToSpawnId].spawnPrefab, __instance.debugEnemySpawnPositions[3].position, UnityEngine.Quaternion.identity, StartOfRound.Instance.propsContainer);
            gameObject.GetComponent<GrabbableObject>().fallTime = 0.0f;
            gameObject.GetComponent<GrabbableObject>().scrapValue = ScrapRandom.Next(1, 100);
            gameObject.GetComponent<NetworkObject>().Spawn();
        }

    }
}
