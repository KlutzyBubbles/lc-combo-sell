using System.Collections.Generic;
using static Steamworks.InventoryItem;

namespace ComboSell
{
    internal class ComboSettings
    {

        public bool multiplesFirst = true;

        public string[] includeMultiples = [];
        public string[] excludeMultiples = [];

        public int maxMultiple = 5;
        public int minMultiple = 2;
        public float defaultMultipleMultiplier = 0.2f;
        public float defaultSetMultiplier = 0.2f;

        public Dictionary<int, float> multipleMultipliers = new Dictionary<int, float>();

        public Dictionary<string, SetMultiplier> setMultipliers = new Dictionary<string, SetMultiplier>();

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

        internal struct SetMultiplier
        {
            public string[] items;
            public float multiplier;
        }
    }
}
