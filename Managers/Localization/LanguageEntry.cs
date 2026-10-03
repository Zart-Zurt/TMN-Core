using System;

namespace Core.Localization
{
    [Serializable]
    public struct LanguageEntry
    {
        public string Key;
        public string Value;

        public LanguageEntry(string key, string value)
        {
            Key = key;
            Value = value;
        }
    }
}
