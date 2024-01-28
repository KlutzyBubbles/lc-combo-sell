using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ComboSell
{
    [Serializable]
    internal class ComboSettings
    {
        public static readonly string[] ItemNames = {
            "Binoculars", "Boombox", "CardboardBox", "Flashlight", "Jetpack", "Key", "LockPicker", "LungApparatus", "MapDevice", "ProFlashlight", "Shovel",
            "StunGrenade", "ExtensionLadder", "TZPInhalant", "WalkieTalkie", "ZapGun", "7Ball", "Airhorn", "Bell", "BigBolt", "BottleBin", "Brush", "Candy",
            "CashRegister", "ChemicalJug", "ClownHorn", "Cog1", "Dentures", "DustPan", "EggBeater", "EnginePart1", "FancyCup", "FancyLamp", "FancyPainting",
            "FishTestProp", "FlashLaserPointer", "GoldBar", "Hairdryer", "MagnifyingGlass", "MetalSheet", "MoldPan", "Mug", "PerfumeBottle", "Phone", "PickleJar",
            "PillBottle", "Remote", "Ring", "RobotToy", "RubberDuck", "SodaCanRed", "SteeringWheel", "StopSign", "TeaKettle", "Toothpaste", "ToyCube","RedLocustHive",
            "RadarBooster", "YieldSign", "Shotgun", "GunAmmo", "SprayPaint", "DiyFlashbang", "GiftBox", "Flask", "TragedyMask", "ComedyMask", "WhoopieCushion"
        };

        public bool removeUnknownNames = false;
        public bool multiplesFirst = true;

        public string[] includeMultiples = [];
        public string[] excludeMultiples = [];

        public int maxMultiple = 5;
        public int minMultiple = 2;
        public float defaultMultipleMultiplier = 0.2f;
        public float defaultSetMultiplier = 0.2f;

        public Dictionary<int, float> multipleMultipliers = new Dictionary<int, float>();

        public Dictionary<string, SetMultiplier> setMultipliers = new Dictionary<string, SetMultiplier>();

        public ComboSettings() {}
        public ComboSettings(bool removeUnknownNames)
        {
            this.removeUnknownNames = removeUnknownNames;
        }

        public float getMultipleMultiplier(int amount)
        {
            Plugin.Debug($"getMultipleMultiplier({amount})");
            if (multipleMultipliers.ContainsKey(amount))
            {
                Plugin.Debug($"multipleMultipliers contians the key, returning {multipleMultipliers[amount]}");
                return multipleMultipliers[amount];
            }
            float multiplier = 1 + (defaultMultipleMultiplier * (amount - 1));
            Plugin.Debug($"multipleMultipliers doesnt contians the key, returning {multiplier}");
            return multiplier;
        }
        public float getSetMultiplier(string setName, int fallbackAmount)
        {
            Plugin.Debug($"getMultipleMultiplier({setName})");
            if (setMultipliers.ContainsKey(setName))
            {
                Plugin.Debug($"setMultipliers contians the key, returning {setMultipliers[setName]}");
                return setMultipliers[setName].multiplier;
            }
            float multiplier = 1 + (defaultSetMultiplier * (fallbackAmount - 1));
            Plugin.Debug($"setMultipliers doesnt contians the key, returning {multiplier}");
            return multiplier;
        }

        public void standardizeValues()
        {
            Plugin.Debug($"standardizeValues()");
            Plugin.Debug($"Valid item names: {string.Join(", ", ItemNames)}");
            standardizeItemList(ref includeMultiples, ItemNames, "includeMultiples");
            Plugin.Debug($"includeMultiples: [{String.Join(", ", includeMultiples)}]");
            standardizeItemList(ref excludeMultiples, ItemNames, "includeMultiples");
            Plugin.Debug($"includeMultiples: [{String.Join(", ", includeMultiples)}]");
            Plugin.Debug($"Checking minMultiple");
            if (minMultiple < 1)
            {
                Plugin.StaticLogger.LogWarning($"minMultiple ({minMultiple}) cannot be lower than 1, defaulting to 1");
                minMultiple = 1;
            }
            Plugin.Debug($"Checking maxMultiple");
            if (maxMultiple < minMultiple)
            {
                Plugin.StaticLogger.LogWarning($"maxMultiple ({maxMultiple}) cannot be lower than minMultiple ({minMultiple}), defaulting to minMultiple ({minMultiple})");
                maxMultiple = minMultiple;
            }
            Plugin.Debug($"Checking multipleMultipliers");
            foreach (int multiplier in multipleMultipliers.Keys.ToArray())
            {
                if (multiplier < minMultiple || multiplier > maxMultiple)
                {
                    Plugin.StaticLogger.LogWarning($"multiplier key ({multiplier}) in multipleMultipliers is out of range of minMultiple ({minMultiple}) and maxMultiple ({maxMultiple}). Removing...");
                    if (removeUnknownNames)
                        multipleMultipliers.Remove(multiplier);
                }
            }
            Plugin.Debug($"Checking setMultipliers");
            foreach (string setMultiplierName in setMultipliers.Keys.ToArray())
            {
                SetMultiplier setMultiplier = setMultipliers[setMultiplierName];
                List<string> invalidItems = new List<string>();
                Plugin.Debug($"Checking names in setMultiplers ({setMultiplierName})");
                foreach (string itemName in setMultiplier.items)
                {
                    if (!ItemNames.Contains(itemName))
                    {
                        invalidItems.Add(itemName);
                    }
                }
                if (invalidItems.Count > 0)
                {
                    Plugin.StaticLogger.LogWarning($"Items {string.Join(",", invalidItems)} from setMultiplier {setMultiplierName} cannot be found in set of all items, see readme or turn on debug to see values");
                    if (removeUnknownNames)
                        setMultipliers.Remove(setMultiplierName);
                }
            }
        }

        public void standardizeItemList(ref string[] listOfItems, string[] allItems, string logName)
        {
            Plugin.Debug($"standardizeItemList({listOfItems.Length}, {allItems.Length}, {logName}) [{String.Join(", ", listOfItems)}]");
            List<string> tempList = listOfItems.Distinct().ToList();
            if (tempList.Count != listOfItems.Length)
            {
                Plugin.StaticLogger.LogWarning($"Duplicates found and removed in {logName}, consider removing the duplicates");
            }
            Plugin.Debug($"Checking each in de-duped list");
            foreach (string itemName in tempList.ToArray())
            {
                if (!allItems.Contains(itemName))
                {
                    Plugin.StaticLogger.LogWarning($"Cannot find '{itemName}' from {logName} in set of all item names, see readme or turn on debug to see values");
                    if (removeUnknownNames)
                        tempList.Remove(itemName);
                }
            }
            Plugin.Debug($"Output list: [{String.Join(", ", listOfItems)}]");
            listOfItems = tempList.ToArray();
        }

        public static ComboSettings FromJson(string json, bool removeUnkownNames)
        {
            try
            {
                ComboSettings settings = JsonConvert.DeserializeObject<ComboSettings>(json);
                settings.removeUnknownNames = removeUnkownNames;
                settings.standardizeValues();
                return settings;
            } catch (Exception e)
            {
                Plugin.StaticLogger.LogError($"Unable to load or standardize values for combo settings, please check config");
                Plugin.StaticLogger.LogError(e);
                return new ComboSettings();
            }
        }
    }
    internal struct SetMultiplier
    {
        public string[] items;
        public float multiplier;
    }
}
