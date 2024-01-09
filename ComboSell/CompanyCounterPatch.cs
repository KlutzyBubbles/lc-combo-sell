using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace ComboSell
{
    internal class CompanyCounterPatch
    {
        public static ComboSettings settings;

        [HarmonyPatch(typeof(DepositItemsDesk), "SellItemsOnServer")]
        [HarmonyPostfix]
        private static void SellItemsOnServerPostfix(DepositItemsDesk __instance)
        {
            try
            {
                Plugin.Debug("SellItemsOnServerPostfix()");
                if (!__instance.IsServer)
                {
                    return;
                }
                __instance.inSellingItemsAnimation = true;
                ComboPricer pricer = new ComboPricer(ref __instance.itemsOnCounter, settings);
                ComboResult result = pricer.processObjects();
                Plugin.Debug($"Result info:");
                Plugin.Debug($"multipleCombos ({result.multipleCombos.Count})[{string.Join(", ", result.multipleCombos)}]");
                Plugin.Debug($"setCombos ({result.setCombos.Count})[{string.Join(", ", result.setCombos)}]");
                Plugin.Debug($"otherObjects ({result.otherObjects.Count})[{string.Join(", ", result.otherObjects)}]");
                int totalValue = 0;
                Plugin.Debug($"totalValue start {totalValue}");
                foreach (ObjectCombo combo in result.multipleCombos)
                {
                    Plugin.Debug($"combo {combo.name}, totalValue {combo.totalValue}, multiplier {combo.multiplier}, type {combo.type}, items [{combo.itemNames}]");
                    totalValue += combo.totalValue;
                }
                Plugin.Debug($"totalValue multiple {totalValue}");
                foreach (ObjectCombo combo in result.setCombos)
                {
                    Plugin.Debug($"combo {combo.name}, totalValue {combo.totalValue}, multiplier {combo.multiplier}, type {combo.type}, items [{combo.itemNames}]");
                    totalValue += combo.totalValue;
                }
                Plugin.Debug($"totalValue set {totalValue}");
                foreach (GrabbableObject otherObject in result.otherObjects)
                {
                    Plugin.Debug($"other object {otherObject.itemProperties.name}");
                    totalValue += otherObject.scrapValue;
                }
                Plugin.Debug($"totalValue other {totalValue}");
                totalValue = (int)(totalValue * StartOfRound.Instance.companyBuyingRate);
                Terminal terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
                terminal.groupCredits += totalValue;
                __instance.SellItemsClientRpc(totalValue, terminal.groupCredits, __instance.itemsOnCounterAmount, StartOfRound.Instance.companyBuyingRate);
                __instance.SellAndDisplayItemProfits(totalValue, terminal.groupCredits);
            }
            catch (Exception e)
            {
                Plugin.StaticLogger.LogError($"Error processing DisplayCreditsEarningPostfix, please report this with debug logs.");
                Plugin.StaticLogger.LogError(e);
            }
        }

        /*
        [HarmonyPatch(typeof(DepositItemsDesk), "SellAndDisplayItemProfits")]
        [HarmonyPostfix]
        private static void SellAndDisplayItemProfitsPostfix(ref int profit, ref int newGroupCredits, DepositItemsDesk __instance)
        {
            Plugin.Debug($"SellAndDisplayItemProfitsPostfix({profit}, {newGroupCredits})");
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
        */

        [HarmonyPatch(typeof(HUDManager), "DisplayCreditsEarning")]
        [HarmonyPostfix]
        private static void DisplayCreditsEarningPostfix(ref int creditsEarned, ref List<GrabbableObject> objectsSold, ref int newGroupCredits, HUDManager __instance)
        {
            try
            {
                Plugin.Debug($"DisplayCreditsEarningPostfix({creditsEarned}, [{string.Join(", ", objectsSold.ToList().Select(obj => obj.itemProperties.name))}], {newGroupCredits})");
                // Debug.Log(string.Format("Earned {0}; sold {1} items; new credits amount: {2}", creditsEarned, objectsSold.Length, newGroupCredits));
                string text = "";
                ComboPricer pricer = new ComboPricer(ref objectsSold, settings);
                ComboResult result = pricer.processObjects();
                Plugin.Debug($"Result info:");
                Plugin.Debug($"multipleCombos ({result.multipleCombos.Count})[{string.Join(", ", result.multipleCombos)}]");
                Plugin.Debug($"setCombos ({result.setCombos.Count})[{string.Join(", ", result.setCombos)}]");
                Plugin.Debug($"otherObjects ({result.otherObjects.Count})[{string.Join(", ", result.otherObjects)}]");
                if (result.multipleCombos.Count > 0)
                    text += "Multiplier Combos \n";
                foreach (ObjectCombo combo in result.multipleCombos)
                {
                    Plugin.Debug($"combo {combo.name}, totalValue {combo.totalValue}, multiplier {combo.multiplier}, type {combo.type}, items [{combo.itemNames}]");
                    int value = combo.totalValue;
                    text += string.Format("{0} ({1}) : {2} (x{3}) \n", combo.uniqueItemNames, combo.name, value, combo.multiplier);
                }
                if (result.setCombos.Count > 0)
                    text += "Set Combos \n";
                foreach (ObjectCombo combo in result.setCombos)
                {
                    Plugin.Debug($"combo {combo.name}, totalValue {combo.totalValue}, multiplier {combo.multiplier}, type {combo.type}, items [{combo.itemNames}]");
                    int value = combo.totalValue;
                    text += string.Format("{0} ({1}) : {2} (x{3}) \n", combo.itemNames, combo.name, value, combo.multiplier);
                }
                if (result.otherObjects.Count > 0)
                    text += "Regular Sales \n";
                List<Item> source = new List<Item>();
                for (int index = 0; index < objectsSold.Count; ++index)
                    source.Add(objectsSold[index].itemProperties);
                Item[] uniques = source.Distinct().ToArray();
                foreach (Item uniqueObject in uniques)
                {
                    int value = 0;
                    int count = 0;
                    foreach (GrabbableObject otherObject in result.otherObjects)
                    {
                        if (otherObject.itemProperties == uniqueObject)
                        {
                            value += otherObject.scrapValue;
                            count++;
                        }
                    }
                    text += string.Format("{0} (x{1}) : {2} \n", uniqueObject.itemName, count, value);
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
            catch (Exception e)
            {
                Plugin.StaticLogger.LogError($"Error processing DisplayCreditsEarningPostfix, please report this with debug logs.");
                Plugin.StaticLogger.LogError(e);
            }
        }
    }
}
