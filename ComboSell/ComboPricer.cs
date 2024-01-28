using System;
using System.Collections.Generic;
using System.Linq;

namespace ComboSell
{
    internal class ComboPricer
    {
        private List<GrabbableObject> rawObjects;
        private List<GrabbableObject> _sortedObjects;

        private ComboSettings settings;

        public ComboPricer(ref List<GrabbableObject> objects, ComboSettings settings)
        {
            this.settings = settings;
            rawObjects = objects;
        }

        private List<GrabbableObject> sortedObjects
        {
            get
            {
                if (_sortedObjects == null || _sortedObjects.Count != rawObjects.Count)
                {
                    _sortedObjects = rawObjects.ToList();
                    _sortedObjects.Sort(delegate (GrabbableObject object1, GrabbableObject object2)
                    {
                        if (object1.itemProperties.name != object2.itemProperties.name)
                        {
                            return object1.scrapValue.CompareTo(object2.scrapValue);
                        }
                        return object1.itemProperties.name.CompareTo(object2.itemProperties.name);
                    });
                }
                return _sortedObjects;
            }
        }

        public ComboResult processObjects()
        {
            Plugin.Debug("processObjects()");
            List<GrabbableObject> unusedObjects = [.. sortedObjects];
            List<ObjectCombo> multipleCombos = new List<ObjectCombo>();
            List<ObjectCombo> setCombos = new List<ObjectCombo>();
            if (settings.multiplesFirst)
            {
                Plugin.Debug("multiples first");
                multipleCombos = processMultiples(ref unusedObjects);
                setCombos = processSets(ref unusedObjects);
            }
            else
            {
                Plugin.Debug("sets first");
                setCombos = processSets(ref unusedObjects);
                multipleCombos = processMultiples(ref unusedObjects);
            }
            return new ComboResult(multipleCombos, setCombos, unusedObjects.ToList());
        }

        public List<ObjectCombo> processMultiples(ref List<GrabbableObject> unusedObjects)
        {
            Plugin.Debug($"processMultiples([{string.Join(", ", unusedObjects.ToList().Select(obj => obj.itemProperties.name))}])");
            string[] uniques = unusedObjects.ToList().Select(obj => obj.itemProperties.name).Distinct().ToArray();
            Plugin.Debug($"uniques: [{string.Join(", ", uniques.ToList())}]");
            List<ObjectCombo> combos = new List<ObjectCombo>();
            int maxMultiples = (unusedObjects.Count - uniques.Length) + 1;
            if (maxMultiples > settings.maxMultiple) {
                maxMultiples = settings.maxMultiple;
            }
            foreach (string unique in uniques)
            {
                Plugin.Debug($"Processing for multiples on unique {unique}");
                if (settings.includeMultiples.Length > 0 && !settings.includeMultiples.Contains(unique)) continue;
                Plugin.Debug($"Passes include");
                if (settings.excludeMultiples.Length > 0 && settings.excludeMultiples.Contains(unique)) continue;
                Plugin.Debug($"Passes exclude");
                int count = 0;
                foreach (GrabbableObject obj in unusedObjects)
                {
                    if (obj.itemProperties.name == unique) count++;
                }
                Plugin.Debug($"Found {count} of item");
                if (count > settings.minMultiple)
                {
                    Plugin.Debug($"Found more than minMultiple({settings.minMultiple}) for '{unique}'");
                    do
                    {
                        int leftToGet = count > maxMultiples ? maxMultiples : count;
                        count -= leftToGet;
                        Plugin.Debug($"leftToGet {leftToGet}");
                        if (leftToGet > settings.minMultiple)
                        {
                            ObjectCombo combo = new ObjectCombo(settings.getMultipleMultiplier(leftToGet), ComboType.Mulitple, $"x{leftToGet}");
                            foreach (GrabbableObject obj in unusedObjects.ToList())
                            {
                                if (leftToGet > 0 && obj.itemProperties.name == unique)
                                {
                                    combo.addObject(obj);
                                    unusedObjects.Remove(obj);
                                    leftToGet--;
                                    Plugin.Debug($"Adding object {unique} to combo with new leftToGet {leftToGet}");
                                }
                            }
                            combos.Add(combo);
                        }
                    }
                    while (count >= maxMultiples);
                }
            }
            return combos;
        }

