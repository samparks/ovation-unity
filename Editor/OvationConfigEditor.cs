// Copyright (c) 2026 Ovation Games. MIT License. See LICENSE for details.

using UnityEditor;
using UnityEngine;

namespace Ovation.Editor
{
    /// <summary>
    /// Custom inspector for <see cref="OvationConfig"/>. Shows a masked API key field,
    /// environment info, and runtime status when in Play Mode.
    /// </summary>
    [CustomEditor(typeof(OvationConfig))]
    public class OvationConfigEditor : UnityEditor.Editor
    {
        private SerializedProperty _apiKey;
        private SerializedProperty _environment;
        private SerializedProperty _baseUrlOverride;
        private SerializedProperty _enableDebugLogging;
        private SerializedProperty _autoManagePlayerId;
        private SerializedProperty _maxQueueSize;
        private SerializedProperty _queueFlushIntervalSeconds;
        private SerializedProperty _maxCacheSizeMB;

        private bool _showApiKey;

        private void OnEnable()
        {
            _apiKey = serializedObject.FindProperty("apiKey");
            _environment = serializedObject.FindProperty("environment");
            _baseUrlOverride = serializedObject.FindProperty("baseUrlOverride");
            _enableDebugLogging = serializedObject.FindProperty("enableDebugLogging");
            _autoManagePlayerId = serializedObject.FindProperty("autoManagePlayerId");
            _maxQueueSize = serializedObject.FindProperty("maxQueueSize");
            _queueFlushIntervalSeconds = serializedObject.FindProperty("queueFlushIntervalSeconds");
            _maxCacheSizeMB = serializedObject.FindProperty("maxCacheSizeMB");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Ovation SDK Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // Authentication section
            EditorGUILayout.LabelField("Authentication", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Your API key should be fetched from your backend at runtime. " +
                "Setting it here is for editor testing only — never ship a build with the key embedded.",
                MessageType.Warning);

            EditorGUILayout.BeginHorizontal();
            if (_showApiKey)
                EditorGUILayout.PropertyField(_apiKey, new GUIContent("API Key"));
            else
            {
                var masked = string.IsNullOrEmpty(_apiKey.stringValue)
                    ? "(not set)"
                    : _apiKey.stringValue.Substring(0, Mathf.Min(20, _apiKey.stringValue.Length)) + "...";
                EditorGUILayout.LabelField("API Key", masked);
            }
            if (GUILayout.Button(_showApiKey ? "Hide" : "Show", GUILayout.Width(50)))
                _showApiKey = !_showApiKey;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Environment section
            EditorGUILayout.LabelField("Environment", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_environment);
            EditorGUILayout.PropertyField(_baseUrlOverride, new GUIContent("Base URL Override"));

            if (string.IsNullOrEmpty(_baseUrlOverride.stringValue))
            {
                EditorGUILayout.HelpBox(
                    $"Using default: {OvationConfig.DefaultBaseUrl}",
                    MessageType.Info);
            }

            EditorGUILayout.Space(10);

            // Player Identity
            EditorGUILayout.LabelField("Player Identity", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_autoManagePlayerId);
            if (!_autoManagePlayerId.boolValue)
            {
                EditorGUILayout.HelpBox(
                    "Auto player ID management is disabled. You must call SetPlayerId() before issuing achievements.",
                    MessageType.Info);
            }

            EditorGUILayout.Space(10);

            // Debug
            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_enableDebugLogging);

            EditorGUILayout.Space(10);

            // Offline Queue
            EditorGUILayout.LabelField("Offline Queue", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_maxQueueSize);
            EditorGUILayout.PropertyField(_queueFlushIntervalSeconds);

            EditorGUILayout.Space(10);

            // Asset Cache
            EditorGUILayout.LabelField("Asset Cache", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_maxCacheSizeMB, new GUIContent("Max Cache Size (MB)"));

            EditorGUILayout.Space(10);

            // Utility buttons
            if (Application.isPlaying && OvationSDK.Instance != null)
            {
                EditorGUILayout.LabelField("Runtime Info", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Player ID", OvationSDK.Instance.PlayerId ?? "(none)");
                EditorGUILayout.LabelField("Initialized", OvationSDK.Instance.IsInitialized.ToString());
                EditorGUILayout.LabelField("Queued Items", OvationSDK.Instance.OfflineQueueCount.ToString());
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
