// Copyright (c) 2026 Ovation Games. MIT License. See LICENSE for details.

using System.Collections;
using System.Collections.Generic;
using Ovation.Models;
using Ovation.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Ovation.UI
{
    /// <summary>
    /// Achievement toast notification system.
    /// Add this component to a GameObject in your scene, or call AchievementToast.Create() to set it up automatically.
    /// Subscribes to OvationSDK.OnAchievementEarned and shows a popup when achievements are unlocked.
    /// </summary>
    public class AchievementToast : MonoBehaviour
    {
        [SerializeField] private AchievementToastConfig toastConfig;

        private Canvas _canvas;
        private readonly Queue<IssueAchievementResult> _queue = new Queue<IssueAchievementResult>();
        private bool _showing;

        /// <summary>
        /// Create the toast system automatically. Call this after Ovation.Init() or in Start().
        /// </summary>
        public static AchievementToast Create(AchievementToastConfig config = null)
        {
            var go = new GameObject("[Ovation Toast]");
            DontDestroyOnLoad(go);
            var toast = go.AddComponent<AchievementToast>();
            toast.toastConfig = config;
            return toast;
        }

        private void Start()
        {
            // Create default config if none assigned
            if (toastConfig == null)
                toastConfig = ScriptableObject.CreateInstance<AchievementToastConfig>();

            // Create the overlay canvas
            CreateCanvas();

            // Subscribe to achievement events
            if (OvationSDK.Instance != null)
                OvationSDK.Instance.OnAchievementEarned += OnAchievementEarned;
        }

        private void OnDestroy()
        {
            if (OvationSDK.Instance != null)
                OvationSDK.Instance.OnAchievementEarned -= OnAchievementEarned;
        }

        private void OnAchievementEarned(IssueAchievementResult result)
        {
            _queue.Enqueue(result);
            if (!_showing)
                StartCoroutine(ShowNextToast());
        }

        private IEnumerator ShowNextToast()
        {
            while (_queue.Count > 0)
            {
                _showing = true;
                var result = _queue.Dequeue();
                yield return StartCoroutine(ShowToast(result));
            }
            _showing = false;
        }

        private IEnumerator ShowToast(IssueAchievementResult result)
        {
            var panel = CreateToastPanel(result);
            var rt = panel.GetComponent<RectTransform>();

            // Play sound
            if (toastConfig.sound != null)
            {
                var audioSource = gameObject.GetComponent<AudioSource>();
                if (audioSource == null)
                    audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.PlayOneShot(toastConfig.sound, toastConfig.soundVolume);
            }

            // Calculate positions for slide animation
            var visiblePos = GetVisiblePosition(rt);
            var hiddenPos = GetHiddenPosition(rt, visiblePos);

            // Slide in
            float elapsed = 0f;
            while (elapsed < toastConfig.slideInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / toastConfig.slideInDuration);
                rt.anchoredPosition = Vector2.Lerp(hiddenPos, visiblePos, t);
                yield return null;
            }
            rt.anchoredPosition = visiblePos;

            // Hold
            yield return new WaitForSecondsRealtime(toastConfig.displayDuration);

            // Slide out
            elapsed = 0f;
            while (elapsed < toastConfig.slideOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / toastConfig.slideOutDuration);
                rt.anchoredPosition = Vector2.Lerp(visiblePos, hiddenPos, t);
                yield return null;
            }

            Destroy(panel);
        }

        private void CreateCanvas()
        {
            var canvasGo = new GameObject("OvationToastCanvas");
            canvasGo.transform.SetParent(transform);

            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 9999;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();
        }

        private GameObject CreateToastPanel(IssueAchievementResult result)
        {
            // Main panel
            var panel = new GameObject("AchievementToast");
            panel.transform.SetParent(_canvas.transform, false);

            var panelRt = panel.AddComponent<RectTransform>();
            panelRt.sizeDelta = new Vector2(toastConfig.width, toastConfig.height);
            SetAnchor(panelRt);

            var panelImg = panel.AddComponent<Image>();
            panelImg.color = toastConfig.backgroundColor;

            // Accent strip on the left
            var accent = new GameObject("Accent");
            accent.transform.SetParent(panel.transform, false);
            var accentRt = accent.AddComponent<RectTransform>();
            accentRt.anchorMin = new Vector2(0, 0);
            accentRt.anchorMax = new Vector2(0, 1);
            accentRt.pivot = new Vector2(0, 0.5f);
            accentRt.sizeDelta = new Vector2(4, 0);
            accentRt.anchoredPosition = Vector2.zero;
            var accentImg = accent.AddComponent<Image>();
            accentImg.color = toastConfig.accentColor;

            // Icon placeholder (a colored square)
            var icon = new GameObject("Icon");
            icon.transform.SetParent(panel.transform, false);
            var iconRt = icon.AddComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0, 0.5f);
            iconRt.anchorMax = new Vector2(0, 0.5f);
            iconRt.pivot = new Vector2(0, 0.5f);
            iconRt.sizeDelta = new Vector2(48, 48);
            iconRt.anchoredPosition = new Vector2(16, 0);
            var iconImg = icon.AddComponent<Image>();
            iconImg.color = toastConfig.accentColor;

            // Trophy symbol inside icon
            var trophyText = CreateText(icon, "TrophyIcon", "\u2605", 24, TextAnchor.MiddleCenter, Color.white);
            var trophyRt = trophyText.GetComponent<RectTransform>();
            trophyRt.anchorMin = Vector2.zero;
            trophyRt.anchorMax = Vector2.one;
            trophyRt.sizeDelta = Vector2.zero;

            // Header text: "ACHIEVEMENT UNLOCKED"
            var header = CreateText(panel, "Header", "ACHIEVEMENT UNLOCKED", 10, TextAnchor.LowerLeft, toastConfig.accentColor);
            var headerRt = header.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0, 0.5f);
            headerRt.anchorMax = new Vector2(1, 1);
            headerRt.offsetMin = new Vector2(76, 4);
            headerRt.offsetMax = new Vector2(-12, -8);

            // Achievement name
            var displayName = result.DisplayName ?? result.Slug;
            var title = CreateText(panel, "Title", displayName, 16, TextAnchor.UpperLeft, toastConfig.titleColor);
            var titleRt = title.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0, 0);
            titleRt.anchorMax = new Vector2(1, 0.5f);
            titleRt.offsetMin = new Vector2(76, 8);
            titleRt.offsetMax = new Vector2(-12, -2);
            title.GetComponent<Text>().fontStyle = FontStyle.Bold;

            OvationLogger.Log($"Toast shown: {displayName}");
            return panel;
        }

        private GameObject CreateText(GameObject parent, string name, string content, int fontSize, TextAnchor alignment, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);

            var rt = go.AddComponent<RectTransform>();
            var text = go.AddComponent<Text>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;

            return go;
        }

        private void SetAnchor(RectTransform rt)
        {
            switch (toastConfig.position)
            {
                case ToastPosition.TopRight:
                    rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1, 1);
                    break;
                case ToastPosition.TopLeft:
                    rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
                    break;
                case ToastPosition.BottomRight:
                    rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1, 0);
                    break;
                case ToastPosition.BottomLeft:
                    rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 0);
                    break;
                case ToastPosition.TopCenter:
                    rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1);
                    break;
            }
        }

        private Vector2 GetVisiblePosition(RectTransform rt)
        {
            float m = toastConfig.margin;
            switch (toastConfig.position)
            {
                case ToastPosition.TopRight: return new Vector2(-m, -m);
                case ToastPosition.TopLeft: return new Vector2(m, -m);
                case ToastPosition.BottomRight: return new Vector2(-m, m);
                case ToastPosition.BottomLeft: return new Vector2(m, m);
                case ToastPosition.TopCenter: return new Vector2(0, -m);
                default: return new Vector2(-m, -m);
            }
        }

        private Vector2 GetHiddenPosition(RectTransform rt, Vector2 visiblePos)
        {
            float slideOffset = toastConfig.height + toastConfig.margin;
            switch (toastConfig.position)
            {
                case ToastPosition.TopRight:
                case ToastPosition.TopLeft:
                case ToastPosition.TopCenter:
                    return visiblePos + new Vector2(0, slideOffset);
                case ToastPosition.BottomRight:
                case ToastPosition.BottomLeft:
                    return visiblePos - new Vector2(0, slideOffset);
                default:
                    return visiblePos + new Vector2(0, slideOffset);
            }
        }
    }
}
