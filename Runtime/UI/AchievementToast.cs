// Copyright (c) 2026 Ovation Games. MIT License. See LICENSE for details.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Ovation.Models;
using Ovation.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ovation.UI
{
    /// <summary>
    /// Achievement toast notification system with a modern, minimal design.
    /// Subscribes to OvationSDK.OnAchievementEarned and shows a popup when achievements unlock.
    /// Call <see cref="Create"/> after initializing the SDK.
    /// </summary>
    public class AchievementToast : MonoBehaviour
    {
        [SerializeField] private AchievementToastConfig toastConfig;

        private Canvas _canvas;
        private readonly Queue<IssueAchievementResult> _queue = new Queue<IssueAchievementResult>();
        private bool _showing;
        private Sprite _iconSprite;
        private Sprite _panelSprite;
        private Sprite _pillSprite;
        private Dictionary<string, string> _slotIdToName;

        // Maximum pills to render; any overflow becomes "+N"
        private const int MaxPills = 5;

        /// <summary>
        /// Create the toast system. Call this once after <see cref="OvationSDK.Init"/>.
        /// </summary>
        /// <param name="config">Optional custom config. Pass null for defaults.</param>
        public static AchievementToast Create(AchievementToastConfig config = null)
        {
            var go = new GameObject("[Ovation Toast]");
            DontDestroyOnLoad(go);
            var toast = go.AddComponent<AchievementToast>();
            toast.toastConfig = config;
            return toast;
        }

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Start()
        {
            if (toastConfig == null)
                toastConfig = ScriptableObject.CreateInstance<AchievementToastConfig>();

            LoadEmbeddedIcon();
            GenerateSprites();
            CreateCanvas();
            FetchSlotNames();

            if (OvationSDK.Instance != null)
                OvationSDK.Instance.OnAchievementEarned += OnAchievementEarned;
        }

        private void OnDestroy()
        {
            if (OvationSDK.Instance != null)
                OvationSDK.Instance.OnAchievementEarned -= OnAchievementEarned;
        }

        // ── Queue handling ─────────────────────────────────────────────────────

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

                Achievement detail = null;
                if (OvationSDK.Instance != null && !string.IsNullOrEmpty(result.Slug))
                {
                    bool fetched = false;
                    OvationSDK.Instance.GetAchievement(result.Slug,
                        a => { detail = a; fetched = true; },
                        _ => { fetched = true; }
                    );
                    while (!fetched) yield return null;
                }

                yield return StartCoroutine(ShowToast(result, detail));
            }
            _showing = false;
        }

        private IEnumerator ShowToast(IssueAchievementResult result, Achievement detail)
        {
            var panel = CreateToastPanel(result, detail);
            var rt    = panel.GetComponent<RectTransform>();

            // Force a layout rebuild so ContentSizeFitter has set the panel height
            // before we calculate the slide start/end positions.
            Canvas.ForceUpdateCanvases();

            if (toastConfig.sound != null)
            {
                var src = gameObject.GetComponent<AudioSource>()
                       ?? gameObject.AddComponent<AudioSource>();
                src.PlayOneShot(toastConfig.sound, toastConfig.soundVolume);
            }

            var visiblePos = GetVisiblePosition();
            var hiddenPos  = GetHiddenPosition(visiblePos, rt.rect.height);

            // Slide in
            float elapsed = 0f;
            rt.anchoredPosition = hiddenPos;
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

        // ── Canvas setup ───────────────────────────────────────────────────────

        private void CreateCanvas()
        {
            var canvasGo = new GameObject("OvationToastCanvas");
            canvasGo.transform.SetParent(transform);

            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 9999;

            // Fixed reference resolution — the toast stays proportional at any screen size.
            // At 1920×1080: scale=1.0  |  at 720p: scale≈0.67  |  at 4K: scale=2.0
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode          = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution  = new Vector2(1920, 1080);
            scaler.screenMatchMode      = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight   = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();
        }

        // ── Sprite generation ──────────────────────────────────────────────────

        private void GenerateSprites()
        {
            // 128×128 square, 16px radius — 9-sliced so it can stretch to any panel size
            _panelSprite = CreateRoundedRectSprite(128, 128, 16);
            // 64×32 pill, 8px radius — 9-sliced so it can stretch to any pill width
            _pillSprite  = CreateRoundedRectSprite(64, 32, 8);
        }

        /// <summary>
        /// Generates a white anti-aliased rounded rectangle texture and wraps it in a
        /// 9-sliced sprite so Image.Type.Sliced can stretch it without distorting corners.
        /// </summary>
        private static Sprite CreateRoundedRectSprite(int width, int height, int radius)
        {
            var tex    = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = 0f, dy = 0f;
                    if (x < radius)            dx = radius - x;
                    else if (x > width  - 1 - radius) dx = x - (width  - 1 - radius);
                    if (y < radius)            dy = radius - y;
                    else if (y > height - 1 - radius) dy = y - (height - 1 - radius);

                    if (dx > 0f && dy > 0f)
                    {
                        float dist  = Mathf.Sqrt(dx * dx + dy * dy);
                        float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                        pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
                    }
                    else
                    {
                        pixels[y * width + x] = Color.white;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            var border = new Vector4(radius, radius, radius, radius);
            return Sprite.Create(tex, new Rect(0, 0, width, height),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }

        // ── Icon ───────────────────────────────────────────────────────────────

        private void LoadEmbeddedIcon()
        {
            try
            {
                var bytes = Convert.FromBase64String(EmbeddedIcon.Base64);
                var tex   = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Bilinear;
                if (tex.LoadImage(bytes))
                    _iconSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f));
            }
            catch (Exception ex)
            {
                OvationLogger.Warning($"Failed to load embedded icon: {ex.Message}");
            }
        }

        // ── Slot name cache ────────────────────────────────────────────────────

        private void FetchSlotNames()
        {
            _slotIdToName = new Dictionary<string, string>();
            if (OvationSDK.Instance != null)
            {
                OvationSDK.Instance.GetStandardSlots(
                    slots =>
                    {
                        foreach (var slot in slots)
                            _slotIdToName[slot.Id] = slot.Name;
                        OvationLogger.Log($"Toast: cached {_slotIdToName.Count} slot names");
                    },
                    _ => { }
                );
            }
        }

        // ── Toast panel construction ───────────────────────────────────────────

        private GameObject CreateToastPanel(IssueAchievementResult result, Achievement detail)
        {
            var displayName = result.DisplayName ?? result.Slug ?? "Achievement";

            string rarityText = null;
            if (detail?.RarityPercentage != null)
            {
                var tier = GetRarityTier(detail.RarityPercentage.Value);
                rarityText = $"{tier} · {detail.RarityPercentage.Value:F1}%";
            }

            var slotNames = new List<string>();
            if (detail?.SlotAssets != null)
            {
                foreach (var slotId in detail.SlotAssets.Keys)
                {
                    var name = (_slotIdToName != null && _slotIdToName.TryGetValue(slotId, out var n))
                        ? n : slotId;
                    slotNames.Add(StartCase(name));
                }
            }

            // ── Root panel ──────────────────────────────────────────────────
            // Width is fixed; height is driven by ContentSizeFitter after layout.
            var panel   = new GameObject("AchievementToast");
            panel.transform.SetParent(_canvas.transform, false);

            var panelRt = panel.AddComponent<RectTransform>();
            panelRt.sizeDelta = new Vector2(toastConfig.width, 0);
            ApplyAnchor(panelRt);

            var panelImg = panel.AddComponent<Image>();
            panelImg.color = toastConfig.backgroundColor;
            if (_panelSprite != null)
            {
                panelImg.sprite = _panelSprite;
                panelImg.type   = Image.Type.Sliced;
                panelImg.pixelsPerUnitMultiplier = 1f;
            }

            // Horizontal layout: [icon column] [content column]
            var hlg = panel.AddComponent<HorizontalLayoutGroup>();
            hlg.padding             = new RectOffset(12, 16, 14, 14);
            hlg.spacing             = 12;
            hlg.childAlignment      = TextAnchor.MiddleLeft;
            hlg.childControlWidth   = true;
            hlg.childControlHeight  = true;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = true;   // children fill the computed panel height

            // Panel auto-sizes its height to fit content
            var panelCsf = panel.AddComponent<ContentSizeFitter>();
            panelCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            panelCsf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

            // ── Icon column ─────────────────────────────────────────────────
            var iconCol = new GameObject("IconColumn");
            iconCol.transform.SetParent(panel.transform, false);

            var iconColVlg = iconCol.AddComponent<VerticalLayoutGroup>();
            iconColVlg.childAlignment      = TextAnchor.MiddleCenter;
            iconColVlg.childControlWidth   = true;
            iconColVlg.childControlHeight  = false;
            iconColVlg.childForceExpandWidth  = true;
            iconColVlg.childForceExpandHeight = false;

            var iconColLe = iconCol.AddComponent<LayoutElement>();
            iconColLe.minWidth       = 44;
            iconColLe.preferredWidth = 44;
            iconColLe.flexibleWidth  = 0;

            var iconImgGo  = new GameObject("Icon");
            iconImgGo.transform.SetParent(iconCol.transform, false);

            var iconImgLe        = iconImgGo.AddComponent<LayoutElement>();
            iconImgLe.minWidth   = 36;
            iconImgLe.preferredWidth  = 36;
            iconImgLe.minHeight  = 36;
            iconImgLe.preferredHeight = 36;
            iconImgLe.flexibleWidth   = 0;
            iconImgLe.flexibleHeight  = 0;

            var iconImg = iconImgGo.AddComponent<Image>();
            if (_iconSprite != null)
                iconImg.sprite = _iconSprite;
            else
                iconImg.color = toastConfig.accentColor;
            iconImg.preserveAspect = true;

            // ── Content column ──────────────────────────────────────────────
            var contentCol = new GameObject("ContentColumn");
            contentCol.transform.SetParent(panel.transform, false);

            var contentColLe      = contentCol.AddComponent<LayoutElement>();
            contentColLe.flexibleWidth = 1;

            var contentVlg = contentCol.AddComponent<VerticalLayoutGroup>();
            contentVlg.spacing             = 3;
            contentVlg.childAlignment      = TextAnchor.UpperLeft;
            contentVlg.childControlWidth   = true;
            contentVlg.childControlHeight  = true;
            contentVlg.childForceExpandWidth  = true;
            contentVlg.childForceExpandHeight = false;

            var contentCsf = contentCol.AddComponent<ContentSizeFitter>();
            contentCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentCsf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

            // Header: "ACHIEVEMENT UNLOCKED"
            AddText(contentCol.transform, "Header", "ACHIEVEMENT UNLOCKED",
                fontSize: 10f, style: FontStyles.Normal, color: toastConfig.accentColor,
                alignment: TextAlignmentOptions.Left, preferredHeight: 13f,
                characterSpacing: 3f);

            // Achievement name
            AddText(contentCol.transform, "Title", displayName,
                fontSize: 17f, style: FontStyles.Bold, color: toastConfig.titleColor,
                alignment: TextAlignmentOptions.Left, preferredHeight: 23f);

            // Rarity (optional)
            if (rarityText != null)
            {
                AddText(contentCol.transform, "Rarity", rarityText,
                    fontSize: 11f, style: FontStyles.Italic, color: toastConfig.descriptionColor,
                    alignment: TextAlignmentOptions.Left, preferredHeight: 14f);
            }

            // Pills row (optional)
            if (slotNames.Count > 0)
            {
                var pillsRow    = new GameObject("PillsRow");
                pillsRow.transform.SetParent(contentCol.transform, false);

                var pillsRowLe  = pillsRow.AddComponent<LayoutElement>();
                pillsRowLe.minHeight       = 22;
                pillsRowLe.preferredHeight = 22;
                pillsRowLe.flexibleHeight  = 0;

                var pillsHlg = pillsRow.AddComponent<HorizontalLayoutGroup>();
                pillsHlg.spacing             = 5;
                pillsHlg.childAlignment      = TextAnchor.MiddleLeft;
                // childControlWidth=true means the group reads LayoutElement.preferredWidth
                // and applies it; childForceExpandWidth=false keeps pills their natural size
                pillsHlg.childControlWidth   = true;
                pillsHlg.childControlHeight  = true;
                pillsHlg.childForceExpandWidth  = false;
                pillsHlg.childForceExpandHeight = true;

                // Render up to MaxPills, collapse remainder into "+N"
                int visibleCount = Mathf.Min(slotNames.Count, MaxPills);
                int overflow     = slotNames.Count - visibleCount;
                if (overflow > 0)
                    visibleCount = MaxPills - 1; // leave room for "+N"

                for (int i = 0; i < visibleCount; i++)
                    AddPill(pillsRow.transform, slotNames[i]);

                if (overflow > 0)
                    AddPill(pillsRow.transform, $"+{overflow + 1}");
            }

            OvationLogger.Log($"Toast: {displayName}" +
                (rarityText != null ? $" ({rarityText})" : "") +
                (slotNames.Count > 0 ? $" [{string.Join(", ", slotNames)}]" : ""));

            return panel;
        }

        // ── UI helpers ─────────────────────────────────────────────────────────

        private static void AddText(Transform parent, string objName, string text,
            float fontSize, FontStyles style, Color color, TextAlignmentOptions alignment,
            float preferredHeight, float characterSpacing = 0f)
        {
            var go = new GameObject(objName);
            go.transform.SetParent(parent, false);

            var le             = go.AddComponent<LayoutElement>();
            le.preferredHeight = preferredHeight;
            le.minHeight       = preferredHeight;
            le.flexibleHeight  = 0;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text               = text;
            tmp.fontSize           = fontSize;
            tmp.fontStyle          = style;
            tmp.color              = color;
            tmp.alignment          = alignment;
            tmp.enableWordWrapping = false;
            tmp.overflowMode       = TextOverflowModes.Ellipsis;
            tmp.characterSpacing   = characterSpacing;
        }

        private void AddPill(Transform parent, string label)
        {
            var pill = new GameObject("Pill_" + label);
            pill.transform.SetParent(parent, false);

            var pillImg   = pill.AddComponent<Image>();
            var pillColor = toastConfig.accentColor;
            pillColor.a   = 0.22f;
            pillImg.color = pillColor;
            if (_pillSprite != null)
            {
                pillImg.sprite = _pillSprite;
                pillImg.type   = Image.Type.Sliced;
                pillImg.pixelsPerUnitMultiplier = 1f;
            }

            // Measure text width so we can tell the parent layout group exactly how wide
            // this pill should be. This avoids ContentSizeFitter conflicts with the parent.
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(pill.transform, false);

            var textRt         = textGo.AddComponent<RectTransform>();
            textRt.anchorMin   = Vector2.zero;
            textRt.anchorMax   = Vector2.one;
            textRt.offsetMin   = new Vector2(8f, 2f);
            textRt.offsetMax   = new Vector2(-8f, -2f);

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text               = label;
            tmp.fontSize           = 10f;
            tmp.fontStyle          = FontStyles.Normal;
            tmp.color              = toastConfig.accentColor;
            tmp.alignment          = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            tmp.overflowMode       = TextOverflowModes.Ellipsis;

            // GetPreferredValues reads font metrics directly — works before layout runs
            var textW  = tmp.GetPreferredValues(label, float.PositiveInfinity, 22f).x;
            var pillW  = Mathf.Max(textW + 16f, 28f);

            var pillLe          = pill.AddComponent<LayoutElement>();
            pillLe.minWidth     = pillW;
            pillLe.preferredWidth  = pillW;
            pillLe.flexibleWidth   = 0;
            pillLe.minHeight       = 22f;
            pillLe.preferredHeight = 22f;
            pillLe.flexibleHeight  = 0;
        }

        // ── Positioning ────────────────────────────────────────────────────────

        private void ApplyAnchor(RectTransform rt)
        {
            switch (toastConfig.position)
            {
                case ToastPosition.TopRight:   rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f); break;
                case ToastPosition.TopLeft:    rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f); break;
                case ToastPosition.BottomRight:rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0f); break;
                case ToastPosition.BottomLeft: rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 0f); break;
                case ToastPosition.TopCenter:  rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f); break;
            }
        }

        private Vector2 GetVisiblePosition()
        {
            float m = toastConfig.margin;
            switch (toastConfig.position)
            {
                case ToastPosition.TopRight:    return new Vector2(-m,  -m);
                case ToastPosition.TopLeft:     return new Vector2( m,  -m);
                case ToastPosition.BottomRight: return new Vector2(-m,   m);
                case ToastPosition.BottomLeft:  return new Vector2( m,   m);
                case ToastPosition.TopCenter:   return new Vector2( 0f, -m);
                default:                        return new Vector2(-m,  -m);
            }
        }

        private Vector2 GetHiddenPosition(Vector2 visiblePos, float panelHeight)
        {
            float slideOffset = panelHeight + toastConfig.margin + 4f;
            switch (toastConfig.position)
            {
                case ToastPosition.BottomRight:
                case ToastPosition.BottomLeft:
                    return visiblePos - new Vector2(0f, slideOffset);
                default:
                    return visiblePos + new Vector2(0f, slideOffset);
            }
        }

        // ── Utility ────────────────────────────────────────────────────────────

        private static string GetRarityTier(float percentage)
        {
            if (percentage <= 1f)  return "Mythic";
            if (percentage <= 5f)  return "Legendary";
            if (percentage <= 20f) return "Rare";
            if (percentage <= 50f) return "Uncommon";
            return "Common";
        }

        /// <summary>Converts a slug like "avatar_frame" to "Avatar Frame".</summary>
        internal static string StartCase(string slug)
        {
            if (string.IsNullOrEmpty(slug)) return slug;
            var spaced = Regex.Replace(slug, "[_-]", " ");
            return Regex.Replace(spaced, @"\b\w", m => m.Value.ToUpper());
        }
    }
}
