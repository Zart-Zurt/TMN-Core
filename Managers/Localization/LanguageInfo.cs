using System;

namespace Core.Localization
{
    [Serializable]
    public readonly struct LanguageInfo
    {
        public readonly string Code;
        public readonly string DisplayName;
        public readonly string Author;
        public readonly bool IsCustom;

        public LanguageInfo(string code, string displayName, string author, bool isCustom)
        {
            Code = code;
            DisplayName = displayName;
            Author = author;
            IsCustom = isCustom;
        }

        public override string ToString()
        {
            return IsCustom ? $"{DisplayName} ({Author})" : DisplayName;
        }
    }
}
