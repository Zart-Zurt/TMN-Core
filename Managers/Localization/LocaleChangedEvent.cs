namespace Core.Localization
{
    public readonly struct LocaleChangedEvent
    {
        public readonly string localeCode;
        public readonly string languageName;
        public readonly bool isCustom;

        public string LocaleCode => localeCode;
        public string LanguageName => languageName;
        public bool IsCustom => isCustom;

        public LocaleChangedEvent(string code, string name = "", bool custom = false)
        {
            localeCode = code;
            languageName = name;
            isCustom = custom;
        }
    }
}
