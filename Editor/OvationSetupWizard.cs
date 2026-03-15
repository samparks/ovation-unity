// Copyright (c) 2026 Ovation Games. MIT License. See LICENSE for details.

using UnityEditor;
using UnityEngine;

namespace Ovation.Editor
{
    /// <summary>
    /// Step-by-step wizard for first-time Ovation SDK setup. Guides the developer through
    /// entering their API key, selecting an environment, and creating a config asset.
    /// Access via Ovation > Setup Wizard in the Unity menu bar.
    /// </summary>
    public class OvationSetupWizard : EditorWindow
    {
        private string _apiKey = "";
        private OvationEnvironment _environment = OvationEnvironment.Test;
        private string _baseUrlOverride = "";
        private int _step;

        [MenuItem("Ovation/Setup Wizard")]
        public static void ShowWindow()
        {
            var window = GetWindow<OvationSetupWizard>("Ovation Setup");
            window.minSize = new Vector2(450, 350);
            window.Show();
        }

        [MenuItem("Ovation/Create Config Asset")]
        public static void CreateConfigAsset()
        {
            var config = ScriptableObject.CreateInstance<OvationConfig>();
            var path = EditorUtility.SaveFilePanelInProject(
                "Save Ovation Config",
                "OvationConfig",
                "asset",
                "Choose a location for the Ovation config asset");

            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(config, path);
                AssetDatabase.SaveAssets();
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = config;
                Debug.Log($"[Ovation] Config asset created at {path}");
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Ovation SDK Setup", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            switch (_step)
            {
                case 0:
                    DrawWelcomeStep();
                    break;
                case 1:
                    DrawApiKeyStep();
                    break;
                case 2:
                    DrawEnvironmentStep();
                    break;
                case 3:
                    DrawFinishStep();
                    break;
            }
        }

        private void DrawWelcomeStep()
        {
            EditorGUILayout.HelpBox(
                "Welcome to Ovation! This wizard will help you set up the SDK in your project.\n\n" +
                "You'll need:\n" +
                "1. An Ovation authority account (register at app.ovation.games)\n" +
                "2. An API key from the Ovation portal\n\n" +
                "The setup takes about 1 minute.",
                MessageType.Info);

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Get Started"))
                _step = 1;
        }

        private void DrawApiKeyStep()
        {
            EditorGUILayout.LabelField("Step 1: API Key", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "Enter your API key for editor testing. In production, fetch this from your backend at runtime " +
                "using OvationSDK.Instance.SetApiKey() — never embed keys in builds.",
                MessageType.Warning);

            _apiKey = EditorGUILayout.TextField("API Key", _apiKey);

            if (!string.IsNullOrEmpty(_apiKey))
            {
                if (_apiKey.StartsWith("ovn_test_"))
                    EditorGUILayout.HelpBox("Test key detected. Test data is isolated from live data.", MessageType.Info);
                else if (_apiKey.StartsWith("ovn_live_"))
                    EditorGUILayout.HelpBox("Live key detected. This will create real data.", MessageType.Warning);
                else
                    EditorGUILayout.HelpBox("Key doesn't match expected format (ovn_test_* or ovn_live_*).", MessageType.Error);
            }

            EditorGUILayout.Space(10);
            DrawNavButtons();
        }

        private void DrawEnvironmentStep()
        {
            EditorGUILayout.LabelField("Step 2: Environment", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            _environment = (OvationEnvironment)EditorGUILayout.EnumPopup("Environment", _environment);

            EditorGUILayout.Space(5);
            _baseUrlOverride = EditorGUILayout.TextField("Base URL Override (optional)", _baseUrlOverride);

            if (string.IsNullOrEmpty(_baseUrlOverride))
            {
                EditorGUILayout.HelpBox(
                    $"Using default: {OvationConfig.DefaultBaseUrl}",
                    MessageType.Info);
            }

            EditorGUILayout.Space(10);
            DrawNavButtons();
        }

        private void DrawFinishStep()
        {
            EditorGUILayout.LabelField("Step 3: Create Config", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("Summary:");
            EditorGUILayout.LabelField($"  Environment: {_environment}");
            EditorGUILayout.LabelField($"  API Key: {(_apiKey.Length > 20 ? _apiKey.Substring(0, 20) + "..." : _apiKey)}");
            EditorGUILayout.LabelField($"  Base URL: {(string.IsNullOrEmpty(_baseUrlOverride) ? OvationConfig.DefaultBaseUrl : _baseUrlOverride)}");

            EditorGUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "Next steps after creating the config:\n\n" +
                "1. Create an empty GameObject in your scene\n" +
                "2. Add the OvationSDK component to it\n" +
                "3. Drag the config asset onto the 'Config' field\n" +
                "4. The SDK will auto-initialize on Start()",
                MessageType.Info);

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Create Config Asset & Finish"))
            {
                CreateConfigWithSettings();
                Close();
            }

            EditorGUILayout.Space(5);
            if (GUILayout.Button("Back"))
                _step--;
        }

        private void DrawNavButtons()
        {
            EditorGUILayout.BeginHorizontal();
            if (_step > 0 && GUILayout.Button("Back"))
                _step--;
            if (GUILayout.Button("Next"))
                _step++;
            EditorGUILayout.EndHorizontal();
        }

        private void CreateConfigWithSettings()
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "Save Ovation Config",
                "OvationConfig",
                "asset",
                "Choose a location for the Ovation config asset");

            if (string.IsNullOrEmpty(path))
                return;

            var config = ScriptableObject.CreateInstance<OvationConfig>();

            // Use SerializedObject to set private fields
            AssetDatabase.CreateAsset(config, path);
            var so = new SerializedObject(config);
            so.FindProperty("apiKey").stringValue = _apiKey;
            so.FindProperty("environment").enumValueIndex = (int)_environment;
            so.FindProperty("baseUrlOverride").stringValue = _baseUrlOverride;
            so.ApplyModifiedProperties();

            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = config;

            Debug.Log($"[Ovation] Config asset created at {path}. Add OvationSDK to a GameObject and assign this config.");
        }
    }
}
