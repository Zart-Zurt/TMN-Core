using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Save
{
    [Serializable]
    public class SaveSlotData : ISerializationCallbackReceiver
    {
        public int version = 1;
        public string gameVersion = string.Empty;
        public string slotName = "Slot 1";
        public string lastSaveTimestamp = string.Empty;
        public Dictionary<string, string> entries = new();

        [SerializeField] private List<string> keys = new();
        [SerializeField] private List<string> values = new();

        public void OnBeforeSerialize()
        {
            keys.Clear();
            values.Clear();

            foreach (var kvp in entries)
            {
                keys.Add(kvp.Key);
                values.Add(kvp.Value);
            }
        }

        public void OnAfterDeserialize()
        {
            entries = new Dictionary<string, string>();

            var count = Mathf.Min(keys.Count, values.Count);
            for (var i = 0; i < count; i++)
            {
                entries[keys[i]] = values[i];
            }
        }
    }
}
