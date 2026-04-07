// Copyright (c) 2026 Ovation Games. MIT License. See LICENSE for details.

using System.Collections.Generic;
using Ovation.Models;
using UnityEditor;
using UnityEngine;

namespace Ovation.Editor
{
    /// <summary>
    /// Runtime developer tools for testing the Ovation SDK. Available in Play Mode via
    /// Ovation > Dev Tools. Lets you issue achievements, browse achievement definitions,
    /// view player state, link external IDs, and manage test data.
    /// </summary>
    public class OvationDevTools : EditorWindow
    {
        private string _achievementSlug = "";
        private string _externalId = "";
        private Vector2 _scrollPos;
        private List<Achievement> _achievements;
        private List<PlayerAchievement> _playerAchievements;
        private string _statusMessage = "";
        private MessageType _statusType = MessageType.Info;
        private bool _loadingAchievements;
        private bool _loadingPlayerAchievements;

        [MenuItem("Tools/Ovation/Dev Tools")]
        public static void ShowWindow()
        {
            var window = GetWindow<OvationDevTools>("Ovation Dev Tools");
            window.minSize = new Vector2(400, 500);
            window.Show();
        }

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Ovation Dev Tools", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to use dev tools.", MessageType.Info);
                EditorGUILayout.EndScrollView();
                return;
            }

            if (OvationSDK.Instance == null || !OvationSDK.Instance.IsInitialized)
            {
                EditorGUILayout.HelpBox("OvationSDK is not initialized. Make sure it's in your scene with a valid config.", MessageType.Warning);
                EditorGUILayout.EndScrollView();
                return;
            }

            // Status bar
            if (!string.IsNullOrEmpty(_statusMessage))
            {
                EditorGUILayout.HelpBox(_statusMessage, _statusType);
                EditorGUILayout.Space(5);
            }

            DrawSDKStatus();
            EditorGUILayout.Space(10);
            DrawIssueAchievement();
            EditorGUILayout.Space(10);
            DrawAchievementBrowser();
            EditorGUILayout.Space(10);
            DrawPlayerAchievements();
            EditorGUILayout.Space(10);
            DrawPlayerTools();
            EditorGUILayout.Space(10);
            DrawDangerZone();

            EditorGUILayout.EndScrollView();
        }

        private void DrawSDKStatus()
        {
            EditorGUILayout.LabelField("SDK Status", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Initialized", OvationSDK.Instance.IsInitialized.ToString());
            EditorGUILayout.LabelField("Player ID", OvationSDK.Instance.PlayerId ?? "(none)");
            EditorGUILayout.LabelField("Offline Queue", OvationSDK.Instance.OfflineQueueCount.ToString());
            EditorGUI.indentLevel--;
        }

        private void DrawIssueAchievement()
        {
            EditorGUILayout.LabelField("Issue Achievement", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            _achievementSlug = EditorGUILayout.TextField("Slug", _achievementSlug);
            GUI.enabled = !string.IsNullOrEmpty(_achievementSlug);
            if (GUILayout.Button("Unlock", GUILayout.Width(70)))
            {
                var slug = _achievementSlug;
                OvationSDK.Instance.IssueAchievement(slug,
                    result =>
                    {
                        if (result.WasQueued)
                            SetStatus($"'{slug}' queued for offline sync", MessageType.Warning);
                        else if (result.WasNew)
                            SetStatus($"NEW: '{result.DisplayName}' unlocked!", MessageType.Info);
                        else
                            SetStatus($"'{result.DisplayName}' already earned", MessageType.Info);
                        Repaint();
                    },
                    error =>
                    {
                        SetStatus($"Failed: {error.Code} - {error.Message}", MessageType.Error);
                        Repaint();
                    }
                );
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            // Quick-fire buttons for loaded achievements
            if (_achievements != null && _achievements.Count > 0)
            {
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("Quick Unlock:", EditorStyles.miniLabel);
                EditorGUILayout.BeginHorizontal();
                int buttonsInRow = 0;
                foreach (var a in _achievements)
                {
                    if (buttonsInRow >= 3)
                    {
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                        buttonsInRow = 0;
                    }
                    if (GUILayout.Button(a.Slug, EditorStyles.miniButton))
                    {
                        _achievementSlug = a.Slug;
                        OvationSDK.Instance.IssueAchievement(a.Slug,
                            result =>
                            {
                                if (result.WasNew)
                                    SetStatus($"NEW: '{result.DisplayName}' unlocked!", MessageType.Info);
                                else
                                    SetStatus($"'{result.DisplayName}' already earned", MessageType.Info);
                                Repaint();
                            },
                            error =>
                            {
                                SetStatus($"Failed: {error.Message}", MessageType.Error);
                                Repaint();
                            }
                        );
                    }
                    buttonsInRow++;
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawAchievementBrowser()
        {
            EditorGUILayout.LabelField("Authority Achievements", EditorStyles.boldLabel);

            GUI.enabled = !_loadingAchievements;
            if (GUILayout.Button(_loadingAchievements ? "Loading..." : "Fetch All Achievements"))
            {
                _loadingAchievements = true;
                OvationSDK.Instance.GetAchievements(
                    achievements =>
                    {
                        _achievements = achievements;
                        _loadingAchievements = false;
                        SetStatus($"Loaded {achievements.Count} achievements", MessageType.Info);
                        Repaint();
                    },
                    error =>
                    {
                        _loadingAchievements = false;
                        SetStatus($"Failed to load: {error.Message}", MessageType.Error);
                        Repaint();
                    }
                );
            }
            GUI.enabled = true;

            if (_achievements != null)
            {
                EditorGUI.indentLevel++;
                foreach (var a in _achievements)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(a.Slug, EditorStyles.boldLabel, GUILayout.Width(150));
                    EditorGUILayout.LabelField(a.DisplayName);
                    if (a.RarityPercentage.HasValue)
                        EditorGUILayout.LabelField($"{a.RarityPercentage:F1}%", GUILayout.Width(50));
                    if (a.Archived)
                        EditorGUILayout.LabelField("[archived]", GUILayout.Width(70));
                    if (a.IsHidden)
                        EditorGUILayout.LabelField("[hidden]", GUILayout.Width(60));
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
            }
        }

        private void DrawPlayerAchievements()
        {
            EditorGUILayout.LabelField("Player Achievements", EditorStyles.boldLabel);

            GUI.enabled = !_loadingPlayerAchievements;
            if (GUILayout.Button(_loadingPlayerAchievements ? "Loading..." : "Fetch Player Achievements"))
            {
                _loadingPlayerAchievements = true;
                OvationSDK.Instance.GetPlayerAchievements(
                    achievements =>
                    {
                        _playerAchievements = achievements;
                        _loadingPlayerAchievements = false;
                        SetStatus($"Player has {achievements.Count} achievements", MessageType.Info);
                        Repaint();
                    },
                    error =>
                    {
                        _loadingPlayerAchievements = false;
                        SetStatus($"Failed: {error.Message}", MessageType.Error);
                        Repaint();
                    }
                );
            }
            GUI.enabled = true;

            if (_playerAchievements != null)
            {
                EditorGUI.indentLevel++;
                foreach (var pa in _playerAchievements)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(pa.Slug, EditorStyles.boldLabel, GUILayout.Width(150));
                    EditorGUILayout.LabelField(pa.DisplayName);
                    EditorGUILayout.LabelField(pa.EarnedAt.ToString("g"), GUILayout.Width(130));
                    EditorGUILayout.EndHorizontal();

                    if (pa.Assets != null && pa.Assets.Count > 0)
                    {
                        EditorGUI.indentLevel++;
                        foreach (var asset in pa.Assets)
                            EditorGUILayout.LabelField($"[{asset.SlotName}] {asset.DisplayName}", EditorStyles.miniLabel);
                        EditorGUI.indentLevel--;
                    }
                }
                EditorGUI.indentLevel--;
            }
        }

        private void DrawPlayerTools()
        {
            EditorGUILayout.LabelField("Player Tools", EditorStyles.boldLabel);

            // External ID
            EditorGUILayout.BeginHorizontal();
            _externalId = EditorGUILayout.TextField("External ID", _externalId);
            GUI.enabled = !string.IsNullOrEmpty(_externalId);
            if (GUILayout.Button("Link", GUILayout.Width(50)))
            {
                OvationSDK.Instance.SetExternalId(_externalId,
                    result =>
                    {
                        SetStatus($"External ID linked: {result.ExternalId}", MessageType.Info);
                        Repaint();
                    },
                    error =>
                    {
                        SetStatus($"Failed: {error.Message}", MessageType.Error);
                        Repaint();
                    }
                );
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            // Copy Player ID
            if (GUILayout.Button("Copy Player ID to Clipboard"))
            {
                if (!string.IsNullOrEmpty(OvationSDK.Instance.PlayerId))
                {
                    EditorGUIUtility.systemCopyBuffer = OvationSDK.Instance.PlayerId;
                    SetStatus($"Copied: {OvationSDK.Instance.PlayerId}", MessageType.Info);
                }
            }
        }

        private void DrawDangerZone()
        {
            EditorGUILayout.LabelField("Danger Zone", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "These actions are destructive. Reset Player ID creates a new anonymous player. " +
                "Delete Test Data wipes ALL test data for your authority.",
                MessageType.Warning);

            EditorGUILayout.BeginHorizontal();

            var prevColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);

            if (GUILayout.Button("Reset Player ID"))
            {
                if (EditorUtility.DisplayDialog("Reset Player ID",
                    "This will clear the stored player ID and create a new anonymous player on next init. Continue?",
                    "Reset", "Cancel"))
                {
                    PlayerPrefs.DeleteKey("Ovation_PlayerId");
                    PlayerPrefs.Save();
                    SetStatus("Player ID cleared. Restart Play Mode to create a new player.", MessageType.Warning);
                }
            }

            if (GUILayout.Button("Delete Test Data"))
            {
                if (EditorUtility.DisplayDialog("Delete All Test Data",
                    "This will permanently delete ALL test data for your authority (players, achievements, everything). This cannot be undone. Continue?",
                    "Delete Everything", "Cancel"))
                {
                    OvationSDK.Instance.DeleteTestData(
                        () =>
                        {
                            SetStatus("All test data deleted", MessageType.Warning);
                            _achievements = null;
                            _playerAchievements = null;
                            Repaint();
                        },
                        error =>
                        {
                            SetStatus($"Failed: {error.Message}", MessageType.Error);
                            Repaint();
                        }
                    );
                }
            }

            GUI.backgroundColor = prevColor;
            EditorGUILayout.EndHorizontal();
        }

        private void SetStatus(string message, MessageType type)
        {
            _statusMessage = message;
            _statusType = type;
        }

        private void OnInspectorUpdate()
        {
            // Repaint periodically so status updates show
            if (Application.isPlaying)
                Repaint();
        }
    }
}
