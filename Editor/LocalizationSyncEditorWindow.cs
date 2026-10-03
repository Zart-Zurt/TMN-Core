#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Core.Localization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Core.Editor
{
    public class LocalizationSyncEditorWindow : EditorWindow
    {
        private const string SheetCsvUrlPrefKey = "Core_LocalizationSheetCsvUrl";
        private const string AutoSyncOnPlayPrefKey = "Core_AutoSyncLocalizationOnPlay";
        private const string DefaultStreamingLanguagesPath = "Assets/StreamingAssets/Languages";

        private string sheetCsvUrl = "";
        private bool autoSyncOnPlay;
        private string _statusMessage = "";
        private MessageType _statusMessageType = MessageType.Info;
        private Vector2 _scrollPosition;

        [MenuItem("Tools/Localization Sync")]
        public static void OpenWindow()
        {
            var window = GetWindow<LocalizationSyncEditorWindow>("Localization Sync");
            window.minSize = new Vector2(450f, 520f);
            window.Show();
        }

        private void OnEnable()
        {
            sheetCsvUrl = EditorPrefs.GetString(SheetCsvUrlPrefKey, "");
            autoSyncOnPlay = EditorPrefs.GetBool(AutoSyncOnPlayPrefKey, false);
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawHeader();
            DrawConfigurationFields();
            DrawActions();
            DrawTemplateAndToolsSection();
            DrawStatusBox();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Localization Sync Hub", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Synchronize translations from Google Sheets and manage community templates.", EditorStyles.miniLabel);
            EditorGUILayout.Space(8);
        }

        private void DrawConfigurationFields()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Google Sheets Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            EditorGUILayout.HelpBox(
                "Google Sheet must be published to the web as CSV.\n" +
                "Columns: Key, en (English), tr (Türkçe), de (Deutsch), etc.\n" +
                "Header format can be either language code ('en', 'tr') or named ('English (en)', 'Türkçe (tr)').",
                MessageType.Info);

            EditorGUILayout.Space(4);

            EditorGUI.BeginChangeCheck();
            sheetCsvUrl = EditorGUILayout.TextField("Sheet CSV URL", sheetCsvUrl);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetString(SheetCsvUrlPrefKey, sheetCsvUrl);
            }

            EditorGUI.BeginChangeCheck();
            autoSyncOnPlay = EditorGUILayout.Toggle("Auto Sync on Play", autoSyncOnPlay);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool(AutoSyncOnPlayPrefKey, autoSyncOnPlay);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }

        private void DrawActions()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Synchronization Actions", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            GUI.backgroundColor = new Color(0.35f, 0.75f, 0.35f);
            if (GUILayout.Button("Pull Localization from Sheets", GUILayout.Height(36)))
            {
                PullFromSheets();
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }

        private void DrawTemplateAndToolsSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Community & Modding Tools", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Generate translation templates and open languages folder.", EditorStyles.miniLabel);
            EditorGUILayout.Space(6);

            if (GUILayout.Button("Generate Modding Template & README", GUILayout.Height(28)))
            {
                GenerateModdingTemplates();
            }

            if (GUILayout.Button("Open Languages Folder (Explorer)", GUILayout.Height(28)))
            {
                OpenLanguagesFolder();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }

        private void DrawStatusBox()
        {
            if (!string.IsNullOrEmpty(_statusMessage))
            {
                EditorGUILayout.HelpBox(_statusMessage, _statusMessageType);
            }
        }

        public void PullFromSheets()
        {
            if (string.IsNullOrEmpty(sheetCsvUrl))
            {
                UpdateStatus("Sheet CSV URL cannot be empty. Please enter a valid published Google Sheets CSV link.", MessageType.Error);
                return;
            }

            UpdateStatus("Downloading localization CSV from Google Sheets...", MessageType.Info);

            ExecutePullRequest(sheetCsvUrl, (success, message) =>
            {
                UpdateStatus(message, success ? MessageType.Info : MessageType.Error);
                Repaint();
            });
        }

        public static void SyncLocalizationFromSheets(Action onComplete = null)
        {
            var url = EditorPrefs.GetString(SheetCsvUrlPrefKey, "");
            if (string.IsNullOrEmpty(url))
            {
                Debug.LogWarning("[LocalizationSync] Sheet CSV URL is not configured. Skipping auto-sync.");
                onComplete?.Invoke();
                return;
            }

            ExecutePullRequest(url, (success, message) =>
            {
                if (success)
                {
                    Debug.Log($"[LocalizationSync] {message}");
                }
                else
                {
                    Debug.LogError($"[LocalizationSync] {message}");
                }

                onComplete?.Invoke();
            });
        }

        private static void ExecutePullRequest(string url, Action<bool, string> onFinished)
        {
            var request = UnityWebRequest.Get(url);
            var asyncOp = request.SendWebRequest();

            asyncOp.completed += _ =>
            {
                try
                {
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        var errorMsg = $"Pull failed: {request.error}";
                        Debug.LogError($"[LocalizationSync] {errorMsg}");
                        onFinished?.Invoke(false, errorMsg);
                        return;
                    }

                    var csvText = request.downloadHandler.text;
                    if (string.IsNullOrEmpty(csvText))
                    {
                        var emptyMsg = "CSV response was empty.";
                        Debug.LogWarning($"[LocalizationSync] {emptyMsg}");
                        onFinished?.Invoke(false, emptyMsg);
                        return;
                    }

                    var resultMsg = ProcessAndSaveLocalizationCsv(csvText);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                    if (Application.isPlaying && LocalizationManager.Instance != null)
                    {
                        LocalizationManager.Instance.ReloadLanguages();
                    }

                    onFinished?.Invoke(true, resultMsg);
                }
                catch (Exception exception)
                {
                    var exMsg = $"Error processing localization CSV: {exception.Message}";
                    Debug.LogError($"[LocalizationSync] {exMsg}\n{exception.StackTrace}");
                    onFinished?.Invoke(false, exMsg);
                }
                finally
                {
                    request.Dispose();
                }
            };
        }

        public static string ProcessAndSaveLocalizationCsv(string csvContent)
        {
            var lines = csvContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 2)
            {
                return "CSV has insufficient rows (needs header and at least one entry).";
            }

            var headerTokens = ParseCsvLine(lines[0]);
            if (headerTokens.Count < 2)
            {
                return "CSV header must have at least 'Key' and one language column.";
            }

            var languages = new List<LanguageData>();
            for (var col = 1; col < headerTokens.Count; col++)
            {
                var rawHeader = headerTokens[col].Trim();
                ParseHeader(rawHeader, out var code, out var displayName);
                languages.Add(new LanguageData(code, displayName, "Official", false));
            }

            var keyCount = 0;
            for (var row = 1; row < lines.Length; row++)
            {
                var line = lines[row].Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                {
                    continue;
                }

                var tokens = ParseCsvLine(line);
                if (tokens.Count == 0)
                {
                    continue;
                }

                var key = tokens[0].Trim();
                if (string.IsNullOrEmpty(key))
                {
                    continue;
                }

                keyCount++;
                for (var col = 1; col < tokens.Count && col - 1 < languages.Count; col++)
                {
                    var val = tokens[col];
                    languages[col - 1].AddOrUpdateEntry(key, val);
                }
            }

            EnsureDirectory(DefaultStreamingLanguagesPath);

            var savedLanguages = new List<string>();
            for (var i = 0; i < languages.Count; i++)
            {
                var lang = languages[i];
                var json = lang.ToJson(true);

                var streamingFile = Path.Combine(DefaultStreamingLanguagesPath, $"{lang.LanguageCode}.json");
                File.WriteAllText(streamingFile, json);

                savedLanguages.Add($"{lang.LanguageName} ({lang.LanguageCode})");
            }

            GenerateModdingTemplates();

            return $"Successfully pulled {languages.Count} languages ({string.Join(", ", savedLanguages)}) with {keyCount} keys each.";
        }

        public static void GenerateModdingTemplates()
        {
            EnsureDirectory(DefaultStreamingLanguagesPath);

            var template = new LanguageData("template", "Your Language Name Here", "Your Name / Community", true);
            template.AddOrUpdateEntry("menu.play", "Translate: Play");
            template.AddOrUpdateEntry("menu.settings", "Translate: Settings");
            template.AddOrUpdateEntry("menu.quit", "Translate: Quit");
            template.AddOrUpdateEntry("settings.audio", "Translate: Audio");
            template.AddOrUpdateEntry("settings.video", "Translate: Video");
            template.AddOrUpdateEntry("settings.gameplay", "Translate: Gameplay");
            template.AddOrUpdateEntry("settings.controls", "Translate: Controls");
            template.AddOrUpdateEntry("settings.language", "Translate: Language");

            var templatePath = Path.Combine(DefaultStreamingLanguagesPath, "template_language.json");
            File.WriteAllText(templatePath, template.ToJson(true));

            var readmePath = Path.Combine(DefaultStreamingLanguagesPath, "README_TRANSLATIONS.txt");
            var readmeContent =
@"=====================================================
COMMUNITY LOCALIZATION & TRANSLATION GUIDE
=====================================================

Welcome! You can easily add your own language to the game or translate existing ones.

HOW TO ADD A NEW LANGUAGE:
1. Navigate to: [GameDirectory]/Languages (or StreamingAssets/Languages).
2. Make a copy of 'template_language.json' or any existing language file (e.g. 'en.json').
3. Rename your new file to your language code (e.g. 'es.json', 'it.json', 'pl.json').
4. Open the file with any text editor (Notepad, VSCode, Notepad++):
   - Set 'languageCode': your language code (e.g. 'es', 'pl', 'pt-BR')
   - Set 'languageName': the name of your language in your own script (e.g. 'Español', 'Polski')
   - Set 'author': your name or mod team name
   - Set 'isCustom': true
   - In 'entries', translate each 'Value' matching its 'Key'.
5. Save the file.
6. Launch the game! Your language will automatically appear in Settings -> Language.

TIPS:
- Any keys you leave untranslated will automatically fall back to English.
- UTF-8 encoding is supported; you can use any characters, accents, or emojis.
";
            File.WriteAllText(readmePath, readmeContent);
            AssetDatabase.Refresh();
        }

        private static void OpenLanguagesFolder()
        {
            EnsureDirectory(DefaultStreamingLanguagesPath);
            var fullPath = Path.GetFullPath(DefaultStreamingLanguagesPath);
            EditorUtility.RevealInFinder(fullPath);
        }

        private static void EnsureDirectory(string relativePath)
        {
            if (!Directory.Exists(relativePath))
            {
                Directory.CreateDirectory(relativePath);
            }
        }

        private static void ParseHeader(string rawHeader, out string code, out string displayName)
        {
            var match = Regex.Match(rawHeader, @"^(.*?)\s*\((.*?)\)$");
            if (match.Success)
            {
                displayName = match.Groups[1].Value.Trim();
                code = match.Groups[2].Value.Trim();
            }
            else
            {
                code = rawHeader.Trim();
                displayName = GetLanguageNameFromCode(code);
            }
        }

        private static string GetLanguageNameFromCode(string code)
        {
            return code.ToLowerInvariant() switch
            {
                "en" => "English",
                "tr" => "Türkçe",
                "de" => "Deutsch",
                "fr" => "Français",
                "es" => "Español",
                "it" => "Italiano",
                "pt" or "pt-br" => "Português",
                "ru" => "Русский",
                "pl" => "Polski",
                "ja" => "日本語",
                "ko" => "한국어",
                "zh" or "zh-cn" => "简体中文",
                "zh-tw" => "繁體中文",
                _ => code.ToUpperInvariant()
            };
        }

        private void UpdateStatus(string message, MessageType type)
        {
            _statusMessage = message;
            _statusMessageType = type;
        }

        private static List<string> ParseCsvLine(string line)
        {
            var tokens = new List<string>();
            var inQuotes = false;
            var currentToken = "";

            for (var i = 0; i < line.Length; i++)
            {
                var c = line[i];

                if (c == '\"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    tokens.Add(currentToken);
                    currentToken = "";
                }
                else
                {
                    currentToken += c;
                }
            }

            tokens.Add(currentToken);
            return tokens;
        }
    }
}
#endif
