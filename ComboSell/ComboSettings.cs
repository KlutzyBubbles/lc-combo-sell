using System.Collections.Generic;

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

        public Dictionary<int, float> multipleMultipliers = new Dictionary<int, float>();

        public float getMultipleMultiplier(int amount)
        {
            if (multipleMultipliers.ContainsKey(amount))
            {
                return multipleMultipliers[amount];
            }
            return 1 + (defaultMultipleMultiplier * (amount - 1));
        }

    }
}
