using System;
using System.Collections.Generic;
using Core.Events;
using Core.Inputs;
using Core.Localization;
using Core.Settings;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Core.UI.Settings
{
    [AddComponentMenu("Core/UI/Settings UI Controller")]
    [TypeInfoBox("Central UI controller for the settings menu. Coordinates tab navigation and synchronizes UI controls for Audio, Video, Gameplay, and Localization with system managers.")]
    public class SettingsUIController : MonoBehaviour
    {
        [Header("Root & Navigation")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button closeButton;

        [Header("Audio")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;

        [Header("Video")]
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private TMP_Dropdown fullscreenModeDropdown;
        [SerializeField] private TMP_Dropdown frameRateDropdown;
        [SerializeField] private Toggle vSyncToggle;

        [Header("Gameplay")]
        [SerializeField] private Slider fovSlider;
        [SerializeField] private Slider mouseSensitivitySlider;
        [SerializeField] private Toggle invertYToggle;

        [Header("Localization")]
        [SerializeField] private TMP_Dropdown languageDropdown;
        [SerializeField] private Button openLanguagesFolderButton;

        [Header("Tabs")]
        [SerializeField] private Button audioTabButton;
        [SerializeField] private Button videoTabButton;
        [SerializeField] private Button gameplayTabButton;
        [SerializeField] private Button controlsTabButton;
        [SerializeField] private GameObject audioPanel;
        [SerializeField] private GameObject videoPanel;
        [SerializeField] private GameObject gameplayPanel;
        [SerializeField] private GameObject controlsPanel;

        private readonly List<Resolution> _filteredResolutions = new();
        private readonly List<LanguageInfo> _availableLanguages = new();
        private readonly int[] _targetFrameRates = { 30, 60, 120, 144, 240, -1 };
        private readonly FullScreenMode[] _fullScreenModes =
        {
            FullScreenMode.FullScreenWindow,
            FullScreenMode.ExclusiveFullScreen,
            FullScreenMode.MaximizedWindow,
            FullScreenMode.Windowed
        };

        private bool _isUpdatingUI;

        private void Awake()
        {
            InitializeDropdownOptions();
            InitializeTabs();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<SettingsChangedEvent>(OnSettingsChanged);
            EventBus.Subscribe<LocaleChangedEvent>(OnLocaleChanged);
            AddListeners();

            RefreshLanguageDropdownOptions();

            if (SettingsManager.Instance != null)
            {
                ApplySettingsToUI(SettingsManager.Instance.CurrentSettings);
            }
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<SettingsChangedEvent>(OnSettingsChanged);
            EventBus.Unsubscribe<LocaleChangedEvent>(OnLocaleChanged);
            RemoveListeners();
        }

        private void Update()
        {
            var escapePressed = false;
            if (Keyboard.current != null)
            {
                escapePressed = Keyboard.current.escapeKey.wasPressedThisFrame;
            }

            if (escapePressed)
            {
                if (InputRebindingUIController.IsAnyRebindingActive)
                {
                    return;
                }

                TogglePanel();
            }
        }

        public void TogglePanel()
        {
            if (panelRoot != null)
            {
                SetPanelActive(!panelRoot.activeSelf);
            }
        }

        public void OpenPanel()
        {
            SetPanelActive(true);
        }

        public void ClosePanel()
        {
            SetPanelActive(false);
        }

        private void SetPanelActive(bool active)
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(active);
            }

            if (active)
            {
                RefreshLanguageDropdownOptions();
                if (SettingsManager.Instance != null)
                {
                    ApplySettingsToUI(SettingsManager.Instance.CurrentSettings);
                }
            }
        }

        private void OnSettingsChanged(SettingsChangedEvent evt)
        {
            ApplySettingsToUI(evt.settings);
        }

        private void OnLocaleChanged(LocaleChangedEvent evt)
        {
            if (_isUpdatingUI)
            {
                return;
            }

            RefreshLanguageDropdownOptions();
            if (SettingsManager.Instance != null)
            {
                ApplySettingsToUI(SettingsManager.Instance.CurrentSettings);
            }
        }

        private void ApplySettingsToUI(SettingsData data)
        {
            _isUpdatingUI = true;

            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.value = data.masterVolume;
            }

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.value = data.musicVolume;
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.value = data.sfxVolume;
            }

            if (fovSlider != null)
            {
                fovSlider.value = data.fieldOfView;
            }

            if (mouseSensitivitySlider != null)
            {
                mouseSensitivitySlider.value = data.mouseSensitivity;
            }

            if (invertYToggle != null)
            {
                invertYToggle.isOn = data.invertY;
            }

            if (vSyncToggle != null)
            {
                vSyncToggle.isOn = data.vSync;
            }

            if (resolutionDropdown != null && _filteredResolutions.Count > 0)
            {
                var selectedIndex = 0;
                for (var i = 0; i < _filteredResolutions.Count; i++)
                {
                    if (_filteredResolutions[i].width == data.resolutionWidth &&
                        _filteredResolutions[i].height == data.resolutionHeight)
                    {
                        selectedIndex = i;
                        break;
                    }
                }

                resolutionDropdown.value = selectedIndex;
                resolutionDropdown.RefreshShownValue();
            }

            if (fullscreenModeDropdown != null)
            {
                var modeIndex = 0;
                for (var i = 0; i < _fullScreenModes.Length; i++)
                {
                    if (_fullScreenModes[i] == data.fullScreenMode)
                    {
                        modeIndex = i;
                        break;
                    }
                }

                fullscreenModeDropdown.value = modeIndex;
                fullscreenModeDropdown.RefreshShownValue();
            }

            if (frameRateDropdown != null)
            {
                var rateIndex = 0;
                for (var i = 0; i < _targetFrameRates.Length; i++)
                {
                    if (_targetFrameRates[i] == data.targetFrameRate)
                    {
                        rateIndex = i;
                        break;
                    }
                }

                frameRateDropdown.value = rateIndex;
                frameRateDropdown.RefreshShownValue();
            }

            if (languageDropdown != null && _availableLanguages.Count > 0)
            {
                var targetCode = !string.IsNullOrEmpty(data.languageCode)
                    ? data.languageCode
                    : (LocalizationManager.Instance != null ? LocalizationManager.Instance.CurrentLocaleCode : "en");

                var langIndex = 0;
                for (var i = 0; i < _availableLanguages.Count; i++)
                {
                    if (string.Equals(_availableLanguages[i].Code, targetCode, StringComparison.OrdinalIgnoreCase))
                    {
                        langIndex = i;
                        break;
                    }
                }

                languageDropdown.value = langIndex;
                languageDropdown.RefreshShownValue();
            }

            _isUpdatingUI = false;
        }

        private void OnSettingUIChanged()
        {
            if (_isUpdatingUI || SettingsManager.Instance == null)
            {
                return;
            }

            var current = SettingsManager.Instance.CurrentSettings;

            if (masterVolumeSlider != null)
            {
                current.masterVolume = masterVolumeSlider.value;
            }

            if (musicVolumeSlider != null)
            {
                current.musicVolume = musicVolumeSlider.value;
            }

            if (sfxVolumeSlider != null)
            {
                current.sfxVolume = sfxVolumeSlider.value;
            }

            if (fovSlider != null)
            {
                current.fieldOfView = fovSlider.value;
            }

            if (mouseSensitivitySlider != null)
            {
                current.mouseSensitivity = mouseSensitivitySlider.value;
            }

            if (invertYToggle != null)
            {
                current.invertY = invertYToggle.isOn;
            }

            if (vSyncToggle != null)
            {
                current.vSync = vSyncToggle.isOn;
            }

            if (resolutionDropdown != null && resolutionDropdown.value >= 0 && resolutionDropdown.value < _filteredResolutions.Count)
            {
                var res = _filteredResolutions[resolutionDropdown.value];
                current.resolutionWidth = res.width;
                current.resolutionHeight = res.height;
                current.refreshRate = Mathf.RoundToInt((float)res.refreshRateRatio.value);
            }

            if (fullscreenModeDropdown != null && fullscreenModeDropdown.value >= 0 && fullscreenModeDropdown.value < _fullScreenModes.Length)
            {
                current.fullScreenMode = _fullScreenModes[fullscreenModeDropdown.value];
            }

            if (frameRateDropdown != null && frameRateDropdown.value >= 0 && frameRateDropdown.value < _targetFrameRates.Length)
            {
                current.targetFrameRate = _targetFrameRates[frameRateDropdown.value];
            }

            if (languageDropdown != null && languageDropdown.value >= 0 && languageDropdown.value < _availableLanguages.Count)
            {
                current.languageCode = _availableLanguages[languageDropdown.value].Code;
            }

            SettingsManager.Instance.SaveSettings(current);
        }

        private void InitializeDropdownOptions()
        {
            if (resolutionDropdown != null)
            {
                resolutionDropdown.ClearOptions();
                _filteredResolutions.Clear();
                var options = new List<string>();

                var screenResolutions = Screen.resolutions;
                for (var i = 0; i < screenResolutions.Length; i++)
                {
                    var res = screenResolutions[i];
                    var isDuplicate = false;
                    for (var j = 0; j < _filteredResolutions.Count; j++)
                    {
                        if (_filteredResolutions[j].width == res.width &&
                            _filteredResolutions[j].height == res.height)
                        {
                            isDuplicate = true;
                            break;
                        }
                    }

                    if (!isDuplicate)
                    {
                        _filteredResolutions.Add(res);
                        options.Add($"{res.width} x {res.height}");
                    }
                }

                if (_filteredResolutions.Count == 0)
                {
                    options.Add("1920 x 1080");
                }

                resolutionDropdown.AddOptions(options);
            }

            if (fullscreenModeDropdown != null)
            {
                fullscreenModeDropdown.ClearOptions();
                var modeOptions = new List<string>
                {
                    "Borderless Fullscreen",
                    "Exclusive Fullscreen",
                    "Maximized Window",
                    "Windowed"
                };
                fullscreenModeDropdown.AddOptions(modeOptions);
            }

            if (frameRateDropdown != null)
            {
                frameRateDropdown.ClearOptions();
                var rateOptions = new List<string>();
                for (var i = 0; i < _targetFrameRates.Length; i++)
                {
                    rateOptions.Add(_targetFrameRates[i] == -1 ? "Unlimited" : $"{_targetFrameRates[i]} FPS");
                }

                frameRateDropdown.AddOptions(rateOptions);
            }

            RefreshLanguageDropdownOptions();
        }

        private void RefreshLanguageDropdownOptions()
        {
            if (languageDropdown == null)
            {
                return;
            }

            languageDropdown.ClearOptions();
            _availableLanguages.Clear();

            if (LocalizationManager.Instance != null)
            {
                _availableLanguages.AddRange(LocalizationManager.Instance.GetAvailableLanguages());
            }

            var options = new List<string>();
            for (var i = 0; i < _availableLanguages.Count; i++)
            {
                options.Add(_availableLanguages[i].ToString());
            }

            if (options.Count == 0)
            {
                options.Add("English (Default)");
            }

            languageDropdown.AddOptions(options);
        }

        private void InitializeTabs()
        {
            if (audioTabButton != null)
            {
                audioTabButton.onClick.AddListener(() => SwitchTab(0));
            }

            if (videoTabButton != null)
            {
                videoTabButton.onClick.AddListener(() => SwitchTab(1));
            }

            if (gameplayTabButton != null)
            {
                gameplayTabButton.onClick.AddListener(() => SwitchTab(2));
            }

            if (controlsTabButton != null)
            {
                controlsTabButton.onClick.AddListener(() => SwitchTab(3));
            }
        }

        public void SwitchTab(int tabIndex)
        {
            if (audioPanel != null)
            {
                audioPanel.SetActive(tabIndex == 0);
            }

            if (videoPanel != null)
            {
                videoPanel.SetActive(tabIndex == 1);
            }

            if (gameplayPanel != null)
            {
                gameplayPanel.SetActive(tabIndex == 2);
            }

            if (controlsPanel != null)
            {
                controlsPanel.SetActive(tabIndex == 3);
            }
        }

        private void AddListeners()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(ClosePanel);
            }

            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.onValueChanged.AddListener(OnSliderChanged);
            }

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.onValueChanged.AddListener(OnSliderChanged);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.onValueChanged.AddListener(OnSliderChanged);
            }

            if (fovSlider != null)
            {
                fovSlider.onValueChanged.AddListener(OnSliderChanged);
            }

            if (mouseSensitivitySlider != null)
            {
                mouseSensitivitySlider.onValueChanged.AddListener(OnSliderChanged);
            }

            if (vSyncToggle != null)
            {
                vSyncToggle.onValueChanged.AddListener(OnToggleChanged);
            }

            if (invertYToggle != null)
            {
                invertYToggle.onValueChanged.AddListener(OnToggleChanged);
            }

            if (resolutionDropdown != null)
            {
                resolutionDropdown.onValueChanged.AddListener(OnDropdownChanged);
            }

            if (fullscreenModeDropdown != null)
            {
                fullscreenModeDropdown.onValueChanged.AddListener(OnDropdownChanged);
            }

            if (frameRateDropdown != null)
            {
                frameRateDropdown.onValueChanged.AddListener(OnDropdownChanged);
            }

            if (languageDropdown != null)
            {
                languageDropdown.onValueChanged.AddListener(OnDropdownChanged);
            }

            if (openLanguagesFolderButton != null)
            {
                openLanguagesFolderButton.onClick.AddListener(OnOpenLanguagesFolderClicked);
            }
        }

        private void RemoveListeners()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(ClosePanel);
            }

            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.onValueChanged.RemoveListener(OnSliderChanged);
            }

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.onValueChanged.RemoveListener(OnSliderChanged);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.onValueChanged.RemoveListener(OnSliderChanged);
            }

            if (fovSlider != null)
            {
                fovSlider.onValueChanged.RemoveListener(OnSliderChanged);
            }

            if (mouseSensitivitySlider != null)
            {
                mouseSensitivitySlider.onValueChanged.RemoveListener(OnSliderChanged);
            }

            if (vSyncToggle != null)
            {
                vSyncToggle.onValueChanged.RemoveListener(OnToggleChanged);
            }

            if (invertYToggle != null)
            {
                invertYToggle.onValueChanged.RemoveListener(OnToggleChanged);
            }

            if (resolutionDropdown != null)
            {
                resolutionDropdown.onValueChanged.RemoveListener(OnDropdownChanged);
            }

            if (fullscreenModeDropdown != null)
            {
                fullscreenModeDropdown.onValueChanged.RemoveListener(OnDropdownChanged);
            }

            if (frameRateDropdown != null)
            {
                frameRateDropdown.onValueChanged.RemoveListener(OnDropdownChanged);
            }

            if (languageDropdown != null)
            {
                languageDropdown.onValueChanged.RemoveListener(OnDropdownChanged);
            }

            if (openLanguagesFolderButton != null)
            {
                openLanguagesFolderButton.onClick.RemoveListener(OnOpenLanguagesFolderClicked);
            }
        }

        private void OnSliderChanged(float _)
        {
            OnSettingUIChanged();
        }

        private void OnToggleChanged(bool _)
        {
            OnSettingUIChanged();
        }

        private void OnDropdownChanged(int _)
        {
            OnSettingUIChanged();
        }

        private void OnOpenLanguagesFolderClicked()
        {
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OpenLanguagesFolder();
            }
        }
    }
}
