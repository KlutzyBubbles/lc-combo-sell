using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace ComboSell
{
    internal class ComboPricer
    {
        private GrabbableObject[] rawObjects;
        private GrabbableObject[] _sortedObjects;

        private ComboSettings settings;

        public ComboPricer(GrabbableObject[] objects, ComboSettings settings)
        {
            this.settings = settings;
            rawObjects = objects;
        }

        private GrabbableObject[] sortedObjects
        {
            get
            {
                if (_sortedObjects == null || _sortedObjects.Length != rawObjects.Length)
                {
                    _sortedObjects = new GrabbableObject[rawObjects.Length];
                    sortObjects();
                }
                return _sortedObjects;
            }
        }

        public GrabbableObject[] sortObjects()
        {
            Plugin.Debug("sortObjects()");
            Array.Copy(rawObjects, _sortedObjects, rawObjects.Length);
            Array.Sort(_sortedObjects, delegate (GrabbableObject object1, GrabbableObject object2) {
                if (object1.itemProperties.itemName != object2.itemProperties.itemName)
                {
                    return object1.scrapValue.CompareTo(object2.scrapValue);
                }
                return object1.itemProperties.itemName.CompareTo(object2.itemProperties.itemName);
            });
            return _sortedObjects;
        }

        public ComboResult processObjects()
        {
            Plugin.Debug("processObjects()");
            List<GrabbableObject> unusedObjects = [.. sortedObjects];
            ObjectCombo[] multipleCombos = [];
            ObjectCombo[] setCombos = [];
            if (settings.multiplesFirst)
            {
                multipleCombos = processMultiples(ref unusedObjects);
                setCombos = processSets(ref unusedObjects);
            }
            else
            {
                setCombos = processSets(ref unusedObjects);
                multipleCombos = processMultiples(ref unusedObjects);
            }
            return new ComboResult(multipleCombos, setCombos, unusedObjects.ToArray());
        }

        public ObjectCombo[] processMultiples(ref List<GrabbableObject> unusedObjects)
        {
            Plugin.Debug($"processMultiples({unusedObjects.Count})");
            string[] uniques = unusedObjects.Select(obj => obj.itemProperties.itemName).Distinct().ToArray();
            List<ObjectCombo> combos = new List<ObjectCombo>();
            int maxMultiples = (unusedObjects.Count - uniques.Length) + 1;
            if (maxMultiples > settings.maxMultiple) {
                maxMultiples = settings.maxMultiple;
            }
            foreach (string unique in uniques)
            {
                if (settings.includeMultiples.Length > 0 && !settings.includeMultiples.Contains(unique)) continue;
                if (settings.excludeMultiples.Length > 0 && settings.excludeMultiples.Contains(unique)) continue;
                int count = 0;
                foreach (GrabbableObject obj in unusedObjects)
                {
                    if (obj.itemProperties.itemName == unique) count++;
                }
                if (count > settings.minMultiple)
                {
                    Plugin.Debug($"Found more than minMultiple({settings.minMultiple}) for '{unique}'");
                    do
                    {
                        int leftToGet = count > maxMultiples ? maxMultiples : count;
                        count -= leftToGet;
                        if (leftToGet > settings.minMultiple)
                        {
                            ObjectCombo combo = new ObjectCombo(settings.getMultipleMultiplier(leftToGet), ComboType.Mulitple, $"x{leftToGet}");
                            foreach (GrabbableObject obj in unusedObjects.ToList())
                            {
                                if (leftToGet > 0 && obj.itemProperties.itemName == unique)
                                {
                                    combo.addObject(obj);
                                    unusedObjects.Remove(obj);
                                    leftToGet--;
                                }
                            }
                            combos.Add(combo);
                        }
                    }
                    while (count >= maxMultiples);
                }
            }
            return combos.ToArray();
        }

        public ObjectCombo[] processSets(ref List<GrabbableObject> unusedObjects)
        {
            Plugin.Debug($"processSets({unusedObjects.Count})");
            List<ObjectCombo> combos = new List<ObjectCombo>();
            foreach (string setName in settings.setMultipliers.Keys)
            {
                bool keepChecking = true;
                while (keepChecking)
                {
                    List<GrabbableObject> foundObjects = new List<GrabbableObject>();
                    foreach (string itemName in settings.setMultipliers[setName].items)
                    {
                        GrabbableObject foundObject = unusedObjects.FirstOrDefault(obj => obj.name == itemName);
                        if (foundObject != null)
                        {
                            foundObjects.Add(foundObject);
                            continue;
                        }
                        else
                        {
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
                        }
                        combos.Add(combo);
                    }
                }
            }
            return combos.ToArray();
        }
    }

    internal struct ComboResult
    {
        public ComboResult(ObjectCombo[] multipleCombos, ObjectCombo[] setCombos, GrabbableObject[] otherObjects)
        {
            this.multipleCombos = multipleCombos;
            this.setCombos = setCombos;
            this.otherObjects = otherObjects;
        }

        public ObjectCombo[] multipleCombos { get; }
        public ObjectCombo[] setCombos { get; }
        public GrabbableObject[] otherObjects { get; }
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
                List<string> doneNames = new List<string>();
                string result = "";
                string prefix = "";
                foreach (GrabbableObject obj in objects)
                {
                    if (!doneNames.Contains(obj.itemProperties.itemName))
                    {
                        result += $"{prefix}{obj.itemProperties.itemName}";
                        doneNames.Add(obj.itemProperties.itemName);
                        prefix = ", ";
                    }
                }
                return result;
            }
        }
    }
}
