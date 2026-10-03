using System;
using UnityEngine;

namespace TMNLibrary.PoolManager
{
    public class Pools : MonoBehaviour
    {
        public enum Types
        {
            Example = 0,
        }

        public static string GetTypeStr(Types poolType)
        {
            return Enum.GetName(typeof(Types), poolType);
        }
    }   
}
