#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Core.Editor
{
    public class DataSyncEditorWindow : EditorWindow
    {
        private const string SheetCsvUrlPrefKey = "Core_SheetCsvUrl";
        private const string AppsScriptPostUrlPrefKey = "Core_AppsScriptPostUrl";
        private const string AutoSyncOnPlayPrefKey = "Core_AutoSyncOnPlay";
        private const string TargetConfigGuidPrefKey = "Core_TargetConfigGuid";

        private string sheetCsvUrl = "";
        private string appsScriptPostUrl = "";
        private bool autoSyncOnPlay;
        private ScriptableObject _targetConfig;
        private string _statusMessage = "";
        private MessageType _statusMessageType = MessageType.Info;
        private Vector2 _scrollPosition;

        [MenuItem("Tools/Data Sync Window")]
        public static void OpenWindow()
        {
            var window = GetWindow<DataSyncEditorWindow>("Data Sync");
            window.minSize = new Vector2(400f, 450f);
            window.Show();
        }

        private void OnEnable()
        {
            sheetCsvUrl = EditorPrefs.GetString(SheetCsvUrlPrefKey, "");
            appsScriptPostUrl = EditorPrefs.GetString(AppsScriptPostUrlPrefKey, "");
            autoSyncOnPlay = EditorPrefs.GetBool(AutoSyncOnPlayPrefKey, false);

            var targetGuid = EditorPrefs.GetString(TargetConfigGuidPrefKey, "");
            if (!string.IsNullOrEmpty(targetGuid))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(targetGuid);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    _targetConfig = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
                }
            }
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawHeader();
            DrawConfigurationFields();
            DrawSyncButtons();
            DrawStatusBox();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Data Synchronization", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Bidirectional sync between Google Sheets and ScriptableObjects.", EditorStyles.miniLabel);
            EditorGUILayout.Space(8);
        }

        private void DrawConfigurationFields()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Sync Endpoints & Target", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            EditorGUI.BeginChangeCheck();
            sheetCsvUrl = EditorGUILayout.TextField("Sheet CSV URL", sheetCsvUrl);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetString(SheetCsvUrlPrefKey, sheetCsvUrl);
            }

            EditorGUI.BeginChangeCheck();
            appsScriptPostUrl = EditorGUILayout.TextField("Apps Script POST URL", appsScriptPostUrl);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetString(AppsScriptPostUrlPrefKey, appsScriptPostUrl);
            }

            EditorGUI.BeginChangeCheck();
            _targetConfig = (ScriptableObject)EditorGUILayout.ObjectField("Target SO", _targetConfig, typeof(ScriptableObject), false);
            if (EditorGUI.EndChangeCheck())
            {
                if (_targetConfig != null)
                {
                    var path = AssetDatabase.GetAssetPath(_targetConfig);
                    var guid = AssetDatabase.AssetPathToGUID(path);
                    EditorPrefs.SetString(TargetConfigGuidPrefKey, guid);
                }
                else
                {
                    EditorPrefs.DeleteKey(TargetConfigGuidPrefKey);
                }
            }

            EditorGUILayout.Space(6);

            EditorGUI.BeginChangeCheck();
            autoSyncOnPlay = EditorGUILayout.Toggle("Auto Sync on Play", autoSyncOnPlay);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool(AutoSyncOnPlayPrefKey, autoSyncOnPlay);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }

        private void DrawSyncButtons()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            GUI.backgroundColor = new Color(0.35f, 0.75f, 0.35f);
            if (GUILayout.Button("Pull from Sheets", GUILayout.Height(32)))
            {
                PullFromSheets();
            }

            GUI.backgroundColor = new Color(0.35f, 0.6f, 0.95f);
            if (GUILayout.Button("Push to Sheets", GUILayout.Height(32)))
            {
                PushToSheets();
            }

            GUI.backgroundColor = new Color(0.85f, 0.7f, 0.35f);
            if (GUILayout.Button("Open Localization Sync Window", GUILayout.Height(26)))
            {
                LocalizationSyncEditorWindow.OpenWindow();
            }

            GUI.backgroundColor = Color.white;
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
                UpdateStatus("Sheet CSV URL cannot be empty.", MessageType.Error);
                return;
            }

            if (_targetConfig == null)
            {
                UpdateStatus("Please assign a Target ScriptableObject to populate.", MessageType.Warning);
                return;
            }

            UpdateStatus("Downloading CSV data from Google Sheets...", MessageType.Info);

            ExecutePullRequest(sheetCsvUrl, _targetConfig, (success, message) =>
            {
                UpdateStatus(message, success ? MessageType.Info : MessageType.Error);
                Repaint();
            });
        }

        public void PushToSheets()
        {
            if (string.IsNullOrEmpty(appsScriptPostUrl))
            {
                UpdateStatus("Apps Script POST URL cannot be empty.", MessageType.Error);
                return;
            }

            if (_targetConfig == null)
            {
                UpdateStatus("Please assign a Target ScriptableObject to push.", MessageType.Warning);
                return;
            }

            UpdateStatus("Pushing JSON data to Google Sheets...", MessageType.Info);

            ExecutePushRequest(appsScriptPostUrl, _targetConfig, (success, message) =>
            {
                UpdateStatus(message, success ? MessageType.Info : MessageType.Error);
                Repaint();
            });
        }

        private void UpdateStatus(string message, MessageType type)
        {
            _statusMessage = message;
            _statusMessageType = type;
        }

        public static void SyncDataFromSheets(Action onComplete = null)
        {
            var csvUrl = EditorPrefs.GetString(SheetCsvUrlPrefKey, "");
            if (string.IsNullOrEmpty(csvUrl))
            {
                Debug.LogWarning("[DataSync] Sheet CSV URL is not set in EditorPrefs. Skipping auto-sync.");
                onComplete?.Invoke();
                return;
            }

            ScriptableObject target = null;
            var targetGuid = EditorPrefs.GetString(TargetConfigGuidPrefKey, "");
            if (!string.IsNullOrEmpty(targetGuid))
            {
                var path = AssetDatabase.GUIDToAssetPath(targetGuid);
                if (!string.IsNullOrEmpty(path))
                {
                    target = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                }
            }

            if (target == null)
            {
                Debug.LogWarning("[DataSync] No Target ScriptableObject found for auto-sync.");
                onComplete?.Invoke();
                return;
            }

            ExecutePullRequest(csvUrl, target, (success, message) =>
            {
                if (success)
                {
                    Debug.Log($"[DataSync] Auto-sync completed: {message}");
                }
                else
                {
                    Debug.LogError($"[DataSync] Auto-sync failed: {message}");
                }

                onComplete?.Invoke();
            });
        }

        private static void ExecutePullRequest(string url, ScriptableObject target, Action<bool, string> onFinished)
        {
            var request = UnityWebRequest.Get(url);
            var asyncOp = request.SendWebRequest();

            asyncOp.completed += operation =>
            {
                try
                {
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        var errorMsg = $"Pull failed: {request.error}";
                        Debug.LogError($"[DataSync] {errorMsg}");
                        onFinished?.Invoke(false, errorMsg);
                        return;
                    }

                    var csvText = request.downloadHandler.text;
                    if (string.IsNullOrEmpty(csvText))
                    {
                        var emptyMsg = "CSV response was empty.";
                        Debug.LogWarning($"[DataSync] {emptyMsg}");
                        onFinished?.Invoke(false, emptyMsg);
                        return;
                    }

                    var appliedCount = ParseAndApplyCsv(csvText, target);
                    EditorUtility.SetDirty(target);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                    var successMsg = $"Successfully pulled and updated {appliedCount} fields in {target.name}.";
                    Debug.Log($"[DataSync] {successMsg}");
                    onFinished?.Invoke(true, successMsg);
                }
                catch (Exception exception)
                {
                    var exMsg = $"Error processing CSV data: {exception.Message}";
                    Debug.LogError($"[DataSync] {exMsg}\n{exception.StackTrace}");
                    onFinished?.Invoke(false, exMsg);
                }
                finally
                {
                    request.Dispose();
                }
            };
        }

        private static void ExecutePushRequest(string url, ScriptableObject target, Action<bool, string> onFinished)
        {
            var json = EditorJsonUtility.ToJson(target, true);
            var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            var rawData = System.Text.Encoding.UTF8.GetBytes(json);

            request.uploadHandler = new UploadHandlerRaw(rawData);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            var asyncOp = request.SendWebRequest();
            asyncOp.completed += operation =>
            {
                try
                {
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        var errorMsg = $"Push failed: {request.error}";
                        Debug.LogError($"[DataSync] {errorMsg}");
                        onFinished?.Invoke(false, errorMsg);
                    }
                    else
                    {
                        var responseText = request.downloadHandler.text;
                        var successMsg = $"Push succeeded. Server response: {responseText}";
                        Debug.Log($"[DataSync] {successMsg}");
                        onFinished?.Invoke(true, successMsg);
                    }
                }
                catch (Exception exception)
                {
                    var exMsg = $"Error during push: {exception.Message}";
                    Debug.LogError($"[DataSync] {exMsg}");
                    onFinished?.Invoke(false, exMsg);
                }
                finally
                {
                    request.Dispose();
                }
            };
        }

        public static int ParseAndApplyCsv(string csvContent, ScriptableObject target)
        {
            if (string.IsNullOrEmpty(csvContent) || target == null)
            {
                return 0;
            }

            var serializedObject = new SerializedObject(target);
            serializedObject.Update();

            var lines = csvContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            var appliedCount = 0;

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                {
                    continue;
                }

                var tokens = ParseCsvLine(line);
                if (tokens.Count < 2)
                {
                    continue;
                }

                var key = tokens[0].Trim();
                var value = tokens[1].Trim();

                if (i == 0 && (key.Equals("Key", StringComparison.OrdinalIgnoreCase) || key.Equals("Field", StringComparison.OrdinalIgnoreCase) || key.Equals("Property", StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                var assigned = ApplyProperty(serializedObject, target, key, value);
                if (assigned)
                {
                    appliedCount++;
                }
            }

            serializedObject.ApplyModifiedProperties();
            return appliedCount;
        }

        private static bool ApplyProperty(SerializedObject serializedObject, ScriptableObject target, string key, string value)
        {
            var property = serializedObject.FindProperty(key);
            if (property == null)
            {
                property = serializedObject.FindProperty($"<{key}>k__BackingField");
            }

            if (property != null)
            {
                switch (property.propertyType)
                {
                    case SerializedPropertyType.Integer:
                        if (int.TryParse(value, out var intValue))
                        {
                            property.intValue = intValue;
                            return true;
                        }
                        break;

                    case SerializedPropertyType.Float:
                        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var floatValue))
                        {
                            property.floatValue = floatValue;
                            return true;
                        }
                        break;

                    case SerializedPropertyType.Boolean:
                        if (bool.TryParse(value, out var boolValue))
                        {
                            property.boolValue = boolValue;
                            return true;
                        }
                        break;

                    case SerializedPropertyType.String:
                        property.stringValue = value;
                        return true;

                    case SerializedPropertyType.Enum:
                        if (int.TryParse(value, out var enumIndex))
                        {
                            property.enumValueIndex = enumIndex;
                            return true;
                        }
                        var enumNames = property.enumDisplayNames;
                        for (var i = 0; i < enumNames.Length; i++)
                        {
                            if (enumNames[i].Equals(value, StringComparison.OrdinalIgnoreCase))
                            {
                                property.enumValueIndex = i;
                                return true;
                            }
                        }
                        break;
                }
            }

            return ApplyViaReflection(target, key, value);
        }

        private static bool ApplyViaReflection(ScriptableObject target, string key, string value)
        {
            var targetType = target.GetType();
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase;

            var propInfo = targetType.GetProperty(key, flags);
            if (propInfo != null && propInfo.CanWrite)
            {
                try
                {
                    var convertedValue = ConvertValue(value, propInfo.PropertyType);
                    if (convertedValue != null)
                    {
                        propInfo.SetValue(target, convertedValue);
                        return true;
                    }
                }
                catch
                {
                }
            }

            var fieldInfo = targetType.GetField(key, flags);
            if (fieldInfo == null)
            {
                fieldInfo = targetType.GetField($"<{key}>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
            }

            if (fieldInfo != null)
            {
                try
                {
                    var convertedValue = ConvertValue(value, fieldInfo.FieldType);
                    if (convertedValue != null)
                    {
                        fieldInfo.SetValue(target, convertedValue);
                        return true;
                    }
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        private static object ConvertValue(string rawValue, Type targetType)
        {
            if (targetType == typeof(string))
            {
                return rawValue;
            }

            if (targetType == typeof(int))
            {
                return int.TryParse(rawValue, out var val) ? val : null;
            }

            if (targetType == typeof(float))
            {
                return float.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var val) ? val : null;
            }

            if (targetType == typeof(double))
            {
                return double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var val) ? val : null;
            }

            if (targetType == typeof(bool))
            {
                return bool.TryParse(rawValue, out var val) ? val : null;
            }

            if (targetType.IsEnum)
            {
                if (int.TryParse(rawValue, out var intVal))
                {
                    return Enum.ToObject(targetType, intVal);
                }

                try
                {
                    return Enum.Parse(targetType, rawValue, true);
                }
                catch
                {
                    return null;
                }
            }

            return null;
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
