using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ComboSell
{
    internal class CompanyCounterPatch
    {
        public static ComboSettings settings;

        [HarmonyPatch(typeof(DepositItemsDesk), "SellItemsOnServer")]
        [HarmonyPrefix]
        private static bool SellItemsOnServerPrefix(DepositItemsDesk __instance)
        {
            try
            {
                Plugin.Debug("SellItemsOnServerPrefix()");
                if (!__instance.IsServer)
                {
                    return false;
                }
                __instance.inSellingItemsAnimation = true;
                Plugin.Debug($"__instance.itemsOnCounter ({__instance.itemsOnCounter.Count})[{string.Join(", ", __instance.itemsOnCounter.ToList().Select(obj => obj.itemProperties.name))}]");
                GrabbableObject[] componentsInChildren = __instance.deskObjectsContainer.GetComponentsInChildren<GrabbableObject>();
                Plugin.Debug($"componentsInChildren ({componentsInChildren.Length})[{string.Join(", ", componentsInChildren.ToList().Select(obj => obj.itemProperties.name))}]");
                List<GrabbableObject> itemsToCalc2 = componentsInChildren.Where(obj => obj.itemProperties.isScrap).ToList();
                Plugin.Debug($"itemsToCalc2 ({itemsToCalc2.Count})[{string.Join(", ", itemsToCalc2.ToList().Select(obj => obj.itemProperties.name))}]");
                List<GrabbableObject> itemsToCalc = __instance.itemsOnCounter.Where(obj => obj.itemProperties.isScrap).ToList();
                Plugin.Debug($"itemsToCalc ({itemsToCalc.Count})[{string.Join(", ", itemsToCalc.ToList().Select(obj => obj.itemProperties.name))}]");
                ComboPricer pricer = new ComboPricer(ref itemsToCalc2, settings);
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
                    Plugin.Debug($"other object {otherObject.itemProperties.name}, scrapValue {otherObject.scrapValue}");
                    totalValue += otherObject.scrapValue;
                }
                Plugin.Debug($"totalValue other {totalValue}");
                totalValue = (int)(totalValue * StartOfRound.Instance.companyBuyingRate);
                Terminal terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
                terminal.groupCredits += totalValue;
                __instance.SellItemsClientRpc(totalValue, terminal.groupCredits, __instance.itemsOnCounterAmount, StartOfRound.Instance.companyBuyingRate);
                __instance.SellAndDisplayItemProfits(totalValue, terminal.groupCredits);
                return false;
            }
            catch (Exception e)
            {
                Plugin.StaticLogger.LogError($"Error processing SellItemsOnServerPostfix, please report this with debug logs.");
                Plugin.StaticLogger.LogError(e);
            }
            return true;
        }

        [HarmonyPatch(typeof(HUDManager), "DisplayCreditsEarning")]
        [HarmonyPostfix]
        private static void DisplayCreditsEarningPostfix(ref int creditsEarned, ref GrabbableObject[] objectsSold, ref int newGroupCredits, HUDManager __instance)
        {
            try
            {
                Plugin.Debug($"DisplayCreditsEarningPostfix({creditsEarned}, [{string.Join(", ", objectsSold.ToList().Select(obj => obj.itemProperties.name))}], {newGroupCredits})");
                string text = "";
                List<GrabbableObject> objects = objectsSold.Where(obj => obj.itemProperties.isScrap).ToList();
                ComboPricer pricer = new ComboPricer(ref objects, settings);
                ComboResult result = pricer.processObjects();
                Plugin.Debug($"Result info:");
                Plugin.Debug($"multipleCombos ({result.multipleCombos.Count})[{string.Join(", ", result.multipleCombos)}]");
                Plugin.Debug($"setCombos ({result.setCombos.Count})[{string.Join(", ", result.setCombos)}]");
                Plugin.Debug($"otherObjects ({result.otherObjects.Count})[{string.Join(", ", result.otherObjects.ToList().Select(obj => obj.itemProperties.name))}]");
                if (result.multipleCombos.Count > 0)
                    text += "Multiplier Combos \n";
                Plugin.Debug($"Processing text for multipleCombos");
                foreach (ObjectCombo combo in result.multipleCombos)
                {
                    Plugin.Debug($"combo {combo.name}, totalValue {combo.totalValue}, multiplier {combo.multiplier}, type {combo.type}, items [{combo.itemNames}]");
                    int value = combo.totalValue;
                    text += string.Format("{0} ({1}) : {2} (x{3}) \n", combo.uniqueItemNames, combo.name, value, combo.multiplier);
                }
                if (result.setCombos.Count > 0)
                    text += "Set Combos \n";
                Plugin.Debug($"Processing text for setCombos");
                foreach (ObjectCombo combo in result.setCombos)
                {
                    Plugin.Debug($"combo {combo.name}, totalValue {combo.totalValue}, multiplier {combo.multiplier}, type {combo.type}, items [{combo.itemNames}]");
                    int value = combo.totalValue;
                    text += string.Format("{0} ({1}) : {2} (x{3}) \n", combo.itemNames, combo.name, value, combo.multiplier);
                }
                if (result.otherObjects.Count > 0)
                    text += "Regular Sales \n";
                Plugin.Debug($"Processing text for otherObjects");
                Item[] uniques = result.otherObjects.ToList().Select(obj => obj.itemProperties).Distinct().ToArray();
                foreach (Item uniqueObject in uniques)
                {
                    Plugin.Debug($"From uniques {uniqueObject.name}");
                    int value = 0;
                    int count = 0;
                    foreach (GrabbableObject otherObject in result.otherObjects)
                    {
                        Plugin.Debug($"From otherObjects {otherObject.itemProperties.name}");
                        if (otherObject.itemProperties == uniqueObject)
                        {
                            Plugin.Debug($"Adding scrap value for {uniqueObject.name} of value {otherObject.scrapValue}");
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
                    Plugin.Debug($"Output text is greater than 8 lines");
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
