using System;
using UnityEngine;

namespace Core.Settings
{
    [Serializable]
    public struct SettingsData
    {
        public float masterVolume;
        public float musicVolume;
        public float sfxVolume;
        public int resolutionWidth;
        public int resolutionHeight;
        public int refreshRate;
        public FullScreenMode fullScreenMode;
        public bool vSync;
        public int targetFrameRate;
        public float mouseSensitivity;
        public bool invertY;
        public float fieldOfView;
        public string languageCode;

        public static SettingsData Default => new()
        {
            masterVolume = 1f,
            musicVolume = 0.8f,
            sfxVolume = 1f,
            resolutionWidth = 1920,
            resolutionHeight = 1080,
            refreshRate = 60,
            fullScreenMode = FullScreenMode.FullScreenWindow,
            vSync = true,
            targetFrameRate = 60,
            mouseSensitivity = 1f,
            invertY = false,
            fieldOfView = 75f,
            languageCode = "en"
        };
    }
}
