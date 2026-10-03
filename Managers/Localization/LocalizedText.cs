using Core.Events;
using TMPro;
using UnityEngine;

namespace Core.Localization
{
    [ExecuteAlways]
    [RequireComponent(typeof(TMP_Text))]
    [AddComponentMenu("Core/Localization/Localized Text")]
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string localizationKey;
        [SerializeField] private string defaultFallbackText;

        private TMP_Text _textComponent;
        private object[] _formatArgs;

        public string LocalizationKey => localizationKey;

        private void Awake()
        {
            _textComponent = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            if (_textComponent == null)
            {
                _textComponent = GetComponent<TMP_Text>();
            }

            EventBus.Subscribe<LocaleChangedEvent>(OnLocaleChanged);
            UpdateText();
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<LocaleChangedEvent>(OnLocaleChanged);
        }

        private void OnLocaleChanged(LocaleChangedEvent evt)
        {
            UpdateText();
        }

        public void UpdateText()
        {
            if (_textComponent == null)
            {
                _textComponent = GetComponent<TMP_Text>();
            }

            if (_textComponent == null || string.IsNullOrEmpty(localizationKey))
            {
                return;
            }

            var fallback = !string.IsNullOrEmpty(defaultFallbackText) ? defaultFallbackText : _textComponent.text;
            var localized = LocalizationManager.Instance != null
                ? LocalizationManager.Instance.Get(localizationKey, fallback)
                : fallback;

            if (_formatArgs != null && _formatArgs.Length > 0)
            {
                try
                {
                    _textComponent.text = string.Format(localized, _formatArgs);
                }
                catch
                {
                    _textComponent.text = localized;
                }
            }
            else
            {
                _textComponent.text = localized;
            }
        }

        public void SetKey(string newKey)
        {
            localizationKey = newKey;
            _formatArgs = null;
            UpdateText();
        }

        public void SetFormattedKey(string newKey, params object[] args)
        {
            localizationKey = newKey;
            _formatArgs = args;
            UpdateText();
        }

        public void SetFormatArgs(params object[] args)
        {
            _formatArgs = args;
            UpdateText();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_textComponent == null)
            {
                _textComponent = GetComponent<TMP_Text>();
            }

            if (!string.IsNullOrEmpty(localizationKey))
            {
                UpdateText();
            }
            else if (!string.IsNullOrEmpty(defaultFallbackText) && _textComponent != null)
            {
                if (string.IsNullOrEmpty(_textComponent.text) || _textComponent.text == "New Text")
                {
                    _textComponent.text = defaultFallbackText;
                }
            }
        }
#endif
    }
}
