﻿using HarmonyLib;

namespace ComboSell
{
    internal class CompanyCounterPatch
    {
        public static ComboSettings settings;

        [HarmonyPatch(typeof(DepositItemsDesk), "SellItemsOnServer")]
        [HarmonyPostfix]
        private static void SellItemsOnServerPrefix(DepositItemsDesk __instance)
        {
            Plugin.Debug("SellItemsOnServerPrefix()");
            if (!__instance.IsServer)
            {
                return;
            }
            __instance.inSellingItemsAnimation = true;
            ComboPricer pricer = new ComboPricer(__instance.itemsOnCounter.ToArray(), settings);
            ComboResult result = pricer.processObjects();
            int totalValue = 0;
            foreach (ObjectCombo combo in result.multipleCombos)
            {
                totalValue += combo.totalValue;
            }
            foreach (ObjectCombo combo in result.setCombos)
            {
                totalValue += combo.totalValue;
            }
            foreach (GrabbableObject otherObject in result.otherObjects)
            {
                totalValue += otherObject.scrapValue;
            }
            totalValue = (int)(totalValue * StartOfRound.Instance.companyBuyingRate);
            Terminal terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
            terminal.groupCredits += totalValue;
            __instance.SellItemsClientRpc(totalValue, terminal.groupCredits, __instance.itemsOnCounterAmount, StartOfRound.Instance.companyBuyingRate);
            __instance.SellAndDisplayItemProfits(totalValue, terminal.groupCredits);
        }

        [HarmonyPatch(typeof(DepositItemsDesk), "SellAndDisplayItemProfits")]
        [HarmonyPostfix]
        private static void SellAndDisplayItemProfitsPrefix(ref int profit, ref int newGroupCredits, DepositItemsDesk __instance)
        {
            Plugin.Debug($"SellAndDisplayItemProfitsPrefix({profit}, {newGroupCredits})");
            UnityEngine.Object.FindObjectOfType<Terminal>().groupCredits = newGroupCredits;
            StartOfRound.Instance.gameStats.scrapValueCollected += profit;
            TimeOfDay.Instance.quotaFulfilled += profit;
            GrabbableObject[] componentsInChildren = __instance.deskObjectsContainer.GetComponentsInChildren<GrabbableObject>();
            if (__instance.acceptItemsCoroutine != null)
            {
                __instance.StopCoroutine(__instance.acceptItemsCoroutine);
            }
            // delayedAcceptanceOfItems calls DisplayCreditsEarning with profit->creditsEarned and componentsInChildren->objectsSold
            __instance.acceptItemsCoroutine = __instance.StartCoroutine(__instance.delayedAcceptanceOfItems(profit, componentsInChildren, newGroupCredits));
            __instance.CheckAllPlayersSoldItemsServerRpc();
        }

        [HarmonyPatch(typeof(HUDManager), "DisplayCreditsEarning")]
        [HarmonyPostfix]
        private static void DisplayCreditsEarningPrefix(ref int creditsEarned, ref GrabbableObject[] objectsSold, ref int newGroupCredits, HUDManager __instance)
        {
            Plugin.Debug($"DisplayCreditsEarningPrefix({creditsEarned}, {objectsSold.Length}, {newGroupCredits})");
            // Debug.Log(string.Format("Earned {0}; sold {1} items; new credits amount: {2}", creditsEarned, objectsSold.Length, newGroupCredits));
            string text = "";
            ComboPricer pricer = new ComboPricer(objectsSold, settings);
            ComboResult result = pricer.processObjects();
            if (result.multipleCombos.Length > 0)
                text += "Multiplier Combos \n";
            foreach (ObjectCombo combo in result.multipleCombos)
            {
                int value = combo.totalValue;
                text += string.Format("{0} ({1}) : {2} (x{3}) \n", combo.itemNames, combo.name, value, combo.multiplier);
            }
            if (result.setCombos.Length > 0)
                text += "Set Combos \n";
            foreach (ObjectCombo combo in result.setCombos)
            {
                int value = combo.totalValue;
                text += string.Format("{0} ({1}) : {2} (x{3}) \n", combo.itemNames, combo.name, value, combo.multiplier);
            }
            if (result.otherObjects.Length > 0)
                text += "Regular Sales \n";
            foreach (GrabbableObject otherObject in result.otherObjects)
            {
                int value = 0;
                int count = 0;
                foreach (GrabbableObject subObject in result.otherObjects)
                {
                    if (subObject.itemProperties == otherObject.itemProperties)
                    {
                        value += subObject.scrapValue;
                        count++;
                    }
                }
                text += string.Format("{0} (x{1}) : {2} \n", otherObject.itemProperties.itemName, count, value);
            }
            __instance.moneyRewardsListText.text = text;
            __instance.moneyRewardsTotalText.text = string.Format("TOTAL: ${0}", creditsEarned);
            __instance.moneyRewardsAnimator.SetTrigger("showRewards");
            __instance.rewardsScrollbar.value = 1f;
            if (text.Split('\n').Length > 8)
            {
                if (__instance.scrollRewardTextCoroutine != null)
                {
                    __instance.StopCoroutine(__instance.scrollRewardTextCoroutine);
                }
                __instance.scrollRewardTextCoroutine = __instance.StartCoroutine(__instance.scrollRewardsListText());
            }
        }
    }
}