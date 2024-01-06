using System;
using System.Collections.Generic;
using System.Linq;

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

        public string[] getUniqueNames()
        {
            Plugin.Debug("getUniqueNames()");
            return getUniqueNames(rawObjects);
        }
        public string[] getUniqueNames(GrabbableObject[] objects)
        {
            Plugin.Debug($"getUniqueNames({objects.Length})");
            string[] names = new string[objects.Length];
            for (int i = 0; i < objects.Length; i++)
            {
                names[i] = objects[i].itemProperties.itemName;
            }
            return names.Distinct().ToArray();
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
            }
            else
            {
                multipleCombos = processMultiples(ref unusedObjects);
            }
            return new ComboResult(multipleCombos, setCombos, unusedObjects.ToArray());
        }

        public ObjectCombo[] processMultiples(ref List<GrabbableObject> unusedObjects)
        {
            Plugin.Debug($"processMultiples({unusedObjects.Count})");
            string[] uniques = this.getUniqueNames(unusedObjects.ToArray());
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
