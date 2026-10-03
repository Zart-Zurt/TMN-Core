using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMNLibrary.Singleton;
using UnityEngine;

namespace Core.Pooling
{
    [AddComponentMenu("Core/Pool Manager")]
    public class PoolManager : Singleton<PoolManager>
    {
        private readonly Dictionary<int, int> _instanceToPoolKey = new();
        private readonly Dictionary<int, Queue<GameObject>> _poolDictionary = new();
        private readonly Dictionary<int, GameObject> _prefabLookup = new();

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null)
            {
                return null;
            }

            var poolKey = prefab.GetInstanceID();

            if (!_poolDictionary.ContainsKey(poolKey))
            {
                _poolDictionary[poolKey] = new Queue<GameObject>();
                _prefabLookup[poolKey] = prefab;
            }

            GameObject instance;

            if (_poolDictionary[poolKey].Count > 0)
            {
                instance = _poolDictionary[poolKey].Dequeue();
                if (instance == null)
                {
                    return Spawn(prefab, position, rotation, parent);
                }
            }
            else
            {
                instance = Instantiate(prefab);
            }

            _instanceToPoolKey[instance.GetInstanceID()] = poolKey;

            instance.transform.SetPositionAndRotation(position, rotation);
            instance.transform.SetParent(parent);
            instance.SetActive(true);

            var poolables = instance.GetComponentsInChildren<IPoolable>(true);
            foreach (var poolable in poolables)
            {
                poolable.OnSpawnFromPool();
            }

            return instance;
        }

        public void Despawn(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            var instanceId = instance.GetInstanceID();
            if (_instanceToPoolKey.TryGetValue(instanceId, out var poolKey))
            {
                if (!_poolDictionary.ContainsKey(poolKey))
                {
                    _poolDictionary[poolKey] = new Queue<GameObject>();
                }

                var poolables = instance.GetComponentsInChildren<IPoolable>(true);
                foreach (var poolable in poolables)
                {
                    poolable.OnReturnToPool();
                }

                instance.SetActive(false);
                instance.transform.SetParent(transform);
                _poolDictionary[poolKey].Enqueue(instance);
            }
            else
            {
                Destroy(instance);
            }
        }

        public void Despawn(GameObject instance, GameObject prefab)
        {
            if (instance == null)
            {
                return;
            }

            if (prefab != null)
            {
                var poolKey = prefab.GetInstanceID();
                _instanceToPoolKey[instance.GetInstanceID()] = poolKey;
                if (!_poolDictionary.ContainsKey(poolKey))
                {
                    _poolDictionary[poolKey] = new Queue<GameObject>();
                    _prefabLookup[poolKey] = prefab;
                }
            }

            Despawn(instance);
        }
    }
}
