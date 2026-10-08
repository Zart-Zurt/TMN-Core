using System.IO;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace Core.Editor
{
    public class CorePackageUpdaterWindow : EditorWindow
    {
        private const string PackageId = "com.tmn.core";
        private const string GitRepositoryUrl = "https://github.com/Zart-Zurt/TMN-Core.git";
        private const string LocalPackageUrl = "file:../../TMN-Core";

        private static AddRequest _addRequest;
        private static ListRequest _listRequest;

        private string _targetTagOrBranch = "main";
        private string _installedVersion = "Loading...";
        private string _installedSource = "Unknown";
        private string _installedHash = string.Empty;
        private string _statusMessage = "Ready";
        private MessageType _statusMessageType = MessageType.Info;
        private bool _isBusy;

        [MenuItem("ZZEngine/Packages/TMN Core/Package Updater Window", priority = 50)]
        public static void ShowWindow()
        {
            var window = GetWindow<CorePackageUpdaterWindow>("TMN Core Updater");
            window.minSize = new Vector2(400, 330);
            window.Show();
        }

        [MenuItem("ZZEngine/Packages/TMN Core/Quick Update to Latest", priority = 51)]
        public static void QuickUpdateToLatest()
        {
            if (_addRequest != null && !_addRequest.IsCompleted)
            {
                EditorUtility.DisplayDialog("TMN Core Updater", "An update request is already in progress.", "OK");
                return;
            }

            _addRequest = Client.Add(GitRepositoryUrl);
            EditorApplication.update += MonitorQuickAddRequest;
            EditorUtility.DisplayProgressBar("TMN Core Updater", "Fetching latest Core from GitHub...", 0.5f);
        }

        private static void MonitorQuickAddRequest()
        {
            if (_addRequest == null || !_addRequest.IsCompleted)
            {
                return;
            }

            EditorApplication.update -= MonitorQuickAddRequest;
            EditorUtility.ClearProgressBar();

            if (_addRequest.Status == StatusCode.Success)
            {
                EditorUtility.DisplayDialog("TMN Core Updater", "TMN Core package successfully updated to latest version!", "OK");
            }
            else
            {
                var error = _addRequest.Error != null ? _addRequest.Error.message : "Unknown error";
                EditorUtility.DisplayDialog("TMN Core Updater", "Failed to update package: " + error, "OK");
            }

            _addRequest = null;
        }

        private void OnEnable()
        {
            RefreshInstalledPackageInfo();
        }

        private void Update()
        {
            if (_listRequest != null && _listRequest.IsCompleted)
            {
                ProcessListRequest();
            }

            if (_addRequest != null && _addRequest.IsCompleted)
            {
                ProcessAddRequest();
            }
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawPackageStatus();
            DrawActions();
            DrawStatusMessage();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("TMN Core Package Updater", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Manage and synchronize the Core framework across projects.", EditorStyles.miniLabel);
            EditorGUILayout.Space(10);
        }

        private void DrawPackageStatus()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Package:", PackageId, EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Installed Version:", _installedVersion);
            EditorGUILayout.LabelField("Source:", _installedSource);

            if (!string.IsNullOrEmpty(_installedHash))
            {
                var shortHash = _installedHash.Length > 7 ? _installedHash.Substring(0, 7) : _installedHash;
                EditorGUILayout.LabelField("Git Hash:", shortHash);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }

        private void DrawActions()
        {
            EditorGUI.BeginDisabledGroup(_isBusy);

            if (GUILayout.Button("Update to Latest (main branch)", GUILayout.Height(32)))
            {
                StartUpdate(GitRepositoryUrl);
            }

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            _targetTagOrBranch = EditorGUILayout.TextField("Tag / Branch:", _targetTagOrBranch);
            if (GUILayout.Button("Update to Tag", GUILayout.Width(110)))
            {
                if (string.IsNullOrWhiteSpace(_targetTagOrBranch))
                {
                    SetStatus("Please specify a valid tag or branch.", MessageType.Warning);
                }
                else
                {
                    StartUpdate(GitRepositoryUrl + "#" + _targetTagOrBranch.Trim());
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Switch to Local Mode"))
            {
                StartUpdate(LocalPackageUrl);
            }

            if (GUILayout.Button("Refresh Info"))
            {
                RefreshInstalledPackageInfo();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Force Clear Lock & Resolve"))
            {
                ForceClearLockAndResolve();
            }

            EditorGUI.EndDisabledGroup();
            EditorGUILayout.Space(10);
        }

        private void DrawStatusMessage()
        {
            if (_isBusy)
            {
                EditorGUILayout.HelpBox("Operation in progress, please wait...", MessageType.Info);
            }
            else if (!string.IsNullOrEmpty(_statusMessage))
            {
                EditorGUILayout.HelpBox(_statusMessage, _statusMessageType);
            }
        }

        private void StartUpdate(string targetIdentifier)
        {
            _isBusy = true;
            SetStatus("Updating package from: " + targetIdentifier, MessageType.Info);
            _addRequest = Client.Add(targetIdentifier);
        }

        private void RefreshInstalledPackageInfo()
        {
            _isBusy = true;
            _installedVersion = "Checking...";
            _installedSource = "Checking...";
            _installedHash = string.Empty;
            _listRequest = Client.List(true);
        }

        private void ProcessListRequest()
        {
            _isBusy = false;

            if (_listRequest.Status == StatusCode.Success)
            {
                var found = false;
                foreach (var package in _listRequest.Result)
                {
                    if (package.name == PackageId)
                    {
                        found = true;
                        _installedVersion = package.version;
                        _installedSource = package.source.ToString();
                        _installedHash = package.git != null ? package.git.hash : string.Empty;
                        SetStatus("Package info updated.", MessageType.Info);
                        break;
                    }
                }

                if (!found)
                {
                    _installedVersion = "Not Installed";
                    _installedSource = "None";
                    SetStatus("Package " + PackageId + " is not installed in this project.", MessageType.Warning);
                }
            }
            else
            {
                var error = _listRequest.Error != null ? _listRequest.Error.message : "Unknown error";
                SetStatus("Failed to query packages: " + error, MessageType.Error);
            }

            _listRequest = null;
            Repaint();
        }

        private void ProcessAddRequest()
        {
            _isBusy = false;

            if (_addRequest.Status == StatusCode.Success)
            {
                SetStatus("Package successfully updated!", MessageType.Info);
                RefreshInstalledPackageInfo();
            }
            else
            {
                var error = _addRequest.Error != null ? _addRequest.Error.message : "Unknown error";
                SetStatus("Failed to update package: " + error, MessageType.Error);
            }

            _addRequest = null;
            Repaint();
        }

        private void ForceClearLockAndResolve()
        {
            _isBusy = true;
            SetStatus("Resolving packages...", MessageType.Info);
            Client.Resolve();
            RefreshInstalledPackageInfo();
        }

        private void SetStatus(string message, MessageType messageType)
        {
            _statusMessage = message;
            _statusMessageType = messageType;
            Repaint();
        }
    }
}
