using System.Collections.Generic;
using TMNLibrary.Singleton;
using UnityEngine;

namespace TMNLibrary.PoolManager
{
    public class PoolManager : MonoSingleton<PoolManager>
    {
        public List<ObjectPool> pools;

        private readonly Dictionary<Pools.Types, ObjectPool> _poolTypeObjectPoolDictionary = new();

        protected override void Awake()
        {
            base.Awake();
            foreach (var pool in pools)
            {
                pool.InitializePool();
                if (!_poolTypeObjectPoolDictionary.TryAdd(pool.poolType, pool))
                    Debug.LogError($"[Pool] Pool Type {pool.poolType} Already Exists!");
            }
        }

        public GameObject Spawn(Pools.Types poolType, Transform parent = null)
        {
            return Spawn(poolType, null, null, parent);
        }

        public GameObject Spawn(Pools.Types poolType, Vector3? position, Quaternion? rotation, Transform parent = null)
        {
            var pool = GetObjectPool(poolType);

            if (pool == null)
                return null;

            var clone = pool.GetNextObject();
            if (!clone)
                return null;
            if (parent)
                clone.transform.SetParent(parent, false);
            if (position != null)
                clone.transform.position = (Vector3)position;
            if (rotation != null)
                clone.transform.rotation = (Quaternion)rotation;
            clone.SetActive(true);
            return clone;
        }

        public GameObject Spawn(Pools.Types poolType, Vector3 minVector, Vector3 maxVector, Quaternion rotation)
        {
            var x = Random.Range(minVector.x, maxVector.x);
            var y = Random.Range(minVector.y, maxVector.y);
            var z = Random.Range(minVector.z, maxVector.z);
            var newPosition = new Vector3(x, y, z);
            return Spawn(poolType, newPosition, rotation);
        }

        public void Despawn(Pools.Types poolType, GameObject obj)
        {
            var poolObject = GetObjectPool(poolType);
            obj.transform.SetParent(poolObject.pool.transform);
            if (!poolObject.PassiveObjectsDictionary.ContainsKey(obj.GetInstanceID()))
                poolObject.PassiveObjectsDictionary.Add(obj.GetInstanceID(), obj);
            obj.SetActive(false);
        }

        public ObjectPool GetObjectPool(Pools.Types poolType)
        {
            _poolTypeObjectPoolDictionary.TryGetValue(poolType, out var objPool);
            return objPool;
        }
    }
}