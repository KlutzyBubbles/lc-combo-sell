using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace ComboSell
{
    internal class DebugPatch
    {
        public static System.Random ScrapRandom = new System.Random();

        [HarmonyPatch(typeof(RoundManager), "InitializeRandomNumberGenerators")]
        [HarmonyPrefix]
        public static void InitializeRandomNumberGeneratorsPrefix(RoundManager __instance)
        {
            ScrapRandom = new System.Random(__instance.playersManager.randomMapSeed + 69);
        }

        [HarmonyPatch(typeof(QuickMenuManager), "Debug_SpawnItem")]
        [HarmonyPostfix]
        public static void Debug_SpawnItemPostfix(QuickMenuManager __instance)
        {
            if (!Application.isEditor || !NetworkManager.Singleton.IsConnectedClient || !NetworkManager.Singleton.IsServer)
                return;
            GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(StartOfRound.Instance.allItemsList.itemsList[__instance.itemToSpawnId].spawnPrefab, __instance.debugEnemySpawnPositions[3].position, Quaternion.identity, StartOfRound.Instance.propsContainer);
            gameObject.GetComponent<GrabbableObject>().fallTime = 0.0f;
            gameObject.GetComponent<GrabbableObject>().scrapValue = ScrapRandom.Next(1, 100);
            gameObject.GetComponent<NetworkObject>().Spawn();
        }

    }
}
