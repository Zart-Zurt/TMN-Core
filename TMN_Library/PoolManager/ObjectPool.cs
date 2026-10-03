using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TMNLibrary.PoolManager
{
    [System.Serializable]
    public class ObjectPool
    {
        public GameObject prefab;

        public int maximumInstances;

        public Pools.Types poolType;

        [HideInInspector] public GameObject pool;

        public Dictionary<int, GameObject> PassiveObjectsDictionary;
        
        private GameObject _tempObject;

        public int MaximumInstances => maximumInstances;

        public Pools.Types PoolType
        {
            get => poolType;
            set => poolType = value;
        }

        public void InitializePool()
        {
            PassiveObjectsDictionary = new Dictionary<int, GameObject>();
            pool = new GameObject("[" + poolType + "]");
            
            for (var i = 0; i < maximumInstances; i++)
            {
                var clone = Object.Instantiate(prefab, pool.transform, true);
                clone.SetActive(false);
                PassiveObjectsDictionary.Add(clone.GetInstanceID(), clone);
            }
        }

        public GameObject GetNextObject()
        {
            if (PassiveObjectsDictionary.Count > 0)
            {
                _tempObject = PassiveObjectsDictionary.Values.ElementAt(0);
                PassiveObjectsDictionary.Remove(PassiveObjectsDictionary.Keys.ElementAt(0));
                return _tempObject;
            }

            Debug.Log($"[Pool] {PoolType} - Dictionary is empty. Instantiating new object.");
            var clone = Object.Instantiate(prefab, pool.transform, true);
            clone.SetActive(false);
            PassiveObjectsDictionary.Add(clone.GetInstanceID(), clone);
            _tempObject = PassiveObjectsDictionary.Values.ElementAt(0);
            PassiveObjectsDictionary.Remove(PassiveObjectsDictionary.Keys.ElementAt(0));
            return _tempObject;
        }
    }
}