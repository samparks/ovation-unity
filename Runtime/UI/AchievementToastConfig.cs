// Copyright (c) 2026 Ovation Games. MIT License. See LICENSE for details.

using UnityEngine;

namespace Ovation.UI
{
    /// <summary>
    /// Configuration for the achievement toast notification popup. Customize colors, position,
    /// timing, and audio. Create via Assets > Create > Ovation > Toast Config, or pass null
    /// to <see cref="AchievementToast.Create"/> to use sensible defaults.
    /// </summary>
    [CreateAssetMenu(fileName = "OvationToastConfig", menuName = "Ovation/Toast Config", order = 2)]
    public class AchievementToastConfig : ScriptableObject
    {
        [Header("Appearance")]
        [Tooltip("Background color of the toast panel.")]
        public Color backgroundColor = new Color(0.12f, 0.12f, 0.16f, 0.92f);

        [Tooltip("Primary text color (achievement name).")]
        public Color titleColor = Color.white;

        [Tooltip("Secondary text color (description).")]
        public Color descriptionColor = new Color(0.75f, 0.75f, 0.78f, 1f);

        [Tooltip("Accent color for the left border strip.")]
        public Color accentColor = new Color(0.38f, 0.71f, 1f, 1f);

        [Header("Layout")]
        [Tooltip("Screen position for the toast.")]
        public ToastPosition position = ToastPosition.TopRight;

        [Tooltip("Width of the toast panel in canvas units at 1920×1080 reference resolution. Scales proportionally on all screen sizes.")]
        public float width = 360f;

        [Tooltip("Margin from the screen edge in canvas units at 1920×1080 reference resolution.")]
        public float margin = 20f;

        [Header("Timing")]
        [Tooltip("How long the toast stays visible (seconds).")]
        public float displayDuration = 3f;

        [Tooltip("Slide-in animation duration (seconds).")]
        public float slideInDuration = 0.3f;

        [Tooltip("Slide-out animation duration (seconds).")]
        public float slideOutDuration = 0.3f;

        [Header("Audio")]
        [Tooltip("Optional sound effect when a toast appears.")]
        public AudioClip sound;

        [Tooltip("Sound volume (0-1).")]
        [Range(0f, 1f)]
        public float soundVolume = 0.5f;
    }

    /// <summary>
    /// Screen position for the achievement toast notification.
    /// </summary>
    public enum ToastPosition
    {
        /// <summary>Top-right corner of the screen (default, matches Steam/Xbox style).</summary>
        TopRight,
        /// <summary>Top-left corner of the screen.</summary>
        TopLeft,
        /// <summary>Bottom-right corner of the screen.</summary>
        BottomRight,
        /// <summary>Bottom-left corner of the screen.</summary>
        BottomLeft,
        /// <summary>Top-center of the screen.</summary>
        TopCenter
    }
}