        public List<ObjectCombo> processSets(ref List<GrabbableObject> unusedObjects)
        {
            Plugin.Debug($"processSets([{string.Join(", ", unusedObjects.ToList().Select(obj => obj.itemProperties.name))}])");
            List<ObjectCombo> combos = new List<ObjectCombo>();
            foreach (string setName in settings.setMultipliers.Keys)
            {
                Plugin.Debug($"Checking set name {setName}");
                bool keepChecking = true;
                while (keepChecking)
                {
                    Plugin.Debug($"keepChecking while");
                    List<GrabbableObject> foundObjects = new List<GrabbableObject>();
                    foreach (string itemName in settings.setMultipliers[setName].items)
                    {
                        Plugin.Debug($"Checking for item {itemName}");
                        bool objectFound = false;
                        foreach (GrabbableObject obj in unusedObjects.ToList())
                        {
                            Plugin.Debug($"Checking '{obj.itemProperties.name}' against '{itemName}'");
                            if (obj.itemProperties.name == itemName)
                            {
                                Plugin.Debug($"Adding foundObject");
                                foundObjects.Add(obj);
                                objectFound = true;
                                break;
                            }
                        }
                        if (!objectFound)
                        {
                            Plugin.Debug($"Object not found");
                            keepChecking = false;
                            break;
                        }
                    }
                    if (keepChecking)
                    {
                        Plugin.Debug($"Found all objects for '{setName}'");
                        ObjectCombo combo = new ObjectCombo(settings.getSetMultiplier(setName, foundObjects.Count), ComboType.Set, $"{setName}");
                        foreach (GrabbableObject obj in foundObjects)
                        {
                            combo.addObject(obj);
                            unusedObjects.Remove(obj);
                            unusedObjects.Remove(obj);
                        }
                        combos.Add(combo);
                    }
                }
            }
            return combos;
        }
    }

    internal struct ComboResult
    {
        public ComboResult(List<ObjectCombo> multipleCombos, List<ObjectCombo> setCombos, List<GrabbableObject> otherObjects)
        {
            this.multipleCombos = multipleCombos ?? new List<ObjectCombo>();
            this.setCombos = setCombos ?? new List<ObjectCombo>();
            this.otherObjects = otherObjects ?? new List<GrabbableObject>();
        }

        public List<ObjectCombo> multipleCombos { get; }
        public List<ObjectCombo> setCombos { get; }
        public List<GrabbableObject> otherObjects { get; }
    }

    internal enum ComboType
    {
        Mulitple,
        Set
    }

    internal class ObjectCombo
    {
        private List<GrabbableObject> _objects;
        public readonly string name;
        public readonly ComboType type;
        public readonly float multiplier;

        public ObjectCombo(float multiplier, ComboType type, string name)
        {
            _objects = new List<GrabbableObject>();
            this.multiplier = multiplier;
            this.type = type;
            this.name = name;
        }

        public ObjectCombo(List<GrabbableObject> objects, float multiplier, ComboType type, string name)
        {
            _objects = objects;
            this.multiplier = multiplier;
            this.type = type;
            this.name = name;
        }

        public void addObject(GrabbableObject obj)
        {
            _objects.Add(obj);
        }

        public GrabbableObject[] objects
        {
            get
            {
                return _objects.ToArray();
            }
        }

        public int totalValue
        {
            get
            {
                int totalValue = 0;
                foreach (GrabbableObject obj in objects)
                {
                    totalValue += obj.scrapValue;
                }
                return (int)(totalValue * multiplier);
            }
        }

        public string itemNames
        {
            get
            {
                return string.Join(", ", objects.ToList().Select(obj => obj.itemProperties.name));
            }
        }
        public string uniqueItemNames
        {
            get
            {
                return string.Join(", ", objects.ToList().Select(obj => obj.itemProperties.name).Distinct());
            }
        }
    }
}
