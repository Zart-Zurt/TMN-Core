using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Core.Localization
{
    [Serializable]
    public class LanguageData
    {
        [SerializeField] private string languageCode = "en";
        [SerializeField] private string languageName = "English";
        [SerializeField] private string author = "Official";
        [SerializeField] private bool isCustom;
        [SerializeField] private List<LanguageEntry> entries = new();

        private readonly Dictionary<string, string> _entryLookup = new(StringComparer.OrdinalIgnoreCase);

        public string LanguageCode => languageCode;
        public string LanguageName => languageName;
        public string Author => author;
        public bool IsCustom => isCustom;
        public IReadOnlyList<LanguageEntry> Entries => entries;

        public LanguageData()
        {
        }

        public LanguageData(string code, string name, string authorName = "Official", bool custom = false)
        {
            languageCode = code;
            languageName = name;
            author = authorName;
            isCustom = custom;
        }

        public void SetMetadata(string code, string name, string authorName, bool custom)
        {
            languageCode = code;
            languageName = name;
            author = authorName;
            isCustom = custom;
        }

        public void AddOrUpdateEntry(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            for (var i = 0; i < entries.Count; i++)
            {
                if (string.Equals(entries[i].Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    entries[i] = new LanguageEntry(key, value);
                    _entryLookup[key] = value;
                    return;
                }
            }

            entries.Add(new LanguageEntry(key, value));
            _entryLookup[key] = value;
        }

        public void InitializeLookup()
        {
            _entryLookup.Clear();
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (!string.IsNullOrEmpty(entry.Key))
                {
                    _entryLookup[entry.Key] = entry.Value ?? string.Empty;
                }
            }
        }

        public bool TryGetValue(string key, out string value)
        {
            if (string.IsNullOrEmpty(key))
            {
                value = string.Empty;
                return false;
            }

            if (_entryLookup.Count == 0 && entries.Count > 0)
            {
                InitializeLookup();
            }

            return _entryLookup.TryGetValue(key, out value);
        }

        public string Get(string key, string fallback = null)
        {
            return TryGetValue(key, out var val) ? val : (fallback ?? key);
        }

        public string ToJson(bool prettyPrint = true)
        {
            return JsonUtility.ToJson(this, prettyPrint);
        }

        public static LanguageData FromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            var data = JsonUtility.FromJson<LanguageData>(json);
            data?.InitializeLookup();
            return data;
        }

        public string ToCsv()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Key,Value");
            for (var i = 0; i < entries.Count; i++)
            {
                var key = EscapeCsv(entries[i].Key);
                var val = EscapeCsv(entries[i].Value);
                sb.AppendLine($"{key},{val}");
            }

            return sb.ToString();
        }

        private static string EscapeCsv(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            if (text.Contains(",") || text.Contains("\"") || text.Contains("\n") || text.Contains("\r"))
            {
                return $"\"{text.Replace("\"", "\"\"")}\"";
            }

            return text;
        }
    }
}
